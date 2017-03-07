using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorAppendVector : VFXOperator
    {
        override public string name { get { return "AppendVector"; } }

        public class InputProperties
        {
            public FloatN a = 0.0f;
            public FloatN b = 0.0f;
        }

        sealed protected override void OnOperatorInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause != InvalidationCause.kUIChanged)
            {
                var newInputSlots = new List<VFXSlot>();
                var size = 0;
                foreach (var slot in inputSlots)
                {
                    var expression = slot.expression;
                    if (expression != null)
                    {
                        size += VFXExpression.TypeToSize(expression.ValueType);
                        newInputSlots.Add(slot);
                    }
                }

                if (inputSlots.All(s => s.HasLink()) && size < 4)
                {
                    AddSlot(VFXSlot.Create(new VFXProperty(typeof(FloatN), "Empty"), VFXSlot.Direction.kInput),false);
                }

                var uselessSlot = newInputSlots.Except(inputSlots).ToArray();
                foreach (var deprecatedSlot in uselessSlot)
                {
                    RemoveSlot(deprecatedSlot,false);
                }
            }

            base.OnOperatorInvalidate(model, cause);
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var allComponent = inputExpression.SelectMany(e => VFXOperatorUtility.ExtractComponents(e))
                                                .Take(4)
                                                .ToArray();
            if (allComponent.Length == 0)
            {
                return new VFXExpression[] { };
            }
            else if (allComponent.Length == 1)
            {
                return allComponent;
            }
            return new[] { new VFXExpressionCombine(allComponent) };
        }
    }
}