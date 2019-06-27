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

        public EnvironmentPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
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

            var cmd = CommandBufferPool.Get(k_SetupEnvironmentTag);

            // Do Fog stuff
            using (new ProfilingSample(cmd, "Fog Setup"))
            {
                DoFogSetup();
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
