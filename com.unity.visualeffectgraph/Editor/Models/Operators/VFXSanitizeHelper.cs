using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    static class SanitizeHelper
    {
        public static void ToOperatorWithoutFloatN(VFXOperator input, Type outputType)
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

            var output = ScriptableObject.CreateInstance(outputType) as VFXOperatorDynamicOperand;

            //Transfer settings
            var settingsIn = input.GetSettings(true).Where(o => o.FieldType != typeof(SerializableType));
            var settingsOut = output.GetSettings(true).Where(o => o.FieldType != typeof(SerializableType));
            if (settingsIn.Count() != settingsOut.Count())
            {
                Debug.LogError("Settings has been changed, unable to automatically transfer them");
                return;
            }

            var itSettingIn = settingsIn.GetEnumerator();
            var itSettingOut = settingsOut.GetEnumerator();
            while (itSettingIn.MoveNext() && itSettingOut.MoveNext())
            {
                if (itSettingIn.Current.Name != itSettingOut.Current.Name.Replace("m_", string.Empty))
                {
                    Debug.Log(string.Format("Unexpected settings : {0} vs {1}", itSettingIn.Current.Name, itSettingOut.Current.Name));
                    return;
                }
                output.SetSettingValue(itSettingOut.Current.Name, itSettingIn.Current.GetValue(input));
            }

            //Apply dynamic type behavior
            if (output is IVFXOperatorUniform)
            {
                var uniform = output as IVFXOperatorUniform;
                var maxType = realTypeAndValue.Where(o => o.wasFloatN && o.type != typeof(FloatN))
                    .Select(o => o.type)
                    .OrderBy(o => VFXExpression.TypeToSize(VFXExpression.GetVFXValueTypeFromType(o)))
                    .LastOrDefault();

                if (maxType != null)
                {
                    //ignore int/uint while sanitizing
                    if (maxType == typeof(int) || maxType == typeof(uint))
                        maxType = typeof(float);

                    maxType = output.GetBestAffinityType(maxType);
                    uniform.SetOperandType(maxType);
                }
            }
            else if (output is VFXOperatorNumericCascadedUnified)
            {
                var cascaded = output as VFXOperatorNumericCascadedUnified;
                //Remove all empty last operand (has influence of output type for append)
                realTypeAndValue = realTypeAndValue.Reverse().SkipWhile(o => o.type == typeof(FloatN)).Reverse().ToArray();

                while (cascaded.inputSlots.Count < realTypeAndValue.Length)
                {
                    cascaded.AddOperand();
                }

                for (int i = 0; i < realTypeAndValue.Length; ++i)
                {
                    var currentType = cascaded.GetBestAffinityType(realTypeAndValue[i].type);
                    if (currentType != null)
                    {
                        cascaded.SetOperandType(i, currentType);
                    }
                }
            }
            else if (output is VFXOperatorNumericUnified)
            {
                var unified = output as VFXOperatorNumericUnified;
                var slotIndiceThatShouldHaveSameType = Enumerable.Empty<int>();
                var slotIndiceThatCanBeScale = Enumerable.Empty<int>();
                if (output is IVFXOperatorNumericUnifiedConstrained)
                {
                    slotIndiceThatShouldHaveSameType = (output as IVFXOperatorNumericUnifiedConstrained).slotIndicesThatMustHaveSameType;
                    slotIndiceThatCanBeScale = (output as IVFXOperatorNumericUnifiedConstrained).slotIndicesThatCanBeScalar;
                }

                Type maxTypeConstrained = null;
                if (slotIndiceThatShouldHaveSameType.Any())
                {
                    var typeConstrained = slotIndiceThatShouldHaveSameType.Select(i =>
                    {
                        if (i < realTypeAndValue.Length)
                        {
                            return realTypeAndValue[i].type;
                        }
                        return (Type)null;
                    }).Where(o => o != null);

                    if (!typeConstrained.Any())
                    {
                        Debug.LogError("Unexpected behavior while sanitizing to unified constrained");
                        return;
                    }
                    maxTypeConstrained = typeConstrained.OrderBy(o => VFXExpression.TypeToSize(VFXExpression.GetVFXValueTypeFromType(o))).LastOrDefault();
                }

                for (int i = 0; i < realTypeAndValue.Length; ++i)
                {
                    var currentType = realTypeAndValue[i].type;
                    if (slotIndiceThatShouldHaveSameType.Contains(i)
                        && !(slotIndiceThatCanBeScale.Contains(i) && VFXExpression.GetMatchingScalar(currentType) == currentType))
                    {
                        currentType = maxTypeConstrained;
                    }
                    currentType = output.GetBestAffinityType(currentType);
                    if (currentType != null)
                    {
                        unified.SetOperandType(i, currentType);
                    }
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
                    VFXSlot.CopyLinksAndValue(output.inputSlots[i], input.inputSlots[i], true);
                }
                else
                {
                    if (current.value == null)
                    {
                        var slotDst = output.inputSlots[i];
                        var slotSrc = input.inputSlots[i].LinkedSlots.First();
                        if (slotSrc.CanLink(slotDst))
                        {
                            //Main path (most common case)
                            VFXSlot.CopyLinks(output.inputSlots[i], input.inputSlots[i], true);
                        }
                        else
                        {
                            //Trying to connect by subslot (e.g. : Vector4 to Vector3)
                            var itSubSlotSrc = slotSrc.children.GetEnumerator();
                            var itSubSlotDst = slotDst.children.GetEnumerator();
                            while (itSubSlotDst.MoveNext() && itSubSlotSrc.MoveNext())
                            {
                                itSubSlotSrc.Current.Link(itSubSlotDst.Current);
                            }
                        }
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
                VFXSlot.CopyLinks(output.outputSlots[i], input.outputSlots[i], true);
            }
            VFXModel.ReplaceModel(output, input);
        }
    }
}
