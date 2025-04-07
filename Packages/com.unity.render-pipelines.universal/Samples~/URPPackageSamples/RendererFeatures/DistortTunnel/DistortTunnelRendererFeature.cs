using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// Create a Scriptable Renderer Feature that renders a distorted tunnel effect.
// This Scriptable Renderer Feature is used in the Universal Render Pipeline (URP) package samples. For more information, https://docs.unity3d.com/Manual/urp/package-sample-urp-package-samples.html.
// For more information about creating scriptable renderer features, refer to https://docs.unity3d.com/Manual/urp/customizing-urp.html.
public class DistortTunnelRendererFeature : ScriptableRendererFeature
{
    // Create a property to set the injection point for the render passes.
    public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingSkybox;

    // Create a property to set the shader to use for the distortion effect.
    public Shader distortShader;

    // Set the material to use for the distortion effect.
    private Material m_DistortMaterial;

    // Declare the three render passes.
    // 1. CopyColorPass blits the screen color texture to the first slice of a 2D render texture array.
    // 2. TunnelPass renders a tunnel GameObject from the scene to the second slice of a 2D render texture array.
    // 3. DistortPass uses the two slices to create the final effect, and blits the result back to the screen.    
    private DistortTunnelPass_CopyColor m_CopyColorPass;
    private DistortTunnelPass_Tunnel m_TunnelPass;
    private DistortTunnelPass_Distort m_DistortPass;

    // Declare and name the render texture that stores the render pass outputs.
    private RTHandle m_DistortTunnelTexHandle;
    private const string k_DistortTunnelTexName = "_DistortTunnelTexture";

    // Create a class that keeps the TextureHandle reference in the frame data, so multiple passes in the render graph system can share the texture.
    public class TexRefData : ContextItem
    {
        public TextureHandle distortTunnelTexHandle = TextureHandle.nullHandle;

        public override void Reset()
        {
            distortTunnelTexHandle = TextureHandle.nullHandle;
        }
    }


    public override void Create()
    {
        // Set the resources the render passes use.
        m_CopyColorPass = new DistortTunnelPass_CopyColor(passEvent);
        m_TunnelPass = new DistortTunnelPass_Tunnel(passEvent);
        m_DistortMaterial = CoreUtils.CreateEngineMaterial(distortShader);
        m_DistortPass = new DistortTunnelPass_Distort(m_DistortMaterial, passEvent);
    }

    // Override the AddRenderPasses method to inject passes into the renderer. Unity calls AddRenderPasses once per camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Skip rendering if the camera isn't a game camera.
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        // Create a 2D render texture array that contains 2 slices.
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;
        desc.dimension = TextureDimension.Tex2DArray;
        desc.volumeDepth = 2;
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_DistortTunnelTexHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_DistortTunnelTexName );

        // Set the 2D texture array and its slices as inputs and outputs for the render passes.
        m_CopyColorPass.SetRTHandles(ref m_DistortTunnelTexHandle,0);
        m_TunnelPass.SetRTHandles(ref m_DistortTunnelTexHandle,1);
        m_DistortPass.SetRTHandles(ref m_DistortTunnelTexHandle);

        // Inject the render passes into the renderer.
        renderer.EnqueuePass(m_CopyColorPass);
        renderer.EnqueuePass(m_TunnelPass);
        renderer.EnqueuePass(m_DistortPass);
    }

    // Free the resources the Scriptable Renderer Feature uses.
    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_DistortMaterial);

        m_DistortPass = null;
        m_TunnelPass = null;
        m_CopyColorPass = null;

        m_DistortTunnelTexHandle?.Release();
    }
}
