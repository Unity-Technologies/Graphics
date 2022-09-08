using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Fullscreen/TvMonochrome")]
public sealed class TvMonochrome : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    public ColorParameter tint = new ColorParameter(Color.black, false);

    Material m_Material;

    public bool IsActive() => m_Material != null && intensity.value > 0f;

    // Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > Graphics > HDRP Settings).
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    const string kShaderName = "Fullscreen/TvMonochrome";

    public override void Setup()
    {
        if (Shader.Find(kShaderName) != null)
            m_Material = new Material(Shader.Find(kShaderName));
        else
            Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process Volume TvMonochrome is unable to load.");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

        m_Material.SetFloat("_Intensity", intensity.value);
        m_Material.SetColor("_Tint", tint.value);
        m_Material.SetTexture("_MainTex", source);

        HDUtils.DrawFullScreen(cmd, m_Material, destination, shaderPassId: 0);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}
