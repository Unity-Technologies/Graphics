using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-LoadTexture2D")]
    [VFXInfo(name = "Load Texture2D", category = "Sampling")]
    class LoadTexture2D : VFXOperator
    {
        override public string name { get { return "Load Texture2D"; } }

        public class InputProperties
        {
            [Tooltip("Sets the texture to load from.")]
            public Texture2D texture = null;
            [Tooltip("Sets the x coordinate for the texel to load.")]
            public uint x = 0;
            [Tooltip("Sets the y coordinate for the texel to load.")]
            public uint y = 0;
            [Tooltip("Sets the mip level to load from.")]
            public uint mipLevel = 0;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the sampled value from the texture at the specified coordinate.")]
            public Vector4 s = Vector4.zero;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var location = new VFXExpressionCombine(
                new VFXExpressionCastUintToFloat(inputExpression[1]),
                new VFXExpressionCastUintToFloat(inputExpression[2]),
                new VFXExpressionCastUintToFloat(inputExpression[3]));
            return new[] { new VFXExpressionLoadTexture2D(inputExpression[0], location) };
        }
    }
}
