namespace CalcBinding.PathAnalysis.Tokens.Abstract.Help
{
    public class PathTokenId
    {
        public PathTokenId(PathTokenType pathType, string value)
        {
            PathType = pathType;
            Value = value;
        }

        public PathTokenType PathType { get; }
        public string Value { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is PathTokenId o))
                return false;

            return o.PathType == PathType && o.Value == Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ PathType.GetHashCode();
        }
    }
}