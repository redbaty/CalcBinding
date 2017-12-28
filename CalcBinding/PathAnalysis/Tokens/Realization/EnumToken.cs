using System;

namespace CalcBinding.PathAnalysis
{
    public class EnumToken : PathToken
    {
        public EnumToken(int start, int end, string @namespace, Type @enum, string enumMember)
            : base(start, end)
        {
            Enum = @enum;
            EnumMember = enumMember;
            Namespace = @namespace;

            Id = new PathTokenId(PathTokenType.Enum, string.Format("{0}:{1}.{2}", Namespace, @enum.Name, EnumMember));
        }

        public Type Enum { get; }
        public string EnumMember { get; }
        public string Namespace { get; }
        public override PathTokenId Id { get; }
    }
}