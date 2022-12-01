using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/Colorblindness")]
public sealed class Colorblindness : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    public enum colorblindness_Types {Protanopia,Deuteranopia,Tritanopia,MonoChromatism,Achromatopsia};
    [Serializable]
    public sealed class colorblindness_Types_Parameter : VolumeParameter<colorblindness_Types>
    {
        public colorblindness_Types_Parameter(colorblindness_Types value, bool overrideState = false) : base(value, overrideState) { }
    }
    public colorblindness_Types_Parameter Type = new colorblindness_Types_Parameter(colorblindness_Types.Protanopia);

    Material m_Material;

    public bool IsActive() => m_Material != null && intensity.value > 0f;

    // Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > Graphics > HDRP Settings).
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    const string kShaderName = "HDRPSamples/ColorblindFilter";

    public override void Setup()
    {
        if (Shader.Find(kShaderName) != null)
            m_Material = new Material(Shader.Find(kShaderName));
        else
            Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process ColorBlindness is unable to load.");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

        m_Material.SetFloat("_Intensity", intensity.value);

        if (Type == colorblindness_Types.Protanopia)
        {
        m_Material.EnableKeyword("_TYPE_PROTANOPIA");
        m_Material.DisableKeyword("_TYPE_DEUTERANOPIA");
        m_Material.DisableKeyword("_TYPE_TRITANOPIA");
        m_Material.DisableKeyword("_TYPE_CONE_MONOCHROMATISM");
        m_Material.DisableKeyword("_TYPE_ACHROMATOPSIA");
        }

        if (Type == colorblindness_Types.Deuteranopia)
        {
        m_Material.DisableKeyword("_TYPE_PROTANOPIA");
        m_Material.EnableKeyword("_TYPE_DEUTERANOPIA");
        m_Material.DisableKeyword("_TYPE_TRITANOPIA");
        m_Material.DisableKeyword("_TYPE_CONE_MONOCHROMATISM");
        m_Material.DisableKeyword("_TYPE_ACHROMATOPSIA");
        }
        if (Type == colorblindness_Types.Tritanopia)
        {
        m_Material.DisableKeyword("_TYPE_PROTANOPIA");
        m_Material.DisableKeyword("_TYPE_DEUTERANOPIA");
        m_Material.EnableKeyword("_TYPE_TRITANOPIA");
        m_Material.DisableKeyword("_TYPE_CONE_MONOCHROMATISM");
        m_Material.DisableKeyword("_TYPE_ACHROMATOPSIA");
        }
        if (Type == colorblindness_Types.MonoChromatism)
        {
        m_Material.DisableKeyword("_TYPE_PROTANOPIA");
        m_Material.DisableKeyword("_TYPE_DEUTERANOPIA");
        m_Material.DisableKeyword("_TYPE_TRITANOPIA");
        m_Material.EnableKeyword("_TYPE_CONE_MONOCHROMATISM");
        m_Material.DisableKeyword("_TYPE_ACHROMATOPSIA");
        }
        if (Type == colorblindness_Types.Achromatopsia)
        {
        m_Material.DisableKeyword("_TYPE_PROTANOPIA");
        m_Material.DisableKeyword("_TYPE_DEUTERANOPIA");
        m_Material.DisableKeyword("_TYPE_TRITANOPIA");
        m_Material.DisableKeyword("_TYPE_CONE_MONOCHROMATISM");
        m_Material.EnableKeyword("_TYPE_ACHROMATOPSIA");
        }

        m_Material.SetTexture("_MainTex", source);
        HDUtils.DrawFullScreen(cmd, m_Material, destination, shaderPassId: 0);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}
