using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.LWRP
{

    public class PhysicalSky
    {
        // Enable physical sky.
        public bool m_IsEnabled = false;
        /// Tracks editable sky parameters.
        public BrunetonParameters m_BrunetonParams = BrunetonParameters.MakeDefault();
        /// Bruneton sky model.
        public BrunetonModel m_Model;
        /// Compute shaders to generate look-up textures.
        public readonly ComputeShader m_PrecomputationCS;
        /// Fragment shader to render the sky.
        public readonly Material m_RenderSkyMat;

        // Need to recompute look-up textures.
        private bool m_NeedRecompute = false;
        /// Actual sky parameters. If != from m_BrunetonParams, look-up texture will need to be recomputed.
        private BrunetonParameters m_ActiveBrunetonParams = BrunetonParameters.MakeDefault();

        public PhysicalSky(PhysicalSkyData data)
        {
            // Get/Load shaders.
            m_PrecomputationCS = data.shaders.precomputationCS;
            m_RenderSkyMat = Load(data.shaders.renderSkyPS);
        }

        public void UpdateParameters()
        {
            if (m_Model == null || !BrunetonParameters.IsEquals(ref m_ActiveBrunetonParams, ref m_BrunetonParams))
            {
                m_ActiveBrunetonParams = m_BrunetonParams;
                m_Model = BrunetonModel.Create(m_ActiveBrunetonParams);
                m_NeedRecompute = true;
            }

            // Always copy these paraneters. Changing them do not require to re-precompute the look-up textures.
            m_ActiveBrunetonParams.m_fogAmount = m_BrunetonParams.m_fogAmount;
            m_ActiveBrunetonParams.m_sunSize = m_BrunetonParams.m_sunSize;
            m_ActiveBrunetonParams.m_sunEdge = m_BrunetonParams.m_sunEdge;
            m_ActiveBrunetonParams.m_exposure = m_BrunetonParams.m_exposure;

            m_Model.FogAmount = m_ActiveBrunetonParams.m_fogAmount; // Not ideal: need refactoring
            m_Model.SunSize = m_ActiveBrunetonParams.m_sunSize; // Not ideal: need refactoring
            m_Model.SunEdge = m_ActiveBrunetonParams.m_sunEdge; // Not ideal: need refactoring
            m_Model.Exposure = m_ActiveBrunetonParams.m_exposure; // Not ideal: need refactoring
        }

        public void DrawSkybox(ScriptableRenderContext context, Camera camera)
        {
            if (m_NeedRecompute)
            {
                m_NeedRecompute = false;
                m_Model.ExecutePrecomputation(context, m_PrecomputationCS);
            }

            m_Model.ExecuteSkyBox(context, camera, m_RenderSkyMat);
        }

        public bool IsEnabled()
        {
            return m_IsEnabled;
        }

        private Material Load(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources.");
                return null;
            }

            return CoreUtils.CreateEngineMaterial(shader);
        }
    }
}
