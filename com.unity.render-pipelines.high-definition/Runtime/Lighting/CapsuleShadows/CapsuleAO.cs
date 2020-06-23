using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Lighting/Capsule/Ambient Occlusion")]
    internal class CapsuleAmbientOcclusion : VolumeComponent
    {

        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f);
    }


    // We probably want to do Capsule AO in the context of the AO system as we should probably merge SSAO and this together (or at least share resources) 
    partial class AmbientOcclusionSystem
    {
        internal void DispatchCapsuleOcclusion(CommandBuffer cmd, HDCamera camera, ComputeBuffer visibleCapsules)
        {
            ComputeShader capsuleOcclusionCS = m_Resources.shaders.capsuleOcclusionCS;
            int capsuleOcclusionKernel = capsuleOcclusionCS.FindKernel("CapsuleOcclusion");

            capsuleOcclusionCS.EnableKeyword("AMBIENT_OCCLUSION");
            capsuleOcclusionCS.DisableKeyword("SPECULAR_OCCLUSION");
            capsuleOcclusionCS.DisableKeyword("DIRECTIONAL_SHADOW");
            cmd.SetComputeTextureParam(capsuleOcclusionCS, capsuleOcclusionKernel, HDShaderIDs._OcclusionTexture, m_AmbientOcclusionTex);
            cmd.SetComputeBufferParam(capsuleOcclusionCS, capsuleOcclusionKernel, HDShaderIDs._CapsuleOccludersDatas, visibleCapsules);

            const int groupSizeX = 8;
            const int groupSizeY = 8;
            int threadGroupX = ((int)(camera.actualWidth) + (groupSizeX - 1)) / groupSizeX;
            int threadGroupY = ((int)(camera.actualHeight) + (groupSizeY - 1)) / groupSizeY;
            cmd.DispatchCompute(capsuleOcclusionCS, capsuleOcclusionKernel, threadGroupX, threadGroupY, camera.viewCount);
        }
    }

} // UnityEngine.Experimental.Rendering.HDPipeline
