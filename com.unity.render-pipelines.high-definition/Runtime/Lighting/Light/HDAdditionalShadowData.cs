using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDAdditionalShadowData
    {
        public static void InitDefaultHDAdditionalShadowData(AdditionalShadowData shadowData)
        {
            // Update bias control for HD

            // bias control default value based on empirical experiment
            shadowData.constantBias         = 0.15f;
            shadowData.normalBias           = 0.75f;
        }
    }
}
