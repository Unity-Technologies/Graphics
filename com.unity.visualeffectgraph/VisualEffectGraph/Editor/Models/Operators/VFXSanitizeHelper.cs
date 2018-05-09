using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    static class SanitizeHelper
    {
        public static void SanitizeToOperatorNew(VFXOperator input, Type outputType)
        {
            if (!input.inputSlots.Where(o => o.property.type == typeof(FloatN)).Any())
            {
                Debug.LogError("SanitizeToOperatorNew is dedicated to operator using FloatN " + input);
                return;
            }

            var realTypeAndValue = input.inputSlots.Select(o =>
                {
                    Type type = null;
                    object value = null;
                    bool wasFloatN = o.property.type == typeof(FloatN);
                    if (!wasFloatN)
                    {
                        type = o.property.type;
                        value = o.HasLink() ? null : o.value;
                    }
                    else
                    {
                        if (o.HasLink())
                        {
                            type = o.LinkedSlots.First().property.type;
                        }
                        else
                        {
                            var floatN = (FloatN)o.value;
                            type = floatN.GetCurrentType();
                            value = (FloatN)o.value;
                        }
                    }
                    return new
                    {
                        type = type,
                        value = value,
                        wasFloatN = wasFloatN
                    };
                }).ToArray();

            var output = ScriptableObject.CreateInstance(outputType) as VFXOperator;
            if (output is IVFXOperatorUniform)
            {
                var uniform = output as IVFXOperatorUniform;
                var minType = realTypeAndValue.Where(o => o.wasFloatN).Select(o => o.type)
                    .OrderBy(o => VFXExpression.TypeToSize(VFXExpression.GetVFXValueTypeFromType(o)))
                    .First();
                //ignore int/uint while sanitizing
                if (minType == typeof(int) || minType == typeof(uint))
                    minType = typeof(float);
                uniform.SetOperandType(minType);
            }
            else if (output is VFXOperatorNumericCascadedUnifiedNew)
            {
                var cascaded = output as VFXOperatorNumericCascadedUnifiedNew;
                while (cascaded.inputSlots.Count < realTypeAndValue.Length)
                {
                    cascaded.AddOperand();
                }

                for (int i = 0; i < realTypeAndValue.Length; ++i)
                {
                    cascaded.SetOperandType(i, realTypeAndValue[i].type);
                }
            }
            else
            {
                Debug.LogError("Unable to determine what kind of " + output.GetType());
                return;
            }

            for (int i = 0; i < realTypeAndValue.Length; ++i)
            {
                var current = realTypeAndValue[i];
                if (!current.wasFloatN)
                {
                    VFXSlot.TransferLinksAndValue(output.inputSlots[i], input.inputSlots[i], true);
                }
                else
                {
                    if (current.value == null)
                    {
                        VFXSlot.TransferLinks(output.inputSlots[i], input.inputSlots[i], true);
                    }
                    else
                    {
                        object orgValue = null;
                        var floatN = (FloatN)current.value;
                        var targetType = output.inputSlots[i].property.type;
                        if (targetType == typeof(float))
                        {
                            orgValue = (float)floatN;
                        }
                        else if (targetType == typeof(Vector2))
                        {
                            orgValue = (Vector2)floatN;
                        }
                        else if (targetType == typeof(Vector3))
                        {
                            orgValue = (Vector3)floatN;
                        }
                        else if (targetType == typeof(Vector4))
                        {
                            orgValue = (Vector4)floatN;
                        }
                        else
                        {
                            Debug.LogError("Unexpected type of FloatN while sanitizing : " + targetType);
                            return;
                        }
                        output.inputSlots[i].value = orgValue;
                    }
                }
            }

            for (int i = 0; i < input.outputSlots.Count; ++i)
            {
                VFXSlot.TransferLinks(output.outputSlots[i], input.outputSlots[i], true);
            }

            VFXModel.ReplaceModel(output, input);
        }
    }
}
