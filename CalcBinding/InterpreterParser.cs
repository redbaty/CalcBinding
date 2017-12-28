using System.Collections.Generic;
using DynamicExpresso;

namespace CalcBinding
{
    public class InterpreterParser : IExpressionParser
    {
        public InterpreterParser()
        {
            Interpreter = new Interpreter();
        }

        public Interpreter Interpreter { get; }

        public Lambda Parse(string expressionText, Parameter[] parameters)
        {
            return Interpreter.Parse(expressionText, parameters);
        }

        public void SetReference(IEnumerable<ReferenceType> referencedTypes)
        {
            Interpreter.Reference(referencedTypes);
        }
    }
}