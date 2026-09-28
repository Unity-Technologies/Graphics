using System;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    /// <summary>
    /// A texture bound to a task.
    /// </summary>
    class VfxTextureData : ResourceValueData
    {
        public VFXValueType ValueType { get; }

        public override Type Type => VFXExpression.TypeToType(ValueType);

        public VfxTextureData(VFXValueType valueType)
        {
            Debug.Assert(VFXExpression.IsTexture(valueType));
            ValueType = valueType;
        }
    }

    /// <summary>
    /// A graphics buffer bound to a task, be it an actual <see cref="GraphicsBuffer"/> or the vertex buffer a
    /// mesh was resolved to.
    /// </summary>
    class VfxGraphicsBufferData : ResourceValueData
    {
        public VFXValueType ValueType { get; }

        // VFXExpression.TypeToType has no case for Buffer, so the mapping is spelled out here.
        public override Type Type => ValueType switch
        {
            VFXValueType.Mesh => typeof(Mesh),
            VFXValueType.SkinnedMeshRenderer => typeof(SkinnedMeshRenderer),
            _ => typeof(GraphicsBuffer)
        };

        public VfxGraphicsBufferData(VFXValueType valueType)
        {
            ValueType = valueType;
        }
    }
}
