using System;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class ImageEffects : MonoBehaviour
{
    public Material material;

    void Start()
    {
        if (null == material || null == material.shader ||
           !material.shader.isSupported)
        {
            enabled = false;
            return;
        }
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        var cmd = new CommandBuffer();
        // cmd.SetGlobalTexture("_CameraDepthNormalsTexture", new RenderTargetIdentifier(BuiltinRenderTextureType.DepthNormals));
        cmd.Blit(source, destination, material, 1);
        Graphics.ExecuteCommandBuffer(cmd);
    }
}
