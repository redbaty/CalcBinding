using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using CalcBinding.Inversion;
using DynamicExpresso;
using Expression = System.Linq.Expressions.Expression;

namespace CalcBinding
{
    /// <summary>
    ///     Converter that supports expression evaluate
    /// </summary>
    public class CalcConverter : IValueConverter, IMultiValueConverter
    {
        private Lambda compiledExpression;
        private Lambda compiledInversedExpression;

        private readonly IExpressionParser parser;
        private Type[] sourceValuesTypes;

        public bool StringFormatDefined { get; set; }

        public FalseToVisibility FalseToVisibility { get; set; } = FalseToVisibility.Collapsed;

        #region Init

        public CalcConverter() : this(null, null)
        {
        }

        public CalcConverter(IExpressionParser parser) : this(parser, null)
        {
        }

        public CalcConverter(Dictionary<string, Type> enumParameters) : this(null, enumParameters)
        {
        }

        public CalcConverter(IExpressionParser parser, Dictionary<string, Type> enums)
        {
            //todo: remake this questionable solution - to initialize null parameters, in view point if parser = null - is mistake in client code.
            // solution was done because I didn't want to duplicate initization
            if (parser == null)
                parser = new InterpreterParser();

            if (parser is InterpreterParser interpreterParser)
            {
                string AggregateProperty(IEnumerable<object> x, string y, string z)
                {
                    var returnString = "";
                    foreach (var item in x)
                    {
                        var itemType = item.GetType();
                        var prop = itemType.GetProperty(y);
                        returnString += (string.IsNullOrEmpty(returnString) ? "" : z) + prop?.GetValue(item);
                    }

                    return returnString;
                }

                interpreterParser.Interpreter.SetFunction("AggregateProperty",
                    (Func<IEnumerable<object>, string, string, string>) AggregateProperty);
            }

            this.parser = parser;

            if (enums != null && enums.Any())
                parser.SetReference(enums.Select(ep => new ReferenceType(ep.Key, ep.Value)));
        }

        #endregion

        #region IValueConverter

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(new[] {value}, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (compiledExpression == null)
                if ((compiledExpression =
                        CompileExpression(null, (string) parameter, true, new List<Type> {targetType})) == null)
                    return null;

            if (compiledInversedExpression == null)
                try
                {
                    var resType = compiledExpression.Expression.Type;
                    var param = Expression.Parameter(resType, "Path");
                    compiledInversedExpression =
                        new Inverter(parser).InverseExpression(compiledExpression.Expression, param);
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Binding error: calc converter can't convert back expression " + parameter + ": " +
                                    e.Message);
                }

            if (compiledInversedExpression != null)
                try
                {
                    if (targetType == typeof(bool) && value.GetType() == typeof(Visibility))
                        value = new BoolToVisibilityConverter(FalseToVisibility)
                            .ConvertBack(value, targetType, null, culture);

                    if (value is string && compiledExpression.Expression.Type != value.GetType())
                        value = ParseStringToObject((string) value, compiledExpression.Expression.Type);

                    var source = compiledInversedExpression.Invoke(value);
                    return source;
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Binding error: calc converter can't invoke back expression " + parameter + ": " +
                                    e.Message);
                }

            return null;
        }

        private object ParseStringToObject(string value, Type type)
        {
            var res = System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            return res;
        }

        #endregion

        #region IMultiValueConverter

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
                return null;

            if (sourceValuesTypes == null)
            {
                sourceValuesTypes = GetTypes(values);
            }
            else
            {
                var currentValuesTypes = GetTypes(values);

                if (!sourceValuesTypes.SequenceEqual(currentValuesTypes))
                {
                    sourceValuesTypes = currentValuesTypes;

                    compiledExpression = null;
                    compiledInversedExpression = null;
                }
            }

            if (compiledExpression == null)
                if ((compiledExpression = CompileExpression(values, (string) parameter)) == null)
                    return null;

            try
            {
                var result = compiledExpression.Invoke(values);

                if (!StringFormatDefined)
                {
                    if (targetType == typeof(Visibility))
                        if (!(result is Visibility))
                            result = new BoolToVisibilityConverter(FalseToVisibility)
                                .Convert(result, targetType, null, culture);

                    if (targetType == typeof(string))
                        result = string.Format(CultureInfo.InvariantCulture, "{0}", result);
                }

                return result;
            }
            catch (Exception e)
            {
                Trace.WriteLine("Binding error: calc converter can't invoke expression " +
                                compiledExpression.ExpressionText + ": " + e.Message);
                return null;
            }
        }

        private Type[] GetTypes(object[] values)
        {
            return values.Select(v => v != null ? v.GetType() : null).ToArray();
        }

        private Lambda CompileExpression(object[] values, string expressionTemplate, bool convertBack = false,
            List<Type> targetTypes = null)
        {
            try
            {
                Lambda res = null;

                var needCompile = false;
                // we can't determine value type if value is null
                // so, binding Path = (a == null) ? "a" : "b" is permitted
                if (convertBack)
                    needCompile = true;
                else if (values.Contains(DependencyProperty.UnsetValue))
                    Trace.WriteLine("Binding error: one of source fields is Unset, return null");
                else
                    needCompile = true;

                if (needCompile)
                {
                    var argumentsTypes = convertBack
                        ? targetTypes
                        : sourceValuesTypes.Select(t => t ?? typeof(object)).ToList();
                    res = CompileExpression(argumentsTypes, expressionTemplate);
                }

                return res;
            }
            catch (Exception e)
            {
                Trace.WriteLine("Binding error: calc converter can't convert expression" + expressionTemplate + ": " +
                                e.Message);
                return null;
            }
        }

        private Lambda CompileExpression(List<Type> argumentsTypes, string expressionTemplate)
        {
            var parametersDefinition = new List<Parameter>();

            for (var i = 0; i < argumentsTypes.Count; i++)
            {
                var paramName = GetVariableName(i);

                expressionTemplate = expressionTemplate.Replace("{" + i + "}", paramName);
                parametersDefinition.Add(new Parameter(paramName, argumentsTypes[i]));
            }

            var compiledExpression = parser.Parse(expressionTemplate, parametersDefinition.ToArray());

            return compiledExpression;
        }

        /// <summary>
        ///     Returns string of one char, following from 'a' on i positions (1 -> b, 2 -> c)
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private string GetVariableName(int i)
        {
            //p1 p2 etc
            return string.Format("p{0}", ++i);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}