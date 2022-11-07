# URP Package Samples

URP Package Samples is a [package sample](package-samples.md) for the Universal Render Pipeline (URP). It contains example shaders, C# scripts, and other assets you can build upon, use to learn how to use a feature, or use directly in your application. For information on how to import URP Package Samples into your project, see [Importing package samples](package-samples.md#importing-package-samples).

Each example uses its own [URP Asset](universalrp-asset.md) so, if you want to build an example scene, [add the example's URP Asset to your Graphics settings](InstallURPIntoAProject.md#set-urp-active). If you don't do this, Unity might strip shaders or render passes that the example uses.

<a name="camera-stacking"></a>
## Camera Stacking

The `URP Package Samples/CameraStacking` folder contains examples for [Camera Stacking](camera-stacking.md). The following table describes each Camera Stacking example in this folder.

| **Example**             | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Mixed field of view** | The example in `CameraStacking/MixedFOV` demonstrates how to use Camera Stacking in a first-person application to prevent the character's equipped items from clipping into the environment. This setup also makes it possible to have different fields of view for the environment camera and the equipped items camera. |
| **Split screen**        | The example in `CameraStacking/SplitScreenPPUI` demonstrates how to create a split-screen camera setup where each screen has its own Camera Stack. It also demonstrates how to apply post-processing on world-space and screen-space camera UI. |
| **3D skybox**           | The example in `CameraStacking/3D Skybox` uses Camera Stacking to transform a miniature environment into a skybox. One overlay camera renders a miniature city and another renders miniature planets. The overlay cameras render to pixels that the main camera did not draw to. With some additional scripted translation, this makes the miniature environment appear full size in the background of the main camera's view. |

<a name="decals"></a>
## Decals

The `URP Package Samples/Decals` folder contains examples for [decals](renderer-feature-decal.md). The following table describes each decal example in this folder.

| **Example**        | **Description**                                              |
| ------------------ | ------------------------------------------------------------ |
| **Blob shadows**   | The example in `Decals/BlobShadow` uses the [Decal Projector component](renderer-feature-decal.md#decal-projector-component) to cast a shadow under a character. This method of shadow rendering is less resource-intensive than shadow maps and is suitable for use on low-end devices. |
| **Paint splat**    | The example in `Decals/PaintSplat` uses a WorldSpaceUV Sub Graph and the [Simple Noise](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Simple-Noise-Node.html) Shader Graph node to create procedural decals. The noise in each paint splat uses the world position of the Decal Projector component. |
| **Proxy lighting** | The example in `Decals/ProxyLighting` builds on the **Blob shadows** example and uses Decal Projectors to add proxy spotlights. These decals modify the emission of surfaces inside the projector's volume. Note: To demonstrate the extent of its lighting simulation, this example disables normal real-time lighting. |

<a name="lens-flares"></a>
## Lens Flares

The `URP Package Samples/LensFlares` folder contains lens flare examples. The following table describes each lens flare example in this folder.

| **Example**             | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Sun flare**           | The `LensFlares/SunFlare` example demonstrates how to use the [Lens Flare component](shared/lens-flare/lens-flare-component.md) to add a lens flare effect to the main directional light in the scene. |
| **Lens flare showroom** | The `LensFlares/LensFlareShowroom` example helps you to author lens flares. To use it:</br>1. In the Hierarchy window, select the **Lens Flare** GameObject.</br>2. In the Lens Flare component, assign a [LensFlareDataSRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@12.0/api/UnityEngine.Rendering.LensFlareDataSRP.html) asset to the **Lens Flare Data** property.</br>3. Change the Lens Flare component and data properties and view the lens flare in the Game View.<br/>**Note**: If the text box is in the way, disable the Canvas in the scene. |

<a name="renderer-features"></a>
## Renderer Features

The `URP Package Samples/RendererFeatures` folder contains examples for [Renderer Features](urp-renderer-feature.md). The following table describes each Renderer Feature example in this folder.

| **Example**           | **Description**                                              |
| --------------------- | ------------------------------------------------------------ |
| **Ambient occlusion** | The example in `RendererFeatures/AmbientOcclusion` uses a Renderer Feature to add [screen space ambient occlusion (SSAO)](post-processing-ssao.md) to URP. See the `SSAO_Renderer` asset for an example of how to set up this effect. |
| **Glitch effect**     | The example in `RendererFeatures/GlitchEffect` uses the [Render Objects](renderer-features/renderer-feature-render-objects.md) Render Feature and the [Scene Color](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Scene-Color-Node.html) Shader Graph node to draw some GameObjects with a glitchy effect. See the `Glitch_Renderer` asset for an example of how to set up this effect. |
| **Keep frame**        | The example in `RendererFeatures/KeepFrame` uses a custom Renderer Feature to preserve frame color between frames. The example uses this to create a swirl effect from a simple particle system.<br/>**Note**: The effect is only visible in Play Mode. |
| **Occlusion effect**  | The example in `RendererFeatures/OcclusionEffect` uses the Render Objects Renderer Feature to draw occluded geometry. The example achieves this effect without any code and sets everything up in the `OcclusionEffect_Renderer` asset. |
| **Trail effect**      | The example in `RendererFeatures/TrailEffect` uses the Renderer Feature from the **Keep frame** example on an additional camera to create a trail map. To do this, the additional camera draws depth to a RenderTexture. The `Sand_Graph` shader samples the map and displaces vertices on the ground. |

<a name="shaders"></a>
## Shaders

The `URP Package Samples/Shaders` folder contains examples for shaders. The following table describes each shader example in this folder.

| **Example** | **Description**                                              |
| ----------- | ------------------------------------------------------------ |
| **Lit**     | The example in `Shaders/Lit` demonstrates how different properties of the [Lit shader](lit-shader.md) affect the surface of some geometry. You can use the materials and textures as guidelines on how to set up materials in URP. |
