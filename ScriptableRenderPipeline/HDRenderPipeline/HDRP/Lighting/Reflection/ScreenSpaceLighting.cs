using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public abstract class ScreenSpaceLighting : VolumeComponent
    {
        int m_RayMinLevelID;
        int m_RayMaxLevelID;
        int m_RayMaxIterationsID;
        int m_RayDepthSuccessBiasID;
        int m_InvScreenWeightDistanceID;

        public IntParameter                 rayMinLevel = new IntParameter(2);
        public IntParameter                 rayMaxLevel = new IntParameter(6);
        public IntParameter                 rayMaxIterations = new IntParameter(32);
        public FloatParameter               rayDepthSuccessBias = new FloatParameter(0.1f);
        public ClampedFloatParameter        screenWeightDistance = new ClampedFloatParameter(0.1f, 0, 1);

        public virtual void PushShaderParameters(CommandBuffer cmd)
        {
            cmd.SetGlobalInt(m_RayMinLevelID, rayMinLevel.value);
            cmd.SetGlobalInt(m_RayMaxLevelID, rayMaxLevel.value);
            cmd.SetGlobalInt(m_RayMaxIterationsID, rayMaxIterations.value);
            cmd.SetGlobalFloat(m_RayDepthSuccessBiasID, rayDepthSuccessBias.value);
            cmd.SetGlobalFloat(m_InvScreenWeightDistanceID, 1f / screenWeightDistance.value);
        }

        protected abstract void FetchIDs(
            out int rayMinLevelID,
            out int rayMaxLevelID,
            out int rayMaxIterationsID,
            out int rayDepthSuccessBiasID,
            out int invScreenWeightDistanceID
        );

        void Awake()
        {
            FetchIDs(
                out m_RayMinLevelID,
                out m_RayMaxLevelID,
                out m_RayMaxIterationsID,
                out m_RayDepthSuccessBiasID,
                out m_InvScreenWeightDistanceID
            );
        }
    }
}
