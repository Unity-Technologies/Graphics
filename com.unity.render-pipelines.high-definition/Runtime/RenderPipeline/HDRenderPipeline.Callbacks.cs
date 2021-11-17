// custom-begin:
using System.Collections.Generic;
using UnityEngine.VFX;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    // Callbacks to allow external classes to inject code into the command buffer at specific locations in the render loop.
    // example:
    //
    /*
    public class ExampleCustomRenderingManager : MonoBehaviour
    {
        // Define some static methods you would like to invoke during the render loop.
        static void OnBuild(HDRenderPipelineAsset asset)
        {
        }
        static void OnFree()
        {
            // TODO: Free any render textures allocated in OnInitializeRenderTextures() here.
        }
        static void OnPushGlobalParameters(HDCamera camera, CommandBuffer commandBuffer)
        {
            // TODO: Set any global shader uniforms here.
        }
        static void OnCameraPostRenderGBuffer(ScriptableRenderContext renderContext, HDCamera camera, CommandBuffer commandBuffer)
        {
        }
        static void OnCameraPostRenderForward(ScriptableRenderContext renderContext, HDCamera camera, CommandBuffer commandBuffer)
        {
            // TODO: Do cool graphics stuff by appending to the commandBuffer here.
        }
        // Define the initialization logic.
        static void HDRPSubscribe()
        {
            // Subscribe to a callback.
            // ExampleCustomRenderingManager.OnCameraPostRenderForward() will now be invoked by HDRenderPipeline
            // at said location in the render loop.
            HDRPUnsubscribe();
            HDRenderPipeline.OnFree += ExampleCustomRenderingManager.OnFree;
            HDRenderPipeline.OnPushGlobalParameters += ExampleCustomRenderingManager.OnPushGlobalParameters;
            HDRenderPipeline.OnCameraPostRenderGBuffer += ExampleCustomRenderingManager.OnCameraPostRenderGBuffer;
            HDRenderPipeline.OnCameraPostRenderForward += ExampleCustomRenderingManager.OnCameraPostRenderForward;
        }
        static void HDRPUnsubscribe()
        {
            HDRenderPipeline.OnFree -= ExampleCustomRenderingManager.OnFree;
            HDRenderPipeline.OnPushGlobalParameters -= ExampleCustomRenderingManager.OnPushGlobalParameters;
            HDRenderPipeline.OnCameraPostRenderGBuffer -= ExampleCustomRenderingManager.OnCameraPostRenderGBuffer;
            HDRenderPipeline.OnCameraPostRenderForward -= ExampleCustomRenderingManager.OnCameraPostRenderForward;
        }
    }
    */
    public partial class HDRenderPipeline : RenderPipeline
    {
        // custom-begin:
        
        public delegate void Action<T1, T2, T3, T4, T5, T6>(T1 arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        // public static event Action OnFree;
        // public static event Action<ScriptableRenderContext, Camera[]> OnBeginFrameRendering;
        public static event Action<HDCamera, CommandBuffer> OnPushGlobalParameters;
        public static event Action<ScriptableRenderContext, HDCamera, CommandBuffer> OnPostBuildLightLists;
        // public static event Action<ScriptableRenderContext, HDCamera, CommandBuffer> OnCameraPostRenderGBuffer;
        // public static event Action<HDCamera, CommandBuffer, ComputeShader, int> OnCameraPreRenderVolumetrics;
        // public static event Action<ScriptableRenderContext, HDCamera, CommandBuffer, RenderTargetIdentifier> OnCameraPostRenderDeferredLighting;
        // public static event Action<ScriptableRenderContext, HDCamera, CommandBuffer, RenderTargetIdentifier, RenderTargetIdentifier> OnCameraPostRenderForward;
        // public static event Action<ScriptableRenderContext, HDCamera, CommandBuffer, RenderTargetIdentifier, RenderTargetIdentifier> OnPostRenderGizmos;
        public static event Action<ScriptableRenderContext, HDCamera, CommandBuffer, RenderTargetIdentifier, RenderTargetIdentifier> OnCameraPreRenderPostProcess;
        public static event Action<Camera, RenderTexture> OnScreenshotCapture;
        // public static event Action<ScriptableRenderContext, Camera> OnNoesisBeginCameraRendering;
        // public static event Action<ScriptableRenderContext, Camera> OnNoesisEndCameraRendering;
        public static event Action<HDCamera, RenderGraph> OnRenderGraphBegin;
        
    }
}
// custom-end
