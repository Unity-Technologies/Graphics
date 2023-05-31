using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-GetTextureDimensions")]
    [VFXInfo(category = "Sampling")]
    class TextureDimensions : VFXOperatorDynamicType
    {
        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetOperandType(), "tex", new TooltipAttribute("Sets the texture to get dimensions from.")));
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "width", new TooltipAttribute("Outputs the width of the texture.")));
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "height", new TooltipAttribute("Outputs the height of the texture.")));

                if (GetOperandType() == typeof(Texture3D))
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "depth", new TooltipAttribute("Outputs the depth of the texture.")));
                else if (GetOperandType() == typeof(Texture2DArray) || GetOperandType() == typeof(CubemapArray))
                    yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "count", new TooltipAttribute("Outputs the texture count of the texture array.")));
            }
        }

        override public string name { get { return "Get " + GetOperandType().Name + " Dimensions"; } }
        override public string libraryName { get { return "Get Texture Dimensions"; } }

        public override IEnumerable<Type> validTypes => new[]
        {
            typeof(Texture2D),
            typeof(Texture3D),
            typeof(Texture2DArray),
            typeof(Cubemap),
            typeof(CubemapArray)
        };

        protected override Type defaultValueType => typeof(Texture2D);

        public override IEnumerable<int> staticSlotIndex => Enumerable.Empty<int>();

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (GetOperandType() == typeof(Texture3D)
                || GetOperandType() == typeof(Texture2DArray)
                || GetOperandType() == typeof(CubemapArray))
            {
                return new VFXExpression[]
                {
                    new VFXExpressionTextureWidth(inputExpression[0]),
                    new VFXExpressionTextureHeight(inputExpression[0]),
                    new VFXExpressionTextureDepth(inputExpression[0]),
                };
            }
            else
            {
                return new VFXExpression[]
                {
                    new VFXExpressionTextureWidth(inputExpression[0]),
                    new VFXExpressionTextureHeight(inputExpression[0]),
                };
            }
        }
    }
}
