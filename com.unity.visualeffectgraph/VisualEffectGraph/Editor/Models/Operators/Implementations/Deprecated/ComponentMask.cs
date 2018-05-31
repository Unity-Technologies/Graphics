using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    //[VFXInfo(category = "Utility")] DEPRECATED
    class ComponentMask : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "DEPRECATED - ComponentMask"; } }

        public enum Component
        {
            X = 0,
            Y = 1,
            Z = 2,
            W = 3,
            None = -1,
        }

        [VFXSetting]
        public Component x = Component.X;
        [VFXSetting]
        public Component y = Component.Y;
        [VFXSetting]
        public Component z = Component.Z;
        [VFXSetting]
        public Component w = Component.W;

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                var mask = new Component[4] { x, y, z, w };
                int componentCount = GetMaskSize(mask);

                const string outputName = "o";
                Type slotType = null;
                switch (componentCount)
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

        private static int GetMaskSize(Component[] mask)
        {
            int maskSize = Math.Min(4, mask.Length);
            while (maskSize > 1 && mask[maskSize - 1] == Component.None) --maskSize;
            return maskSize;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var mask = new Component[4] { x, y, z, w };
            // var mask = new Component[4] { Component.X, Component.Y, Component.Z, Component.W };
            int maskSize = 4;
            while (maskSize > 1 && mask[maskSize - 1] == Component.None) --maskSize;

            var inputComponents = VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray();

            var componentStack = new Stack<VFXExpression>();
            int outputSize = GetMaskSize(mask);
            for (int iComponent = 0; iComponent < outputSize; iComponent++)
            {
                Component currentComponent = mask[iComponent];
                if (currentComponent != Component.None && (int)currentComponent < inputComponents.Length)
                {
                    componentStack.Push(inputComponents[(int)currentComponent]);
                }
                else
                {
                    componentStack.Push(VFXValue<float>.Default);
                }
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

        public override void Sanitize()
        {
            Debug.Log("Sanitizing Graph: Automatically replace ComponentMask with Swizzle");

            var swizzle = CreateInstance<SwizzleDeprecated>();
            var mask = new Component[4] { x, y, z, w };

            string result = "";
            foreach (var m in mask)
            {
                if (m != Component.None)
                {
                    switch (x)
                    {
                        case Component.X: result += 'x'; break;
                        case Component.Y: result += 'y'; break;
                        case Component.Z: result += 'z'; break;
                        case Component.W: result += 'w'; break;
                        default:
                            break;
                    }
                }
            }

            swizzle.SetSettingValue("mask", result);

            // Transfer links
            VFXSlot.CopyLinksAndValue(swizzle.GetInputSlot(0), GetInputSlot(0), true);
            VFXSlot.CopyLinksAndValue(swizzle.GetOutputSlot(0), GetOutputSlot(0), true);

            ReplaceModel(swizzle, this);
            swizzle.Sanitize();
        }
    }
}
