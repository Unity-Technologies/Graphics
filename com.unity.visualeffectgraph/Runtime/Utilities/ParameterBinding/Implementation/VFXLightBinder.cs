using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX Light Binder")]
    [VFXBinder("Utility/Light")]
    public class VFXLightBinder : VFXBinderBase
    {
        public string ColorParameter { get { return (string)m_ColorParameter; } set { m_ColorParameter = value; } }
        public string BrightnessParameter { get { return (string)m_BrightnessParameter; } set { m_ColorParameter = value; } }
        public string RadiusParameter { get { return (string)m_RadiusParameter; } set { m_RadiusParameter = value; } }

        [VFXParameterBinding("UnityEngine.Color"), SerializeField]
        protected ExposedParameter m_ColorParameter = "Color";
        [VFXParameterBinding("System.Single"), SerializeField]
        protected ExposedParameter m_BrightnessParameter = "Brightness";
        [VFXParameterBinding("System.Single"), SerializeField]
        protected ExposedParameter m_RadiusParameter = "Radius";
        public Light Target;

        public bool BindColor = true;
        public bool BindBrightness = false;
        public bool BindRadius = false;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null
                && (!BindColor || component.HasVector4(ColorParameter))
                && (!BindBrightness || component.HasFloat(BrightnessParameter))
                && (!BindRadius || component.HasFloat(RadiusParameter))
                ;
        }

        public override void UpdateBinding(VisualEffect component)
        {
            if (BindColor)
                component.SetVector4(ColorParameter, Target.color);
            if (BindBrightness)
                component.SetFloat(BrightnessParameter, Target.intensity);
            if (BindRadius)
                component.SetFloat(RadiusParameter, Target.range);
        }

        public override string ToString()
        {
            return string.Format("Light : '{0}' -> {1}", m_ColorParameter, Target == null ? "(null)" : Target.name);
        }
    }
}
