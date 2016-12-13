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
        MaterialPropertyBlock   m_PropertyBlock = null;
        Material                m_SkyHDRIMaterial = null; // Renders a cubemap into a render texture (can be cube or 2D)

        override public void Build()
        {
            m_SkyHDRIMaterial = Utilities.CreateEngineMaterial("HDRenderLoop/Sky/SkyHDRI");
            m_PropertyBlock = new MaterialPropertyBlock();
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

            m_PropertyBlock.SetTexture("_Cubemap", hdriSkyParams.skyHDRI);
            m_PropertyBlock.SetVector("_SkyParam", new Vector4(hdriSkyParams.exposure, hdriSkyParams.multiplier, hdriSkyParams.rotation, 0.0f));
            //m_RenderSkyPropertyBlock.SetMatrix("_InvViewProjMatrix", invViewProjectionMatrix);

            var cmd = new CommandBuffer { name = "" };
            cmd.DrawMesh(builtinParams.skyMesh, Matrix4x4.identity, m_SkyHDRIMaterial, 0, 0, m_PropertyBlock);
            builtinParams.renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }
    }
}
