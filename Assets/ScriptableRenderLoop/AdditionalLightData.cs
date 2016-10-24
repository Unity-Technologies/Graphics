namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    //@TODO: We should continously move these values
    // into the engine when we can see them being generally useful
    [RequireComponent(typeof(Light))]
    public class AdditionalLightData : MonoBehaviour
    {
        public const int DefaultShadowResolution = 512;

        public int shadowResolution = DefaultShadowResolution;

        [RangeAttribute(0.0F, 100.0F)]
        public float innerSpotPercent = 0.0F;

        [RangeAttribute(0.0F, 1.0F)]
        public float shadowDimmer = 1.0F;

        public bool affectDiffuse = true;
        public bool affectSpecular = true;

        public static float GetInnerSpotPercent01(AdditionalLightData lightData)
        {
            if (lightData != null)
                return Mathf.Clamp(lightData.innerSpotPercent, 0.0f, 100.0f) / 100.0f;
            else
                return 0.0F;
        }

        public static bool GetAffectDiffuse(AdditionalLightData lightData)
        {
            if (lightData != null)
                return lightData.affectDiffuse;
            else
                return true;
        }

        public static bool GetAffectSpecular(AdditionalLightData lightData)
        {
            if (lightData != null)
                return lightData.affectSpecular;
            else
                return true;
        }

        public static float GetShadowDimmer(AdditionalLightData lightData)
        {
            if (lightData != null)
                return Mathf.Clamp(lightData.shadowDimmer, 0F, 1F);
            else
                return 1.0F;
        }

        public static int GetShadowResolution(AdditionalLightData lightData)
        {
            if (lightData != null)
                return lightData.shadowResolution;
            else
                return DefaultShadowResolution;
        }
    }
}
