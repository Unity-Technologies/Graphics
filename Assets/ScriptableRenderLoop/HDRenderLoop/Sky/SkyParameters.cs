using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.ScriptableRenderLoop;
using System.Reflection;
using System.Linq;


[ExecuteInEditMode]
public class SkyParameters : MonoBehaviour
{
    public float            rotation = 0.0f;
    public float            exposure = 0.0f;
    public float            multiplier = 1.0f;
    public SkyResolution    resolution = SkyResolution.SkyResolution256;

    private FieldInfo[] m_Properties;

    HDRenderLoop GetHDRenderLoop()
    {
        HDRenderLoop renderLoop = UnityEngine.Rendering.GraphicsSettings.GetRenderPipeline() as HDRenderLoop;
        if (renderLoop == null)
        {
            Debug.LogWarning("SkyParameters component can only be used with HDRenderLoop custom RenderPipeline.");
            return null;
        }

        return renderLoop;
    }

    protected void OnEnable()
    {
        HDRenderLoop renderLoop = GetHDRenderLoop();
        if(renderLoop == null)
        {
            return;
        }

        // Enumerate properties in order to compute the hash more quickly later on.
        m_Properties =  GetType()
                        .GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .ToArray();

        if (renderLoop.skyParameters == null)
            renderLoop.skyParameters = this;
        else if(renderLoop.skyParameters != this)
            Debug.LogWarning("Tried to setup another SkyParameters component although there is already one enabled.");
    }

    protected void OnDisable()
    {
        HDRenderLoop renderLoop = GetHDRenderLoop();
        if (renderLoop == null)
        {
            return;
        }

        if (renderLoop.skyParameters == this)
            renderLoop.skyParameters = null;
    }

    public int GetHash()
    {
        unchecked
        {
            int hash = 13;
            foreach (var p in m_Properties)
                hash = hash * 23 + p.GetValue(this).GetHashCode();
            return hash;
        }
    }
}
