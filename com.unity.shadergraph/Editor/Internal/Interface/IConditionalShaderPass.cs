using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph.Internal
{
    interface IConditionalShaderPass
    {
        ShaderPass shaderPass { get; }
        FieldCondition[] fieldConditions { get; }
    }

    static class IConditionalShaderPassExtensions
    {
        public static bool TestActive(this IConditionalShaderPass conditionalShaderPass, List<IField> fields)
        {
            // Test FieldCondition against current active Fields
            bool TestFieldCondition(FieldCondition fieldCondition)
            {
                // Required active field is not active
                if(fieldCondition.condition == true && !fields.Contains(fieldCondition.field))
                    return false;

                // Required non-active field is active
                else if(fieldCondition.condition == false && fields.Contains(fieldCondition.field))
                    return false;

                return true;
            }

            // No FieldConditions is always true
            if(conditionalShaderPass.fieldConditions == null)
            {
                return true;
            }

            // One or more FieldConditions failed
            if(conditionalShaderPass.fieldConditions.Where(x => !TestFieldCondition(x)).Any())
            {
                return false;
            }

            // All FieldConditions passed
            return true;
        }
    }
}
