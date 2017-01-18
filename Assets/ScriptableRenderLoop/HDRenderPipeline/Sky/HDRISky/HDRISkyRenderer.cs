using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDRISkyRenderer
        : SkyRenderer<HDRISkyParameters>
    {
        Material                m_SkyHDRIMaterial = null; // Renders a cubemap into a render texture (can be cube or 2D)

        override public void Build()
        {
            m_SkyHDRIMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/Sky/SkyHDRI");
        }

        override public void Cleanup()
        {
            Utilities.Destroy(m_SkyHDRIMaterial);
        }

        override public bool IsSkyValid(SkyParameters skyParameters)
        {
            return GetParameters(skyParameters).skyHDRI != null;
        }

        public override void SetRenderTargets(BuiltinSkyParameters builtinParams)
        {
            if (builtinParams.depthBuffer == BuiltinSkyParameters.nullRT)
            {
                Utilities.SetRenderTarget(builtinParams.renderContext, builtinParams.colorBuffer);
            }
            else
            {
                Utilities.SetRenderTarget(builtinParams.renderContext, builtinParams.colorBuffer, builtinParams.depthBuffer);
            }
        }

        override public void RenderSky(BuiltinSkyParameters builtinParams, SkyParameters skyParameters, bool renderForCubemap)
        {
            HDRISkyParameters hdriSkyParams = GetParameters(skyParameters);

            m_SkyHDRIMaterial.SetTexture("_Cubemap", hdriSkyParams.skyHDRI);
            m_SkyHDRIMaterial.SetVector("_SkyParam", new Vector4(hdriSkyParams.exposure, hdriSkyParams.multiplier, hdriSkyParams.rotation, 0.0f));

            var cmd = new CommandBuffer { name = "" };
            cmd.DrawMesh(builtinParams.skyMesh, Matrix4x4.identity, m_SkyHDRIMaterial, 0, renderForCubemap ? 0 : 1);
            builtinParams.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }
    }
}
