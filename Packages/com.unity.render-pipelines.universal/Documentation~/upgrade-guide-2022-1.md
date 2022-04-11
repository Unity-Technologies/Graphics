# Upgrading to version 2022.1 of the Universal Render Pipeline

This page describes how to upgrade from an older version of the Universal Render Pipeline (URP) to version 2022.1.

## Upgrading from URP 2021.2

* An error will be issued when instances of `ScriptableRendererFeature` attempt to access render targets before they are allocated by the renderer.

   The `ScriptableRendererFeature` has a new virtual function called `SetupRenderPasses` which is called when render targets are allocated and ready to be used.

  If your code uses `renderer.cameraColorTarget` or `renderer.cameraDepthTarget` inside of the `AddRenderPasses` override, then that use needs to be moved to `SetupRenderPasses`.
  Note that calls to `renderer.EnqueuePass` should still happen in `AddRenderPasses`.

  For example the following use:
  ```c#
  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
  {
      m_CustomPass.Setup(renderer.cameraColorTarget);  // use of target before allocation
      renderer.EnqueuePass(m_ScriptablePass); // letting the renderer know which passes will be used before allocation
  }
  ```

  should become:

  ```c#
  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
  {
      renderer.EnqueuePass(m_ScriptablePass); // letting the renderer know which passes will be used before allocation
  }

  public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
  {
      m_CustomPass.Setup(renderer.cameraColorTarget);  // use of target after allocation
  }
  ```

* The Universal Renderer is now using [`RTHandles`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@13.1/manual/rthandle-system.html) for its internal targets and in its internal passes.

  All uses of `RenderTargetHandle` have been set as Obsolete and the class will be removed in the future.

  The public interfaces `renderer.cameraColorTarget` and `renderer.cameraDepthTarget` have also been marked as obsolete and their uses should be replaced with  `renderer.cameraColorTargetHandle` and `renderer.cameraDepthTargetHandle` respectively.

  `RTHandle` targets do not use `GetTemporaryRT` and are longer-lived than the `RenderTargetIdentifiers` from there. They also cannot be allocated with a `GraphicsFormat` and `DepthBufferBits` set to anything but 0. Depth targets must be separate from Color Targets.

  The following helper functions have been added in order to create and use `RTHandle` targets in the same way as with `GetTemporaryRT`:
     * `RenderingUtils.ReAllocateIfNeeded`
     * `ShadowUtils.ShadowRTReAllocateIfNeeded`

  If the target is known to not change within the lifetime of the application, then simply a `RTHandles.Alloc` would suffice and it will be more efficient due to not doing a check on each frame.

  If the target is a fullscreen texture, meaning that its resolution matches the resolution or a fraction of it, the use of a scaling factor such as `Vector2D.one` is recommended to support dynamic scaling.

  For example, the following `RenderTargetHandle` use:

  ```c#
  public class CustomPass : ScriptableRenderPass
  {
      RenderTargetHandle m_Handle;
      RenderTargetIdentifier m_Destination; // RenderTargetIdentifier sometimes combines color and depth

      public CustomPass()
      {
          m_Handle.Init("_CustomPassHandle");
      }

      public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
      {
          var desc = renderingData.cameraData.cameraTargetDescriptor;
          cmd.GetTemporaryRT(m_Handle.id, desc, FilterMode.Point);
      }

      public override void OnCameraCleanup(CommandBuffer cmd)
      {
          cmd.ReleaseTemporaryRT(m_Handle.id);
      }

      public void Setup(RenderTargetIdentifier destination)
      {
          m_Destination = destination;
      }

      public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
      {
          CommandBuffer cmd = CommandBufferPool.Get();
          ScriptableRenderer.SetRenderTarget(cmd, m_Destination, m_Destination, clearFlag, clearColor); // set same target for color and depth
          // ...
          context.ExecuteCommandBuffer(cmd);
          CommandBufferPool.Release(cmd);
      }
  }
  ```

  should become:

  ```c#
  public class CustomPass : ScriptableRenderPass
  {
      RTHandle m_Handle;
      // RTHandles don't combine color and dpeth
      RTHandle m_DestinationColor;
      RTHandle m_DestinationDepth;

      void Dispose()
      {
          m_Handle?.Release();
      }

      public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
      {
          var desc = renderingData.cameraData.cameraTargetDescriptor;
          desc.depthBufferBits = 0; // Color and depth cannot be combined in RTHandles
          RenderingUtils.ReAllocateIfNeeded(ref m_Handle, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_CustomPassHandle");
      }

      public override void OnCameraCleanup(CommandBuffer cmd)
      {
          m_DestinationColor = null;
          m_DestinationDepth = null;
      }

      public void Setup(RTHandle destinationColor, RTHandle destinationDepth)
      {
          m_DestinationColor = destinationColor;
          m_DestinationDepth = destinationDepth;
      }

      public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
      {
          CommandBuffer cmd = CommandBufferPool.Get();
          ScriptableRenderer.SetRenderTarget(cmd, m_DestinationColor, m_DestinationDepth, clearFlag, clearColor);
          // ...
          context.ExecuteCommandBuffer(cmd);
          CommandBufferPool.Release(cmd);
      }
  }
  ```

## Upgrading from URP 11.x.x

* The Forward Renderer asset is renamed to the Universal Renderer asset. When you open an existing project in the Unity Editor containing URP 12, Unity updates the existing Forward Renderer assets to Universal Renderer assets.

* The Universal Renderer asset contains the property **Rendering Path** that lets you select the Forward or the Deferred Rendering Path.

* The method `ClearFlag.Depth` does not implicitely clear the Stencil buffer anymore. Use the new method `ClearFlag.Stencil`.

## Upgrading from URP 10.0.x–10.2.x

1. The file names of the following Shader Graph shaders were renamed. The new file names do not have spaces:<br/>`Autodesk Interactive`<br/>`Autodesk Interactive Masked`<br/>`Autodesk Interactive Transparent`

    If your code uses the `Shader.Find()` method to search for the shaders, remove spaces from the shader names, for example, `Shader.Find("AutodeskInteractive)`.

## Upgrading from URP 7.2.x and later releases

1. URP 12.x.x does not support the package Post-Processing Stack v2. If your Project uses the package Post-Processing Stack v2, migrate the effects that use that package first.

### DepthNormals Pass

Starting from version 10.0.x, URP can generate a normal texture called `_CameraNormalsTexture`. To render to this texture in your custom shader, add a Pass with the name `DepthNormals`. For example, see the implementation in `Lit.shader`.

### Screen Space Ambient Occlusion (SSAO)

URP 10.0.x implements the Screen Space Ambient Occlusion (SSAO) effect.

If you intend to use the SSAO effect with your custom shaders, consider the following entities related to SSAO:

* The `_SCREEN_SPACE_OCCLUSION` keyword.

* `Input.hlsl` contains the new declaration `float2  normalizedScreenSpaceUV` in the `InputData` struct.

* `Lighting.hlsl` contains the `AmbientOcclusionFactor` struct with the variables for calculating indirect and direct occlusion:

    ```c++
    struct AmbientOcclusionFactor
    {
        half indirectAmbientOcclusion;
        half directAmbientOcclusion;
    };
    ```

* `Lighting.hlsl` contains the following function for sampling the SSAO texture:

    ```c++
    half SampleAmbientOcclusion(float2 normalizedScreenSpaceUV)
    ```

* `Lighting.hlsl` contains the following function:

    ```c++
    AmbientOcclusionFactor GetScreenSpaceAmbientOcclusion(float2
    normalizedScreenSpaceUV)
    ```

To support SSAO in custom shader, add the `DepthNormals` Pass and the `_SCREEN_SPACE_OCCLUSION` keyword the the shader. For example, see `Lit.shader`.

If your custom shader implements custom lighting functions, use the function `GetScreenSpaceAmbientOcclusion(float2 normalizedScreenSpaceUV)` to get the `AmbientOcclusionFactor` value for your lighting calculations.

### Shadow Normal Bias

In 11.0.x the formula used to apply Shadow Normal Bias has been slightly fix in order to work better with punctual lights.
As a result, to match exactly shadow outlines from earlier revisions, the parameter might to be adjusted in some scenes. Typically, using 1.4 instead of 1.0 for a Directional light is usually enough.

### Intermediate Texture

In previous URP versions, URP performed the rendering via an intermediate Renderer if the Renderer had any active Renderer Features. On some platforms, this had significant performance implications. In this release, URP mitigates the issue in the following way: URP expects Renderer Features to declare their inputs using the `ScriptableRenderPass.ConfigureInput` method. The method provides the information that URP uses to determine automatically whether rendering via an intermediate texture is necessary.

For compatibility purpose, there is a new property **Intermediate Texture** in the Universal Renderer. If you select **Always** in the property, URP uses an intermediate texture. Selecting **Auto** enables the new behavior. Use the **Always** option only if a Renderer Feature does not declare its inputs using the `ScriptableRenderPass.ConfigureInput` method.

To ensure that existing projects work correctly, all existing Universal Renderer assets that were using any Renderer Features (excluding those included with URP) have the option **Always** selected in the **Intermediate Texture** property. Any newly created Universal Renderer assets have the option **Auto** selected.

## Upgrading from URP 7.0.x-7.1.x

1. Upgrade to URP 7.2.0 first. Refer to [Upgrading to version 7.2.0 of the Universal Render Pipeline](upgrade-guide-7-2-0.md).

2. URP 8.x.x does not support the package Post-Processing Stack v2. If your Project uses the package Post-Processing Stack v2, migrate the effects that use that package first.

## Upgrading from LWRP to 12.x.x

* There is no direct upgrade path from LWRP to URP 12.x.x. Follow the steps to upgrade LWRP to URP 11.x.x first, and then upgrade from URP 11.x.x to URP 12.x.x.
