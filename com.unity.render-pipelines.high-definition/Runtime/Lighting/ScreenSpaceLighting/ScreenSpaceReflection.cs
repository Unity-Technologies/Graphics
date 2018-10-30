using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class ScreenSpaceReflection : ScreenSpaceLighting
    {
        static ScreenSpaceReflection s_Default = null;
        public static ScreenSpaceReflection @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<ScreenSpaceReflection>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }

        public ClampedFloatParameter minSmoothness = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);
        public ClampedFloatParameter smoothnessFadeStart = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);

        protected override void FetchIDs(
            out int rayLevelID,
            out int rayMinLevelID,
            out int rayMaxLevelID,
            out int rayMaxIterationsID,
            out int depthBufferThicknessID,
            out int screenWeightDistanceID,
            out int rayMaxScreenDistanceID,
            out int rayBlendScreenDistanceID,
            out int rayMarchBehindObjectsID
            )
        {
            rayLevelID = HDShaderIDs._SSReflectionRayLevel;
            rayMinLevelID = HDShaderIDs._SSReflectionRayMinLevel;
            rayMaxLevelID = HDShaderIDs._SSReflectionRayMaxLevel;
            rayMaxIterationsID = HDShaderIDs._SSReflectionRayMaxIterations;
            depthBufferThicknessID = HDShaderIDs._SSReflectionDepthBufferThickness;
            screenWeightDistanceID = HDShaderIDs._SSReflectionInvScreenWeightDistance;
            rayMaxScreenDistanceID = HDShaderIDs._SSReflectionRayMaxScreenDistance;
            rayBlendScreenDistanceID = HDShaderIDs._SSReflectionRayBlendScreenDistance;
            rayMarchBehindObjectsID = HDShaderIDs._SSReflectionRayMarchBehindObjects;
        }
    }
}
