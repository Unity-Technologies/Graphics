using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.VFX.VFXAbstractRenderedOutput;

namespace UnityEditor.VFX
{
    class VFXSRPSubOutput : VFXModel
    {
        public void Init(VFXAbstractRenderedOutput owner)
        {
            if (m_Owner != null)
                throw new InvalidOperationException("Owner is already set");
            if (owner == null)
                throw new NullReferenceException("Owner cannot be null");

            m_Owner = owner;
        }

        private VFXAbstractRenderedOutput m_Owner;
        public VFXAbstractRenderedOutput owner => m_Owner;

        // Caps
        public virtual bool supportsExposure { get { return false; } }
        public virtual bool supportsMotionVector { get { return false; } }
        public virtual bool supportsExcludeFromTUAndAA { get { return false; } }
        public virtual bool supportsSortingPriority { get { return true; } }

        // Sealed override as SRP suboutputs cannot have dependencies
        public sealed override void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly = true) { }

        public virtual string GetBlendModeStr()
        {
            switch (owner.blendMode)
            {
                case BlendMode.Additive:
                    return "Blend SrcAlpha One";
                case BlendMode.Alpha:
                    return "Blend SrcAlpha OneMinusSrcAlpha";
                case BlendMode.AlphaPremultiplied:
                    return "Blend One OneMinusSrcAlpha";
                default:
                    return string.Empty;
            }
        }

        public virtual string GetRenderQueueStr()
        {
            var baseRenderQueue = string.Empty;
            switch (owner.blendMode)
            {
                case BlendMode.Additive:
                case BlendMode.Alpha:
                case BlendMode.AlphaPremultiplied:
                    baseRenderQueue = "Transparent";
                    break;
                case BlendMode.Opaque:
                    if (owner.hasAlphaClipping)
                        baseRenderQueue = "AlphaTest";
                    else
                        baseRenderQueue = "Geometry";
                    break;
                default:
                    throw new NotImplementedException("Unknown blend mode");
            }

            int rawMaterialSortingPriority = owner.GetMaterialSortingPriority();
            int queueOffset = Mathf.Clamp(rawMaterialSortingPriority, -50, +50);
            return baseRenderQueue + queueOffset.ToString("+#;-#;+0");
        }

        public virtual IEnumerable<KeyValuePair<string, VFXShaderWriter>> GetStencilStateOverridesStr()
        {
            return Enumerable.Empty<KeyValuePair<string, VFXShaderWriter>>();
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            base.OnInvalidate(model, cause);
            if (owner is VFXModel)
                ((VFXModel)owner).Invalidate(model, cause); // Forward invalidate event to owner
        }
    }
}
