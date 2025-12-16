using UnityEngine.Rendering;
#if VFX_OUTPUTEVENT_HDRP_10_0_0_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
#endif

namespace UnityEngine.VFX.Utility
{
    [RequireComponent(typeof(Light))]
#if VFX_OUTPUTEVENT_HDRP_10_0_0_OR_NEWER
    [RequireComponent(typeof(HDAdditionalLightData))]
#endif
    class VFXOutputEventPrefabAttributeLightHandler : VFXOutputEventPrefabAttributeAbstractHandler
    {
        public float brightnessScale = 1.0f;
        static readonly int k_Color = Shader.PropertyToID("color");

        public override void OnVFXEventAttribute(VFXEventAttribute eventAttribute, VisualEffect visualEffect)
        {
            var color = eventAttribute.GetVector3(k_Color);
            var intensity = color.magnitude;
            var c = new Color(color.x, color.y, color.z) / intensity;
            intensity *= brightnessScale;

            var light = GetComponent<Light>();
#if VFX_OUTPUTEVENT_HDRP_10_0_0_OR_NEWER
            var hdlight = GetComponent<HDAdditionalLightData>();
            hdlight.SetColor(c);
            light.intensity = LightUnitUtils.ConvertIntensity(light, intensity, light.lightUnit, LightUnitUtils.GetNativeLightUnit(light.type));
#else
            light.color = c;
            light.intensity = intensity;
#endif
        }
    }
}
