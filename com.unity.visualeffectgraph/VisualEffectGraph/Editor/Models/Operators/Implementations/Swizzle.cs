using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Misc")]
    class Swizzle : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Swizzle"; } }

        [VFXSetting, Regex("[^w-zW-Z]", 4)]
        public string mask = "xyzw";

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                Type slotType = null;
                switch (GetMaskSize())
                {
                    case 1: slotType = typeof(float); break;
                    case 2: slotType = typeof(Vector2); break;
                    case 3: slotType = typeof(Vector3); break;
                    case 4: slotType = typeof(Vector4); break;
                    default: break;
                }

                if (slotType != null)
                    yield return new VFXPropertyWithValue(new VFXProperty(slotType, "o"));
            }
        }

        private int GetMaskSize()
        {
            return Math.Min(4, mask.Length);
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var inputComponents = VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray();

            var componentStack = new Stack<VFXExpression>();
            int outputSize = GetMaskSize();
            for (int iComponent = 0; iComponent < outputSize; iComponent++)
            {
                char componentChar = char.ToLower(mask[iComponent]);
                int currentComponent = Math.Min((int)(componentChar - 'x'), inputComponents.Length - 1);
                componentStack.Push(inputComponents[(int)currentComponent]);
            }

            VFXExpression finalExpression = null;
            if (componentStack.Count == 1)
            {
                finalExpression = componentStack.Pop();
            }
            else
            {
                finalExpression = new VFXExpressionCombine(componentStack.Reverse().ToArray());
            }
            return new[] { finalExpression };
        }
    }
}
