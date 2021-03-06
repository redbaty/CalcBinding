﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Markup;
using CalcBinding.PathAnalysis.Tokens.Abstract;
using CalcBinding.PathAnalysis.Tokens.Realization;

namespace CalcBinding.PathAnalysis
{
    /// <summary>
    ///     Idea of parser: to detect right all entries of property pathes, static property pathes etc. without parsing
    ///     language structures
    ///     For full validation of expression there need to write own analizer of C# lanquage whick could determine xaml names
    ///     too...
    /// </summary>
    public class PropertyPathAnalyzer
    {
        #region Static constructor

        static PropertyPathAnalyzer()
        {
            Delimiters = KnownDelimiters.Concat(UnknownDelimiters).Concat(QuoteTerminals).ToArray();
        }

        #endregion


        #region Nested types

        private class Chunk
        {
            public Chunk(string value, int startPosition, int endPosition)
            {
                Value = value;
                Start = startPosition;
                End = endPosition;
            }

            public string Value { get; }
            public int Start { get; }
            public int End { get; }
        }

        #endregion

        #region Private fields

        public static readonly char[] UnknownDelimiters =
        {
            '(', ')', '+', '-', '*', '/', '%', '^', '&', '|', '?', '<', '>', '=', '!', ',', ' '
        };

        public static readonly char[] KnownDelimiters =
        {
            '.', ':'
        };

        public static readonly string[] Keywords =
        {
            "null"
        };

        public static readonly char[] QuoteTerminals =
        {
            '\'', '"'
        };

        private static readonly char[] Delimiters;
        private IXamlTypeResolver _typeResolver;

        #endregion


        #region Parser cycle

        public IEnumerable<PathToken> GetPathes(string normPath, IXamlTypeResolver typeResolver)
        {
            _typeResolver = typeResolver;

            Debug.WriteLine($"PropertyPathAnalyzer.GetPathes: start read {normPath} ");

            var chunks = GetChunks(normPath);
            var pathes = GetPathes(chunks);

            return pathes;
        }

        private static IEnumerable<Chunk> GetChunks(string str)
        {
            var chunkStart = 0;
            var isChunk = false;
            var chunks = new List<Chunk>();
            var position = 0;
            var skip = false;
            var skipTerminal = '\'';

            do
            {
                var c = position >= str.Length ? (char) 0 : str[position];

                // skip strings
                if (skip)
                {
                    if (c == skipTerminal)
                        skip = false;
                }
                else
                {
                    var isDelim = UnknownDelimiters.Contains(c) || QuoteTerminals.Contains(c) || c == 0;

                    if (isChunk)
                        if (isDelim)
                        {
                            chunks.Add(new Chunk(SubStr(str, chunkStart, position - 1), chunkStart, position - 1));
                            isChunk = false;
                        }

                    // dangerous code
                    if (!isChunk)
                        if (isDelim)
                        {
                            if (QuoteTerminals.Contains(c))
                            {
                                skipTerminal = c;
                                skip = true;
                            }
                        }
                        else
                        {
                            chunkStart = position;
                            isChunk = true;
                        }
                }

                if (c == 0)
                    return chunks;

                position++;
            } while (true);
        }

        private IEnumerable<PathToken> GetPathes(IEnumerable<Chunk> chunks)
        {
            var tokens = new List<PathToken>();

            foreach (var chunk in chunks)
            {
                if (!GetPath(chunk, out var path)) continue;
                TracePath(path);
                tokens.Add(path);
            }

            return tokens;
        }

        private bool GetPath(Chunk chunk, out PathToken pathToken)
        {
            var str = chunk.Value;

            if (Keywords.Contains(str))
            {
                pathToken = null;
                return false;
            }

            var colonPos = str.IndexOf(':');

            if (colonPos > 0)
            {
                var left = SubStr(str, 0, colonPos - 1);

                if (IsIdentifier(left))
                {
                    if (GetPropChain(SubStr(str, colonPos + 1, str.Length - 1), out var propChain))
                        if (propChain.Count > 1)
                        {
                            pathToken = GetEnumOrStaticProperty(chunk, left, propChain);
                            return true;
                        }
                }
            }
            else
            {
                if (GetPropChain(str, out var propChain))
                {
                    pathToken = GetPropPathOrMath(chunk, propChain);
                    return true;
                }
            }

            pathToken = null;
            return false;
        }

        private static bool GetPropChain(string str, out List<string> propChain)
        {
            var properties = str.Split(new[] {'.'}, StringSplitOptions.None);

            if (properties.All(IsIdentifier) && properties.Any())
            {
                propChain = properties.ToList();
                return true;
            }

            propChain = null;
            return false;
        }

        private static bool IsIdentifier(string str)
        {
            if (str.Length == 0)
                return false;

            var firstChar = str[0];

            if (char.IsDigit(firstChar) || Delimiters.Contains(firstChar))
                return false;

            for (var i = 1; i <= str.Length - 1; i++)
                if (Delimiters.Contains(str[i]))
                    return false;

            return true;
        }

        private static PathToken GetPropPathOrMath(Chunk chunk, IReadOnlyList<string> propChain)
        {
            PathToken pathToken;

            if (propChain.Count == 2 && propChain[0] == "Math")
                pathToken = new MathToken(chunk.Start, chunk.End, propChain[1]);
            else
                pathToken = new PropertyPathToken(chunk.Start, chunk.End, propChain);

            return pathToken;
        }

        private PathToken GetEnumOrStaticProperty(Chunk chunk, string @namespace, IReadOnlyList<string> identifierChain)
        {
            PathToken pathToken;
            Type enumType;
            var className = identifierChain[0];
            var fullClassName = $"{@namespace}:{className}";

            var propertyChain = identifierChain.Skip(1).ToList();
            if (propertyChain.Count == 1 && (enumType = TakeEnum(fullClassName)) != null)
            {
                // enum output
                var enumMember = propertyChain.Single();
                pathToken = new EnumToken(chunk.Start, chunk.End, @namespace, enumType, enumMember);
            }
            else
            {
                //static property path output
                pathToken = new StaticPropertyPathToken(chunk.Start, chunk.End, @namespace, className, propertyChain);
            }

            return pathToken;
        }

        #endregion


        #region Help methods

        private static string SubStr(string str, int start, int end)
        {
            return str.Substring(start, end - start + 1);
        }

        /// <summary>
        ///     Found out whether xaml namespace:class is enum class or not. If yes, return enum type, otherwise - null
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        private Type TakeEnum(string fullTypeName)
        {
            var type = _typeResolver.Resolve(fullTypeName);

            if (type != null && type.IsEnum)
                return type;

            return null;
        }

        private static void TracePath(PathToken path)
        {
            Debug.WriteLine("PropertyPathAnalyzer: read {0} ({1}) ({2}-{3})", path.Id.Value, path.Id.PathType,
                path.Start, path.End);
        }

        #endregion
    }
}