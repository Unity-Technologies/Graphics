using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    class SkinnedMeshRendererTransformProvider : VariantProvider
    {
        protected sealed override Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "transform", Enum.GetValues(typeof(VFXSkinnedTransform)).Cast<object>().ToArray() },
                };
            }
        }
    }

    [VFXHelpURL("Operator-SampleMesh")]
    [VFXInfo(category = "Sampling", variantProvider = typeof(SkinnedMeshRendererTransformProvider))]
    class SkinnedMeshRendererTransform : VFXOperator
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        private VFXSkinnedTransform transform;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        private VFXSkinnedMeshFrame frame = VFXSkinnedMeshFrame.Current;

        public sealed override string name
        {
            get
            {
                switch (transform)
                {
                    case VFXSkinnedTransform.LocalRootBoneTransform:
                        return "Get Skinned Mesh Local Root Transform";
                    case VFXSkinnedTransform.WorldRootBoneTransform:
                        return "Get Skinned Mesh World Root Transform";
                }
                throw new NotImplementedException();
            }
        }

        public class InputProperties
        {
            [Tooltip("Specifies the Skinned Mesh Renderer component to retrieve the transform from. The Skinned Mesh Renderer has to be an exposed entry.")]
            public SkinnedMeshRenderer skinnedMesh = null;
        }

        public class OutputProperties
        {
            public Transform o = new Transform();
        }

        public sealed override VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            if (slot.spaceable)
            {
                switch (transform)
                {
                    case VFXSkinnedTransform.LocalRootBoneTransform:
                        return VFXCoordinateSpace.Local;
                    case VFXSkinnedTransform.WorldRootBoneTransform:
                        return VFXCoordinateSpace.World;
                }
            }
            return (VFXCoordinateSpace)int.MaxValue;
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionRootBoneTransformFromSkinnedMeshRenderer(inputExpression[0], transform, frame) };
        }
    }
}
