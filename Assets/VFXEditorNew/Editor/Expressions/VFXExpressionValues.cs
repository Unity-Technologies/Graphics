using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXValueFloatNConvertible<T> : VFXValue<T>
    {
        public override void SetContent(object value)
        {
            
        }
    }

    class VFXValueFloat : VFXValue<float> 
    { 
        public VFXValueFloat(float value, bool isConst) : base(value, isConst) { }
        public override void SetContent(object value) // TODO Find a better way for this
        {
            if (value is FloatN)
                m_Content = (FloatN)value;
            else
                base.SetContent(value);
        }
    }

    class VFXValueFloat2 : VFXValue<Vector2> 
    { 
        public VFXValueFloat2(Vector2 value, bool isConst) : base(value, isConst) { }
        public override void SetContent(object value) // TODO Find a better way for this
        {
            if (value is FloatN)
                m_Content = (FloatN)value;
            else
                base.SetContent(value);
        }
    }
    
    class VFXValueFloat3 : VFXValue<Vector3> 
    { 
        public VFXValueFloat3(Vector3 value, bool isConst) : base(value, isConst) { }
        public override void SetContent(object value) // TODO Find a better way for this
        {
            if (value is FloatN)
                m_Content = (FloatN)value;
            else
                base.SetContent(value);
        }
    }

    class VFXValueFloat4 : VFXValue<Vector4> 
    { 
        public VFXValueFloat4(Vector4 value, bool isConst) : base(value, isConst) { }
        public override void SetContent(object value) // TODO Find a better way for this
        {
            if (value is FloatN)
                m_Content = (FloatN)value;
            else
                base.SetContent(value);
        }
    }

    class VFXValueTexture2D : VFXValue<Texture2D> { public VFXValueTexture2D(Texture2D value, bool isConst) : base(value, isConst) { } }
    class VFXValueCurve : VFXValue<AnimationCurve> { public VFXValueCurve(AnimationCurve value, bool isConst) : base(value, isConst) { } }
}