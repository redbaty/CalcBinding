﻿using System.Collections.Generic;
using DynamicExpresso;

namespace CalcBinding
{
    public interface IExpressionParser
    {
        Lambda Parse(string expressionText, params Parameter[] parameters);

        void SetReference(IEnumerable<ReferenceType> referencedTypes);
    }
}