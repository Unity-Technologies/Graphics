using System.Collections.Generic;
using System.Linq;
using Data.Util;

namespace UnityEditor.ShaderGraph.Internal
{
    interface IConditionalShaderString
    {
        string value { get; }
        FieldCondition[] fieldConditions { get; }
    }

    static class IConditionalShaderStringExtensions
    {
        public static bool TestActive(this IConditionalShaderString conditionalShaderString, ActiveFields fields, out string value)
        {
            // Test FieldCondition against current active Fields
            bool TestFieldCondition(FieldCondition fieldCondition)
            {
                // Required active field is not active
                if(fieldCondition.condition == true && !fields.baseInstance.Contains(fieldCondition.field))
                    return false;

                // Required non-active field is active
                else if(fieldCondition.condition == false && fields.baseInstance.Contains(fieldCondition.field))
                    return false;

                return true;
            }

            // No FieldConditions
            if(conditionalShaderString.fieldConditions == null)
            {
                value = conditionalShaderString.value;
                return true;
            }

            // One or more FieldConditions failed
            if(conditionalShaderString.fieldConditions.Where(x => !TestFieldCondition(x)).Any())
            {
                value = null;
                return false;
            }

            // All FieldConditions passed
            value = conditionalShaderString.value;
            return true;
        }
    }
}
