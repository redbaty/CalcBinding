using System.Collections.Generic;
using CalcBinding.PathAnalysis.Tokens.Abstract.Help;

namespace CalcBinding.PathAnalysis.Tokens.Realization
{
    public class StaticPropertyPathToken : PropertyPathToken
    {
        public StaticPropertyPathToken(int start, int end, string @namespace, string @class,
            IEnumerable<string> properties)
            : base(start, end, properties)
        {
            Class = @class;
            Namespace = @namespace;
            Id = new PathTokenId(PathTokenType.StaticProperty,
                string.Format("{0}:{1}.{2}", Namespace, Class, string.Join(".", Properties)));
        }

        public string Class { get; }
        public string Namespace { get; }
        public override PathTokenId Id { get; }
    }
}