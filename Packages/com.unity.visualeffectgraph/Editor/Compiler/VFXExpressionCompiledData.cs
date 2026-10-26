using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    // The expression list and the value descs are captured at compilation time: this data is only valid as long
    // as the expression graph it has been created from is.
    class VFXExpressionCompiledData
    {
        private readonly List<VFXExpression> m_Expressions;
        private readonly VFXExpressionValueContainerDesc[] m_ExpressionValues;
        private readonly Dictionary<VFXExpression, uint> m_CPUExpressionToFlattenedIndex;

        public VFXExpressionCompiledData(List<VFXExpression> expressions,
            VFXExpressionValueContainerDesc[] expressionValues,
            Dictionary<VFXExpression, uint> cpuExpressionToFlattenedIndex)
        {
            m_Expressions = expressions;
            m_ExpressionValues = expressionValues;
            m_CPUExpressionToFlattenedIndex = cpuExpressionToFlattenedIndex;
        }

        public uint FindReducedExpressionIndexFromSlotCPU(VFXSlot slot)
        {
            var targetExpression = slot.GetExpression();
            if (targetExpression == null)
                return uint.MaxValue;

            if (!m_CPUExpressionToFlattenedIndex.TryGetValue(targetExpression, out var index))
                return uint.MaxValue;

            return index;
        }

        private static void SetValueDesc<T>(VFXExpressionValueContainerDesc desc, VFXExpression exp)
        {
            ((VFXExpressionValueContainerDesc<T>)desc).value = exp.Get<T>();
        }

        private static void SetObjectValueDesc<T>(VFXExpressionValueContainerDesc desc, VFXExpression exp)
        {
            ((VFXExpressionObjectValueContainerDesc<T>)desc).entityId = exp.Get<EntityId>();
        }

        public VFXExpressionValueContainerDesc[] UpdateValues(VisualEffectAsset asset)
        {
            // If runtime asset is null or its expression count is 0, it means runtime data generation fails, skip the update value silently
            if (asset == null || VisualEffectAssetUtility.GetExpressionCount(asset) == 0)
                return Array.Empty<VFXExpressionValueContainerDesc>();

            var numFlattenedExpressions = m_Expressions.Count;
            if (VisualEffectAssetUtility.GetExpressionCount(asset) != numFlattenedExpressions)
            {
                Debug.LogError($"Unexpected expression count at {AssetDatabase.GetAssetPath(asset)}, expected: {numFlattenedExpressions} actual: {VisualEffectAssetUtility.GetExpressionCount(asset)}");
                return Array.Empty<VFXExpressionValueContainerDesc>();
            }

            int descIndex = 0;
            for (int i = 0; i < numFlattenedExpressions; ++i)
            {
                var exp = m_Expressions[i];
                if (exp.Is(VFXExpression.Flags.Value))
                {
                    var desc = m_ExpressionValues[descIndex++];
                    if (desc.expressionIndex != i)
                        throw new InvalidOperationException();

                    switch (exp.valueType)
                    {
                        case VFXValueType.Float: SetValueDesc<float>(desc, exp); break;
                        case VFXValueType.Float2: SetValueDesc<Vector2>(desc, exp); break;
                        case VFXValueType.Float3: SetValueDesc<Vector3>(desc, exp); break;
                        case VFXValueType.Float4: SetValueDesc<Vector4>(desc, exp); break;
                        case VFXValueType.Int32: SetValueDesc<int>(desc, exp); break;
                        case VFXValueType.Uint32: SetValueDesc<uint>(desc, exp); break;
                        case VFXValueType.Texture2D:
                        case VFXValueType.Texture2DArray:
                        case VFXValueType.Texture3D:
                        case VFXValueType.TextureCube:
                        case VFXValueType.TextureCubeArray:
                            SetObjectValueDesc<Texture>(desc, exp);
                            break;
                        case VFXValueType.CameraBuffer: SetObjectValueDesc<Texture>(desc, exp); break;
                        case VFXValueType.Matrix4x4: SetValueDesc<Matrix4x4>(desc, exp); break;
                        case VFXValueType.Curve: SetValueDesc<AnimationCurve>(desc, exp); break;
                        case VFXValueType.ColorGradient: SetValueDesc<Gradient>(desc, exp); break;
                        case VFXValueType.Mesh: SetObjectValueDesc<Mesh>(desc, exp); break;
                        case VFXValueType.SkinnedMeshRenderer: SetObjectValueDesc<SkinnedMeshRenderer>(desc, exp); break;
                        case VFXValueType.Boolean: SetValueDesc<bool>(desc, exp); break;
                        case VFXValueType.Buffer: break; //The GraphicsBuffer type isn't serialized
                        default: throw new InvalidOperationException("Invalid type");
                    }
                }
            }

            VisualEffectAssetUtility.SetValueSheet(asset, m_ExpressionValues);
            return m_ExpressionValues;
        }
    }
}
