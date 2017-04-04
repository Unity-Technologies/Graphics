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
                var emptySlot = inputSlots.Where(s => s.GetExpression() == null).ToArray();
                foreach (var slot in emptySlot)
                {
                    RemoveSlot(slot, false);
                }

                var size = inputSlots.Sum(s => VFXExpression.TypeToSize(s.GetExpression().ValueType));
                if (inputSlots.All(s => s.HasLink()) && size < 4)
                {
                    AddSlot(VFXSlot.Create(new VFXProperty(typeof(FloatN), "Empty"), VFXSlot.Direction.kInput), false);
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