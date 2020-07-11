﻿using System.Linq;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    static class IConditionalExtensions
    {
        public static bool TestActive(this IConditional conditional, ActiveFields fields)
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
            if(conditional.fieldConditions == null)
                return true;

            // One or more FieldConditions failed
            if(conditional.fieldConditions.Where(x => !TestFieldCondition(x)).Any())
                return false;

            // All FieldConditions passed
            return true;
        }
    }
}
