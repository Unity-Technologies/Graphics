using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class AppendVectorDeprecated : VFXOperator
    {
        override public string name { get { return "AppendVector (deprecated)"; } }

        private int outputComponentCount
        {
            get
            {
                return Math.Min(4, inputSlots.Sum(s => VFXTypeUtility.GetComponentCount(s)));
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                int totalComponentCount = 0;
                int nbNeededSlots = 1;
                var currentSlots = inputSlots.ToList();
                for (int i = 0; i < currentSlots.Count; ++i)
                {
                    var slotComponentCount = VFXTypeUtility.GetComponentCount(currentSlots[i]);
                    totalComponentCount += slotComponentCount;
                    if (slotComponentCount > 0 && totalComponentCount < 4)
                        ++nbNeededSlots;
                    if (totalComponentCount >= 4)
                        break;
                }

                for (int i = 0; i < nbNeededSlots; ++i)
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(FloatN), ((char)((int)'a' + i)).ToString()), null);
                // if (totalComponentCount < 4)
                //     yield return new VFXPropertyWithValue(new VFXProperty(typeof(FloatN), ((char)((int)'a' + nbNeededSlots)).ToString()), null); // Add an empty slot for additonal connection
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                const string outputName = "o";
                Type slotType = null;
                switch (outputComponentCount)
                {
                    case 1: slotType = typeof(float); break;
                    case 2: slotType = typeof(Vector2); break;
                    case 3: slotType = typeof(Vector3); break;
                    case 4: slotType = typeof(Vector4); break;
                    default: break;
                }

                if (slotType != null)
                    yield return new VFXPropertyWithValue(new VFXProperty(slotType, outputName));
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            int nbComponents = outputComponentCount;
            var allComponent = inputExpression.SelectMany(e => VFXOperatorUtility.ExtractComponents(e))
                .Take(outputComponentCount)
                .ToArray();

            if (allComponent.Length == 0)
            {
                return new VFXExpression[] {};
            }
            else if (allComponent.Length == 1)
            {
                return allComponent;
            }
            return new[] { new VFXExpressionCombine(allComponent) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(AppendVector));
        }
    }
}
