using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Builds the data description an expression's value is bound as.
    /// </summary>
    static class VfxValueDataFactory
    {
        /// <summary>
        /// A resource description for a texture or a buffer, a plain value for anything else.
        /// </summary>
        public static ValueData Create(VFXValueType valueType)
        {
            if (VFXExpression.IsTexture(valueType))
                return new VfxTextureData(valueType);
            if (IsBuffer(valueType))
                return new VfxGraphicsBufferData(valueType);
            return ValueData.Create(VFXExpression.TypeToType(valueType));
        }

        static bool IsBuffer(VFXValueType valueType)
            => VFXExpression.IsBufferOnGPU(valueType) || valueType == VFXValueType.SkinnedMeshRenderer;
    }
}
