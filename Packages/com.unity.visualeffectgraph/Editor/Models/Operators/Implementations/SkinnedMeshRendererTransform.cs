using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    class SkinnedMeshRendererTransformProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                "Get Skinned Mesh Local Root Transform",
                "Sampling",
                typeof(SkinnedMeshRendererTransform),
                new[] {new KeyValuePair<string, object>("transform", VFXSkinnedTransform.LocalRootBoneTransform)}
            );

            yield return new Variant(
                "Get Skinned Mesh World Root Transform",
                "Sampling",
                typeof(SkinnedMeshRendererTransform),
                new[] {new KeyValuePair<string, object>("transform", VFXSkinnedTransform.WorldRootBoneTransform)}
            );
        }
    }

    [VFXHelpURL("Operator-SampleMesh")]
    [VFXInfo(variantProvider = typeof(SkinnedMeshRendererTransformProvider))]
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

        public sealed override VFXSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            if (slot.spaceable)
            {
                switch (transform)
                {
                    case VFXSkinnedTransform.LocalRootBoneTransform:
                        return VFXSpace.Local;
                    case VFXSkinnedTransform.WorldRootBoneTransform:
                        return VFXSpace.World;
                }
            }
            return VFXSpace.None;
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionRootBoneTransformFromSkinnedMeshRenderer(inputExpression[0], transform, frame) };
        }
    }
}
