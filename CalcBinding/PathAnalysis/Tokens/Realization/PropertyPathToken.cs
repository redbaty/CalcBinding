using System.Collections.Generic;
using System.Linq;

namespace CalcBinding.PathAnalysis
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