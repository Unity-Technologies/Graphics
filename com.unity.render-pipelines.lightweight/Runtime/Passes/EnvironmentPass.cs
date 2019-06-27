using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.LWRP
{
    internal class EnvironmentPass : ScriptableRenderPass
    {
        RenderTextureDescriptor m_Descriptor;
        RenderTargetHandle m_Source;
        RenderTargetHandle m_Destination;
        RenderTargetHandle m_Depth;
        RenderTargetHandle m_InternalLut;

        const string k_SetupEnvironmentTag = "Setup Environment Effects";

        // Builtin effects settings
        Fog m_Fog;
        Sky m_Sky;

        PhysicalSky m_PhysicalSky;

        public EnvironmentPass(RenderPassEvent evt, PhysicalSky physicalSky)
        {
            renderPassEvent = evt;
            m_PhysicalSky = physicalSky;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var desc = cameraTextureDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            cmd.GetTemporaryRT(m_Destination.id, desc, FilterMode.Point);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Start by pre-fetching all builtin effect settings we need
            var stack = VolumeManager.instance.stack;
            m_Fog        = stack.GetComponent<Fog>();
            //ebug.Log(m_Fog.cubemap.value.name);
            m_Sky        = stack.GetComponent<Sky>();

            var cmd = CommandBufferPool.Get(k_SetupEnvironmentTag);

            // Do Fog stuff
            using (new ProfilingSample(cmd, "Fog Setup"))
            {
                DoFogSetup();
            }

            // Do Sky stuff
            using (new ProfilingSample(cmd, "Sky Setup"))
            {
                DoSkySetup(renderingData.cameraData.camera);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        #region Fog

        void DoFogSetup()
        {
            if (m_Fog.type.value != FogType.Off)
            {
                var fogParams = LegacyFogParams();
            
                switch (m_Fog.type.value)
                {
                    case FogType.Linear:
                        Shader.EnableKeyword("FOG_LINEAR");
                        Shader.DisableKeyword("FOG_EXP2");
                        Shader.DisableKeyword("FOG_EXP");
                        break;
                    case FogType.Exp2:
                        Shader.DisableKeyword("FOG_LINEAR");
                        Shader.EnableKeyword("FOG_EXP2");
                        Shader.DisableKeyword("FOG_EXP");
                        break;
                    case FogType.Height:
                        fogParams = new Vector4(m_Fog.density.value, m_Fog.cubemap.value.mipmapCount, m_Fog.heightOffset.value, 0);
                        Shader.DisableKeyword("FOG_LINEAR");
                        Shader.DisableKeyword("FOG_EXP2");
                        Shader.EnableKeyword("FOG_EXP");
                        break;
                }
                
                switch (m_Fog.colorType.value)
                {
                    case FogColorType.Color:
                        Shader.DisableKeyword("FOGMAP");
                        break;
                    /*case FogColorType.Gradient:
                        Shader.DisableKeyword("FOGMAP");
                        break;*/
                    case FogColorType.CubeMap:
                        Shader.EnableKeyword("FOGMAP");
                        Shader.SetGlobalFloat("_Rotation", m_Fog.rotation.value);
                        Shader.SetGlobalTexture("_FogMap", m_Fog.cubemap.value);
                        break;
                }
                
                Shader.SetGlobalColor(ShaderConstants._FogColor, m_Fog.fogColor.value);
                Shader.SetGlobalVector(ShaderConstants._FogParams, fogParams);
            }
            else
            {
                // Disable all fog
                Shader.DisableKeyword("FOG_LINEAR");
                Shader.DisableKeyword("FOG_EXP2");
                Shader.DisableKeyword("FOG_EXP");
            }
        }

        #endregion

        #region Sky

        void DoSkySetup(Camera camera)
        {
            if (m_PhysicalSky != null)
            {
                m_PhysicalSky.m_IsEnabled = (m_Sky.type.value == SkyType.Bruneton);

                if (m_PhysicalSky.m_IsEnabled)
                {
                    m_PhysicalSky.m_BrunetonParams.m_mieScattering = m_Sky.mieScattering.value;
                    m_PhysicalSky.m_BrunetonParams.m_raleightScattering = m_Sky.raleightScattering.value;
                    m_PhysicalSky.m_BrunetonParams.m_ozoneDensity = m_Sky.ozoneDensity.value;
                    m_PhysicalSky.m_BrunetonParams.m_phase = m_Sky.phase.value;
                    m_PhysicalSky.m_BrunetonParams.m_fogAmount = m_Sky.fogAmount.value;
                    m_PhysicalSky.m_BrunetonParams.m_sunSize = m_Sky.sunSize.value;
                    m_PhysicalSky.m_BrunetonParams.m_sunEdge = m_Sky.sunEdge.value;
                    m_PhysicalSky.m_BrunetonParams.m_exposure = m_Sky.exposure.value;

                    m_PhysicalSky.UpdateParameters();

                    m_PhysicalSky.m_Model.BindGlobal(camera, null, null);
                }
        }
        }

        #endregion

        #region Internal utilities

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        static class ShaderConstants
        {
            public static readonly int _FogColor = Shader.PropertyToID("_FogColor");
            public static readonly int _FogParams = Shader.PropertyToID("_FogParams");
        }

        Vector4 LegacyFogParams()
        {
            //(density / sqrt(ln(2)), density / ln(2), –1/(end-start), end/(end-start))
            var mipCount = 0;
            if (m_Fog.cubemap.value != null)
                mipCount = m_Fog.cubemap.value.mipmapCount;
            return new Vector4(m_Fog.density.value / Mathf.Sqrt(2f), mipCount, -1f / (m_Fog.farFog.value - m_Fog.nearFog.value), m_Fog.farFog.value / (m_Fog.farFog.value - m_Fog.nearFog.value));
        }

        #endregion
    }
}
