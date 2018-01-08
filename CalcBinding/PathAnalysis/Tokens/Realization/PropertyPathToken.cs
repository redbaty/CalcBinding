using System.Collections.Generic;
using System.Linq;
using CalcBinding.PathAnalysis.Tokens.Abstract;
using CalcBinding.PathAnalysis.Tokens.Abstract.Help;

namespace CalcBinding.PathAnalysis.Tokens.Realization
{
    public class PropertyPathToken : PathToken
    {
        public PropertyPathToken(int start, int end, IEnumerable<string> properties)
            : base(start, end)
        {
            Properties = properties.ToList();
            Id = new PathTokenId(PathTokenType.Property, string.Join(".", Properties));
        }

        public IEnumerable<string> Properties { get; }
        public override PathTokenId Id { get; }
    }
}