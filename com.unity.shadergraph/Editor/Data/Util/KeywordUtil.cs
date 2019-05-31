using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class KeywordUtil
    {
        public static ConcreteSlotValueType ToConcreteSlotValueType(this KeywordType keywordType)
        {
            switch(keywordType)
            {
                case KeywordType.Boolean:
                    return ConcreteSlotValueType.Boolean;
                case KeywordType.Enum:
                    return ConcreteSlotValueType.Vector1;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
