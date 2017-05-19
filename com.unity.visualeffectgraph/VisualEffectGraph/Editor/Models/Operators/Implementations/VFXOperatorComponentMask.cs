using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorComponentMask : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "ComponentMask"; } }

        public class Settings
        {
            public string mask = "zyx";
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var currentSettings = settings as Settings;

            var mask = currentSettings.mask;
            var inputComponents = VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray();

            var componentStack = new Stack<VFXExpression>();
            for (int iComponent = 0; iComponent < mask.Length; iComponent++)
            {
                var iChannelIndex = -1;
                switch (mask[iComponent])
                {
                    case 'x': case 'r': iChannelIndex = 0; break;
                    case 'y': case 'g': iChannelIndex = 1; break;
                    case 'z': case 'b': iChannelIndex = 2; break;
                    case 'w': case 'a': iChannelIndex = 3; break;
                    default: throw new Exception("unexpected component name");
                }

                if (iChannelIndex < inputComponents.Length)
                {
                    componentStack.Push(inputComponents[iChannelIndex]);
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
                finalExpression = new VFXExpressionCombine(componentStack.ToArray());
            }
            return new[] { finalExpression };
        }
    }
}
