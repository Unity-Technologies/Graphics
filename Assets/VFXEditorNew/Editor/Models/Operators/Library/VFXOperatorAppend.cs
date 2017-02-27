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

        public class Properties
        {
            public FloatN a = 0.0f;
            public FloatN b = 0.0f;
        }

        sealed protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            var newInputSlots = new List<VFXMitoSlotInput>();
            var size = 0;
            foreach (var slot in InputSlots)
            {
                var expression = slot.expression;
                if (expression != null)
                {
                    size += VFXExpression.TypeToSize(expression.ValueType);
                    newInputSlots.Add(slot);
                }
            }

            if (newInputSlots.All(s => s.parent != null) && size < 4)
            {
                newInputSlots.Add(new VFXMitoSlotInput(new FloatN()));
            }

            InputSlots = newInputSlots.ToArray();
            base.OnInvalidate(model, cause);
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var allComponent = inputExpression.SelectMany(e => VFXOperatorUtility.ExtractComponents(e));
            return new[] { new VFXExpressionCombine(allComponent.Take(4).ToArray()) };
        }
    }
}