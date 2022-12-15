using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class FullscreenTests : MonoBehaviour
{
    [Header("UV")]
    public Material uv;
    public RenderTexture uvRT1;
    public RenderTexture uvRT2;

    [Header("ViewDir")]
    public Material viewDir;
    public RenderTexture viewDirRT1;
    public RenderTexture viewDirRT2;

    [Header("AlphaBlend")]
    public Material alphaBlend;
    public RenderTexture alphaBlendRT1;
    public RenderTexture alphaBlendRT2;

    [Header("Stencil")]
    public Material stencilWrite;
    public Material stencilTest;
    public RenderTexture stencilRT1;
    public RenderTexture stencilRT2;

    void Update()
    {
        var cmd = new CommandBuffer{ name = "Fullscreen Blit" };

        DrawFullscreenBlitTest(cmd, uv, uvRT1, uvRT2);
        DrawFullscreenBlitTest(cmd, viewDir, viewDirRT1, viewDirRT2);
        DrawFullscreenBlitTest(cmd, alphaBlend, alphaBlendRT1, alphaBlendRT2);

        DrawFullscreenStencilTest(cmd, stencilWrite, stencilTest, stencilRT1, stencilRT2);

        Graphics.ExecuteCommandBuffer(cmd);
    }

    void DrawFullscreenBlitTest(CommandBuffer cmd, Material material, RenderTexture rt1, RenderTexture rt2)
    {
        CoreUtils.SetRenderTarget(cmd, rt1, ClearFlag.All, Color.white);
        CoreUtils.DrawFullScreen(cmd, material, shaderPassId: 0);
        CoreUtils.SetRenderTarget(cmd, rt2, ClearFlag.All, Color.white);
        cmd.Blit(Texture2D.whiteTexture, rt2, material, pass: 1);
    }

    void DrawFullscreenStencilTest(CommandBuffer cmd, Material writeStencil, Material testStencil, RenderTexture rt1, RenderTexture rt2)
    {
        CoreUtils.SetRenderTarget(cmd, rt1, ClearFlag.All, Color.white);
        CoreUtils.DrawFullScreen(cmd, writeStencil, shaderPassId: 0);
        CoreUtils.DrawFullScreen(cmd, testStencil, shaderPassId: 0);

        CoreUtils.SetRenderTarget(cmd, rt2, ClearFlag.All, Color.white);
        cmd.Blit(Texture2D.whiteTexture, rt2, writeStencil, pass: 1);
        cmd.Blit(Texture2D.whiteTexture, rt2, testStencil, pass: 1);
    }
}
