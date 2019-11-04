using System;
using System.Collections.Generic;
using System.Linq;
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
            switch (owner.blendMode)
            {
                case BlendMode.Additive:
                case BlendMode.Alpha:
                case BlendMode.AlphaPremultiplied:
                    return "Transparent";
                case BlendMode.Opaque:
                    if(owner.hasAlphaClipping)
                        return "AlphaTest";
                    else
                        return "Geometry";
                default:
                    throw new NotImplementedException("Unknown blend mode");
            }
        }

        public virtual IEnumerable<KeyValuePair<string, VFXShaderWriter>> GetStencilStateOverridesStr()
        {
            return Enumerable.Empty<KeyValuePair<string, VFXShaderWriter>>();
        }
    }
}
