namespace CalcBinding.PathAnalysis
{
    public class MathToken : PathToken
    {
        public MathToken(int start, int end, string mathMember)
            : base(start, end)
        {
            MathMember = mathMember;
            Id = new PathTokenId(PathTokenType.Math, string.Join(".", "Math", MathMember));
        }

        public string MathMember { get; }
        public override PathTokenId Id { get; }
    }
}