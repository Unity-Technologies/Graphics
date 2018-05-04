using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class ScreenSpaceRefraction : ScreenSpaceLighting
    {
        static ScreenSpaceRefraction s_Default = null;
        public static ScreenSpaceRefraction @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<ScreenSpaceRefraction>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }

        protected override void FetchIDs(
            out int rayLevelID,
            out int rayMaxLinearIterationsID,
            out int rayMinLevelID,
            out int rayMaxLevelID,
            out int rayMaxIterationsID,
            out int rayDepthSuccessBiasID,
            out int invScreenWeightDistanceID
        )
        {
            rayLevelID = HDShaderIDs._SSRefractionRayLevel;
            rayMaxLinearIterationsID = HDShaderIDs._SSRefractionRayMaxLinearIterations;
            rayMinLevelID = HDShaderIDs._SSRefractionRayMinLevel;
            rayMaxLevelID = HDShaderIDs._SSRefractionRayMaxLevel;
            rayMaxIterationsID = HDShaderIDs._SSRefractionRayMaxIterations;
            rayDepthSuccessBiasID = HDShaderIDs._SSRefractionRayDepthSuccessBias;
            invScreenWeightDistanceID = HDShaderIDs._SSRefractionInvScreenWeightDistance;
        }
    }
}
