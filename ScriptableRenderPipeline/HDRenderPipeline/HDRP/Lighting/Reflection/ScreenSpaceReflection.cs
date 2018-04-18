using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public sealed class LitProjectionModelParameter : VolumeParameter<Lit.ProjectionModel> { }

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

        int m_DeferredProjectionModel;

        public LitProjectionModelParameter                 deferredProjectionModel = new LitProjectionModelParameter();

        protected override void FetchIDs(
            out int rayMinLevelID,
            out int rayMaxLevelID,
            out int rayMaxIterationsID,
            out int rayDepthSuccessBiasID,
            out int screenWeightDistanceID
        )
        {
            rayMinLevelID = HDShaderIDs._SSReflectionRayMinLevel;
            rayMaxLevelID = HDShaderIDs._SSReflectionRayMaxLevel;
            rayMaxIterationsID = HDShaderIDs._SSReflectionRayMaxIterations;
            rayDepthSuccessBiasID = HDShaderIDs._SSReflectionRayDepthSuccessBias;
            screenWeightDistanceID = HDShaderIDs._SSReflectionInvScreenWeightDistance;
        }

        public override void PushShaderParameters(CommandBuffer cmd)
        {
            base.PushShaderParameters(cmd);
            cmd.SetGlobalInt(HDShaderIDs._SSReflectionProjectionModel, (int)deferredProjectionModel.value);
        }
    }
}
