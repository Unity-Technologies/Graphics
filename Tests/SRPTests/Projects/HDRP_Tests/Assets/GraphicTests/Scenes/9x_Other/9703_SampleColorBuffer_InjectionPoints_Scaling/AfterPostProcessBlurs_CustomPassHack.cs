using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/9703_AfterPostProcessBlurs_CustomPassHack")]
public sealed class AfterPostProcessBlurs_CustomPassHack : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    public bool IsActive() => intensity.value > 0f;

    // Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > Graphics > HDRP Global Settings).
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcessBlurs;

    public override void Setup()
    {
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        Blitter.BlitCameraTexture(cmd, source, destination);

        var cp = GameObject.Find("After Post Process Blurs").GetComponent<CustomPassVolume>();
        if (cp == null || !cp.enabled)
            return;

        CoreUtils.SetRenderTarget(cmd, destination);
        foreach (var c in cp.customPasses)
            (c as DrawRenderersFromPostProcess).ExecuteFromPostProcess(cmd);
    }

    public override void Cleanup()
    {
    }
}
