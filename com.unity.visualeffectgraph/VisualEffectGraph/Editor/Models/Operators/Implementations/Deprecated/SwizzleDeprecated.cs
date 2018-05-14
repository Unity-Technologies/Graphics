using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class SwizzleDeprecated : VFXOperatorUnaryFloatOperation
    {
        override public string libraryName { get { return "Swizzle (deprecated)"; } }
        override public string name { get { return "Swizzle." + mask + " (deprecated)"; } }

        [VFXSetting, Regex("[^w-zW-Z]", 4)]
        public string mask = "xyzw";

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var p in base.inputProperties)
                {
                    if (p.property.type == typeof(FloatN))
                        yield return new VFXPropertyWithValue(new VFXProperty(p.property.type, string.Empty), p.value); // remove name from FloatN slot
                    else
                        yield return p;
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                Type slotType = null;
                switch (mask.Length)
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

        private static int CharToComponentIndex(char componentChar)
        {
            switch (componentChar)
            {
                default:
                case 'x': return 0;
                case 'y': return 1;
                case 'z': return 2;
                case 'w': return 3;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var inputComponents = (inputExpression.Length > 0) ? VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray() : new VFXExpression[0];

            var componentStack = new Stack<VFXExpression>();
            int outputSize = mask.Length;
            for (int iComponent = 0; iComponent < outputSize; iComponent++)
            {
                char componentChar = char.ToLower(mask[iComponent]);
                int currentComponent = Math.Min(CharToComponentIndex(componentChar), inputComponents.Length - 1);
                componentStack.Push(inputComponents[currentComponent]);
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

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Swizzle));
        }
    }
}
