using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public sealed class LitProjectionModelParameter : VolumeParameter<ScreenSpaceReflection.AvailableProjectionModel> 
    {
        public LitProjectionModelParameter() : base(ScreenSpaceReflection.AvailableProjectionModel.Proxy, false) { }
    }

    [Serializable]
    public class ScreenSpaceReflection : ScreenSpaceLighting
    {
        // Values must be in sync with Lit.ProjectionModel
        public enum AvailableProjectionModel
        {
            None = 0,
            Proxy = 1,
            HiZ = 2
        }

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
            out int rayLevelID,
            out int rayMaxLinearIterationsLevelID,
            out int rayMinLevelID,
            out int rayMaxLevelID,
            out int rayMaxIterationsID,
            out int rayDepthSuccessBiasID,
            out int screenWeightDistanceID
        )
        {
            rayLevelID = HDShaderIDs._SSReflectionRayLevel;
            rayMaxLinearIterationsLevelID = HDShaderIDs._SSReflectionRayMaxLinearIterations;
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
