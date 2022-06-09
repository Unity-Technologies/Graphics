using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using UnityEngine.Assertions;

[Serializable, VolumeComponentMenu("Post-processing/Custom/ScreenCoordPostProcess")]
public sealed class ScreenCoordPostProcess : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    Material m_Material;

    public bool IsActive() => m_Material != null && intensity.value > 0f;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    const string k_ShaderName = "Hidden/Shader/ScreenCoordPostProcess";

    public override void Setup()
    {
        m_Material = CoreUtils.CreateEngineMaterial(k_ShaderName);
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material != null)
        {
            m_Material.SetFloat("_Intensity", intensity.value);
            cmd.Blit(source, destination, m_Material, 0);
        }
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}
