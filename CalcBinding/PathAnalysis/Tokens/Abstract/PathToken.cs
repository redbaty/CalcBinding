namespace CalcBinding.PathAnalysis
{
    public abstract class PathToken
    {
        protected PathToken(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Start { get; }

        public int End { get; }

        public abstract PathTokenId Id { get; }
    }
}