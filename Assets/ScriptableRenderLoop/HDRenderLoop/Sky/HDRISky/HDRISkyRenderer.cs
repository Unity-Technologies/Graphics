using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    public class HDRISkyRenderer
        : SkyRenderer
    {
        Material                m_SkyHDRIMaterial = null; // Renders a cubemap into a render texture (can be cube or 2D)

        override public void Build()
        {
            m_SkyHDRIMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/Sky/SkyHDRI");
        }

        override public void Cleanup()
        {
            Utilities.Destroy(m_SkyHDRIMaterial);
        }
        
        HDRISkyParameters GetHDRISkyParameters(SkyParameters parameters)
        {
            HDRISkyParameters hdriSkyParams = parameters as HDRISkyParameters;
            if (hdriSkyParams == null)
            {
                Debug.LogWarning("HDRISkyRenderer needs an instance of HDRISkyParameters to be able to render.");
                return null;
            }

            return hdriSkyParams;
        }

        override public bool IsSkyValid(SkyParameters skyParameters)
        {
            HDRISkyParameters hdriSkyParams = GetHDRISkyParameters(skyParameters);
            if (hdriSkyParams == null)
            {
                return false;
            }

            return hdriSkyParams.skyHDRI != null;
        }

        override public void RenderSky(BuiltinSkyParameters builtinParams, SkyParameters skyParameters)
        {
            HDRISkyParameters hdriSkyParams = GetHDRISkyParameters(skyParameters);
            if(hdriSkyParams == null)
            {
                return;
            }

            m_SkyHDRIMaterial.SetTexture("_Cubemap", hdriSkyParams.skyHDRI);
            m_SkyHDRIMaterial.SetVector("_SkyParam", new Vector4(hdriSkyParams.exposure, hdriSkyParams.multiplier, hdriSkyParams.rotation, 0.0f));

            var cmd = new CommandBuffer { name = "" };
            cmd.DrawMesh(builtinParams.skyMesh, Matrix4x4.identity, m_SkyHDRIMaterial);
            builtinParams.renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }
    }
}
