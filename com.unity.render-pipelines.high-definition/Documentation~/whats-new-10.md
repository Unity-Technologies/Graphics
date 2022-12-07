# What's new in version 10 / Unity 2020.2

This page contains an overview of new features, improvements, and issues resolved in version 10 of the High Definition Render Pipeline (HDRP), embedded in Unity 2020.2.

## Added

### Added support for the PlayStation 5 platform.

This version of HDRP includes support for the Playstation 5 platform. For more information, see [building for consoles](Building-For-Consoles.md).

### Added support for the Game Core Xbox Series platform and Game Core Xbox One

This version of HDRP includes support for the Game Core Xbox Series platform as well as support for Game Core for Xbox One. For more information, see [building for consoles](Building-For-Consoles.md).

### IES Profiles and light cookies

![](Images/HDRPFeatures-IESProfiles.png)

HDRP now supports the Illuminating Engineering Society's (IES) file format for describing the distribution of light from a light source. HDRP supports the IES profile for Point, Spot (Cone, Pyramid, and Box), and rectangular Area [Lights](Light-Component.md). You can also mix the IES profile with [cookies](https://docs.unity3d.com/Manual/Cookies.html) and even use the profile and cookie mix for [light map baking](https://docs.unity3d.com/Manual/LightMode-Baked.html).

### Exposure

#### Histogram exposure

HDRP's exposure implementation can now compute a histogram of the image which allows you to select high and low percentile values to discard. Discarding outlying values in the shadows or highlights helps to calculate a more stable exposure.

For more information on this feature, see [Exposure](Override-Exposure.md).

#### New metering mode

It is often best to not consider the whole screen as input for the auto-exposure algorithm so HDRP's exposure implementation now includes a metering mask. This includes a texture-based mask and a procedural mode.

For more information about this feature, see [Exposure](Override-Exposure.md).

#### Debug modes

HDRP now includes new debug modes that can help you to set the correct exposure for your Scene.

For more information about the debug modes, see [Exposure](Override-Exposure.md) and [Rendering Debugger](Render-Pipeline-Debug-Window.md).


### Scalability settings

This version of HDRP includes scalability settings for fog and subsurface scattering. These settings allow for more control over the performance and quality of volumetric lighting.

### Screen-space global illumination

![](Images/HDRPFeatures-SSGI.png)

This version of HDRP introduces screen-space global illumination (SSGI). It is an algorithm that accesses indirect diffuse lighting the environment generates. It works in the same way as the [Screen Space Reflection](Override-Screen-Space-Reflection.md) in that it uses ray marching to calculate the result.

For more information, see [Screen Space Global Illumination](Override-Screen-Space-GI.md).

### Custom Pass AOV Export

![img](Images/aov_example.png)
This feature allows you to export arbitrary data from custom pass injection points using an extension of the Arbitrary Output Variables (AOV) API in HDRP. An example use-case is for exporting “Object IDs” that are rendered with a custom pass. For information about the feature and example scripts, see the [AOV documentation](AOVs.md).

### Debug modes

#### Light debug view

![](Images/LightDebugView.png)

To help you to debug lighting in your Scene, HDRP now includes various lighting debug view modes that allow you to separate the various components of the light into multiple parts. These debug modes are also available in the [AOV](AOVs.md) API to allow recorders to export them. The new lighting debug modes are:

- Diffuse
- Specular
- Direct diffuse
- Direct specular
- Indirect diffuse
- Reflection
- Refraction

#### Light Layer debug mode

HDRP now includes a new [light layer](Light-Layers.md) debug mode which can display the light layers assigned to each GameObject or can highlight GameObjects which match the light layers of a specific Light.

For more information, see the Lighting panel section in the [Rendering Debugger](Render-Pipeline-Debug-Window.md).

#### Volume debug mode
![](Images/VolumeDebugMode.png)

The Rendering Debugger window now has a new Volume panel which you can use to visualize the Volume components that affect a specific Camera. For each Volume that contributes to the final interpolated value, this panel shows the value of each property and whether or not it is overridden. It also calculates the Volume's influence percentage using the Volume's weight and blend distance. For more information, see the Volume panel section in the [Rendering Debugger](Render-Pipeline-Debug-Window.md#VolumePanel).

#### Quad Overdraw and Vertex Density

![quad_density](Images/quad_density_example.png)
To help you finding GameObjects in you scene that need LODs, HDRP includes two new full screen rendering debug modes to spot Meshes far away or with too many details.

- Quad Overdraw: highlights GPU quads running multiple fragment shaders, which is mainly caused by small or thin triangles. (Not supported on Metal and PS4)
- Vertex Density: displays pixels running multiple vertex shaders. (Not supported on Metal)

### Alpha to Mask

This version of HDRP adds support for alpha to mask (or alpha to coverage) to all HDRP and Shader Graph shaders.

For more information, see the Alpha to Mask property in [Alpha Clipping](Alpha-Clipping.md).

### Range attenuation for box-shaped spotlights

This version of HDRP adds a range attenuation checkbox to box-shaped spotlights, which makes the Light shine uniformly across its range.

For more information, see the [Light component documentation](Light-Component.md).

### Parallax occlusion mapping for emissive maps

This version of HDRP adds support for emissive maps with parallax occlusion mapping.

### Screen-space reflection on transparent Materials

HDRP's screen-space reflection (SSR) solution now support transparent materials. This is useful for transparent surfaces such as windows or water.

### Fabric, Hair, and Eye material examples

HDRP now includes a new sample that contains example fabric and hair materials. You can use these materials as references to more quickly develop fabric and hair materials for your application. HDRP now also includes an eye Shader Graph which you can use to create a realistic eye Material. There are also new HDRP-specific Shader Graph nodes which allow you to more easier customize this eye Shader Graph.

### Decal improvment - Decal Bias, Decal Layers, and Decal angle fading

This version of HDRP introduces Decal Layers which allow you to specify which decals affect which Materials on a layer by layer basis. For more information about Decal Layers, see the [Decal documentation](Decal.md). This version also introduce the support of angle based fading for Decal when Decal Layers are enabled. Lastly this version introduces a new world-space bias (in meters) option that HDRP applies to the decal’s Mesh to stop it from overlapping with other Meshes along the view vector.

### Input System package support

This version of HDRP introduces support for the [Input System package](http://docs.unity3d.com/Packages/com.unity.inputsystem@latest). Before this version, if you enabled the Input System package and disabled the built-in input system, the debug menu and camera control scripts HDRP provides would no longer work. Now, both the debug menu and camera control scripts work when you exclusively enable the Input System package.

### HDRI Flowmap

The [HDRI Sky](Override-HDRI-Sky.md) override now contains a new property to allow you to apply a flowmap to the sky cubemap.

For more information, see the [HDRI Sky documentation](Override-HDRI-Sky.md).

### Graphics Compositor

![](Images/Compositor-HDRPTemplateWithLogo_Feature.png)
The Graphics Compositor allows real-time compositing operations between layers of 3D content, static images, and videos.

The tool support three types of compositing techniques:

- Graph-based compositions, guided from a Shader Graph.
- Camera stacking compositions, where multiple cameras render to the same render target, the result of which can then be used in graph-based composition.
- 3D composition, where you insert composition layers into a 3D Scene to allow for effects like reflections/refractions between composited layers on 3D GameObject.

Overall, this tool allows you to compose a final frame by mixing images and videos with 3D content in real-time inside Unity, without the need of an external compositing tool.

For information about the feature, see the [HDRP Compositor documentation](Compositor-Main.md).

### Path tracing

#### Path-traced depth of field

![](Images/Path-traced-DOF-Feature.png)

This version of HDRP includes a new depth of field mode for producing path-traced images with high-quality defocus blur. Compared to post-processed depth of field, this mode works correctly with multiple layers of transparency and does not produce any artifacts, apart from noise typical in path traced images (which you can mitigate by increasing the sample count and/or using an external denoising tool).

For more information about this feature, see [Depth-of-field](Post-Processing-Depth-of-Field.md).

#### Accumulation motion blur and path tracer convergence APIs

![](Images/Path_tracing_recording-Feature.png)
HDRP now includes a recording API which you can use to render effects such as high-quality accumulation motion blur and converged path-traced images. These techniques create the final "converged" frame by combining information from multiple intermediate sub-frames. The new API allows your scripts to extract the properly converged final frames and do further processing or save them to disk.

For information about this feature and for some example scripts, see [Multiframe rendering and accumulation documentation](Accumulation.md).

#### Path-traced sub-surface scattering

![](Images/Path-traced-SSS-Feature.png)

Path tracing now supports subsurface scattering (SSS), using a random walk approach. To use it, enable path tracing and set up SSS in the same way as you would for HDRP materials.

For information on SSS in HDRP, see [subsurface scattering](Subsurface-Scattering.md).

#### Path-traced fog
![](Images/Path-traced-fog-Feature.png)

Path tracing now supports fog absorption. Like SSS, to use this feature, enable path tracing and set up fog in the same way as you would for standard fog in HDRP.

For information on fog in HDRP, see [fog](Override-Fog.md).

#### Support for shader graph in path tracing

Path tracing now supports Lit and Unlit Shader Graph nodes.

**Note**: The graph shouldn't contain nodes that rely on screen-space differentials (ddx, ddy). Nodes that compute the differences between the current pixel and a neighboring one do not compute correctly when you use ray tracing.

### Shadows

Contact shadows in HDRP 10.x introduce the following new properties:

* **Bias**
* **Thickness**

These properties might change to the appearance of contact shadows that use the default parameters.

For more information, see [Contact shadows](Override-Contact-Shadows.md).

### Shader Code

#### New vertex normal property

HDRP 10.x adds the new vertex normal, `float3 vtxNormal` to the following decal function prototypes so you HDRP can perform angle-based fading:

* The prototype for the function `GetDecalSurfaceData()` has changed from: `DecalSurfaceData GetDecalSurfaceData(PositionInputs posInput, inout float alpha)` to: `DecalSurfaceData GetDecalSurfaceData(PositionInputs posInput, float3 vtxNormal, inout float alpha)`
* The prototype for the function `ApplyDecalToSurfaceData()` has changed from: `void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)` to: `void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout SurfaceData surfaceData)`

#### New full screen debug pass

From 10.x, HDRP includes a new full screen debug pass named `FullScreenDebug`. For HDRP to render a GameObject during the fullscreen debug pass, the shader that the GameObject's Material uses must contain this pass.

#### positionNDC parameter

Unity 2020.2, adds the  parameter, `positionNDC`,to the function `SampleEnv()`. Its prototype changes from:

`float4 SampleEnv(LightLoopContext lightLoopContext, int index, float3 texCoord, float lod, float rangeCompressionFactorCompensation, int sliceIdx = 0)`

to:

`float4 SampleEnv(LightLoopContext lightLoopContext, int index, float3 texCoord, float lod, float rangeCompressionFactorCompensation, float2 positionNDC, int sliceIdx = 0)`.

For example, the call in the Lit shader is now: `float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness), lightData.rangeCompressionFactorCompensation, posInput.positionNDC);`

#### Fake distance based roughness

HDRP 10.x adds new functionality for fake distance based roughness on Reflection Probes and introduces a new helper function. For example, this is how you use the function in the Lit shader:

`float4 preLD = SampleEnvWithDistanceBaseRoughness(lightLoopContext, posInput, lightData, R, preLightData.iblPerceptualRoughness, intersectionDistance);`

The parameter, `intersectionDistance`, is the return value of the `EvaluateLight_EnvIntersection()` function.

#### Metallic range remapping

From 10.x, HDRP uses range remapping for the metallic property when using a mask map. In the Lit, LitTessellation, LayeredLit, and LayeredLitTesselation shaders, HDRP adds two new properties:

* `_MetallicRemapMin`
* `_MetallicRemapMax`.

In the Decal shader, HDRP adds the `_MetallicRemapMin` property.

This version renames `_MetallicScale` to `_MetallicRemapMax`.

HDRP 10.x adds a new pass, `ScenePickingPass`, to all the shader and master nodes to allow the editor to correctly handle picking with tessellated objects and backfaced objects.

## Updated

### Scene view Camera properties

The HDRP-specific Scene view Camera properties, such as anti-aliasing mode and stop NaNs, are no longer in the preferences window and are instead in the [Scene view camera](https://docs.unity3d.com/Manual/SceneViewCamera.html) settings menu.

For information on HDRP's Scene view Camera properties, see [Scene view Camera](Scene-View-Camera.md).

### Shadow caching system

This version of HDRP improves on shadow atlas and shadow caching management. You can now stagger cascade shadows which means you can update each cascade independently. Cached shadows (those that use **OnEnable**) render everything they can see independently of the main view. This version also introduces more API which you can use to more finely control cached shadows. For more information, see [Shadows](Shadows-in-HDRP.md).

### Volumetric fog control modes

Version 10.2 of HDRP introduces the concept of volumetric fog control modes to help you set up volumetric fog in a Scene. The control modes are:

* **Balance**: Uses a performance-oriented approach to define the quality of the volumetric fog.
* **Manual**: Gives you access to the internal set of properties which directly control the effect. This mode is equivalent to the behavior before this update.

### Custom post-processing: new injection point

This version of HDRP introduces a new injection point for custom post-processing effects. You can now inject custom post-processing effects before the [temporal anti-aliasing](Anti-Aliasing.md#TAA) pass.

### Compute shaders now use multi-compile

HDRP, being a high-end modern renderer, contains a lot of compute shader passes. Up until now, to define variations of some compute shaders, HDRP had to manually declare new kernels for each variation. From this version, every compute shader in HDRP uses Unity's multi-compile API which makes maintenance easier, but more importantly allows HDRP to strip shaders that you do not need to improve compilation times.

### Screen Space Reflection

![](Images/HDRP-SSRImprovement.png)

HDRP improves the Screen Space Reflection by providing a new implementation 'PBR Accumulation'

### Planar reflection probe filtering

![](Images/PlanarReflectionFiltering-Feature.png)

Planar reflection probe filtering is a process that combines the result of planar reflection and surfaces smoothness. Up until this version, the implementation for planar reflection probe filtering did not always produce results of fantastic quality. This version of HDRP includes a new implementation that is closer to being physically-based and improves on the image quality significantly.

### Fake distance based roughness for reflection probe

![](Images/DistanceBaseRoughness-Feature.png)

Reflection Probe can now fake the increasing preceive bluriness of a surface reflection with distance from the object. This option is disabled by default and need to be enabled on the Reflection Probe.

### Screen space reflection

[Screen Space Reflection](Override-Screen-Space-Reflection.md) effect always use the color pyramid generate after the Before Refraction transparent pass. Thus the color buffer only includes transparent GameObjects that use the **BeforeRefraction** [Rendering Pass](Surface-Type.md).

### Distortion

The distortion effect now supports a rough distortion which is disabled by default. Disabling Rough Distortion saves resources as the effect does not generate a color pyramid and instead uses a copy of the screen.

### Platform stability

In the past, HDRP experienced stability issues for DirectX12, Vulkan, Metal, Linux. This version includes improvements to stability on these platforms.

### Lightloop optimization

In terms of performance, one of the most resource intensive operations for HDRP is processing lights before it sends them to the GPU. For many high-end projects that include a lot of lights in their Scene, this is particularly problematic. This version of HDRP introduces an optimization that reduces the resource intensity of the light loop by up to 80% which drastically improves CPU performance in the vast majority of cases.

### Decal improvement

HDRP no longer forces a full depth pre-pass when you enable decals in Deferred Lit Mode. Only materials with the **Receive Decals** property enabled render in the pre-pass. Decal shader code has improved and now produces fewer shader variants and includes better UI to control which material attributes the decal affects. Finally, the [Decal Master Stack](master-stack-decal.md) now exposes affects flags control on the Material.

### Constant buffer setup optimization

In terms of performance, preparing and sending the shader data to the GPU is a resource intensive operation. HDRP now uses a new C# constant buffer API which allows it to set up the various shader uniforms in a single call, instead of multiple ones.

### Temporal anti-aliasing

HDRP's previous temporal anti-aliasing (TAA) solution suffered from typical TAA artifacts. The image would look blurry while moving and ghosting artifacts were often present. The new implementation improves significantly on both of these issues and also offers more customizability.

### AxF mapping modes

You can now control the texture mapping mode for all textures in the [AxF Shader](AxF-Shader.md). You can choose between planar, triplanar, or different uv sets.

For more information about this improvement, see [AxF Shader](AxF-Shader.md).

### Contact shadows improvements

More options are now available for [contact shadows](Override-Contact-Shadows.md). You can now set a bias to avoid self-intersection issues as well as use a new thickness property to fill gaps that contact shadows can leave.

### Light component user experience

![](Images/NewLightUX.png)

The [Light component](Light-Component.md) now includes a visualization to help you set the intensity of your lights using [physical light units](Physical-Light-Units.md).

### Exposure

#### Exposure curve mapping

Before this version, to set the minimum and maximum limit for auto-exposure, you had to use fixed values and, because of this, the exposure values fluctuated too much. Now you can use curves for these limits which helps to produce a more stable exposure.

#### Custom mid-grey point

Auto-exposure systems calculate the average scene luminance and try to map this to an average grey value. There is no fixed standard for this grey value and it differs between camera manufacturers. To provide the maximum possible customization, HDRP now allows you to choose the grey value from a selection of the most common middle grey value in industry.

### Custom Pass API

![](Images/CustomPass_API.png)

From this version, within the rendering of your main Camera, you can now render GameObjects from another point of view (a disabled camera for example). The new API also comes with built-in support for rendering Depth, Normal and Tangent into an RTHandle.

You can also use this Camera override to render some GameObjects with a different field of view, like arms in a first-person application.

### Ray tracing

#### Fallback for transparents

Before this version, if transparent GameObjects were set up for [recursive rendering](Ray-Tracing-Recursive-Rendering.md) and you did not use recursive rendering in your Scene, they would disappear. Also, if they were not refractive, they would appear opaque. Now, HDRP has a set off fallbacks so the visuals of transparent GameObjects are coherent with and without recursive rendering and refraction.

#### Shadow quality

The denoiser for [ray-traced shadows](Ray-Traced-Shadows.md) now produces higher quality shadows.

#### Colored shadows

HDRP now supports colored ray-traced shadows for transparent and transmissive GameObjects.

For more information, see [ray-traced shadows](Ray-Traced-Shadows.md).

#### Ray-traced reflection on transparent Materials

HDRP's ray-traced reflection (RTR) solution now support transparent materials. This is useful for transparent surfaces such as windows or water.

#### Virtual Reality (VR)

Ray tracing now supports VR. However, since ray tracing is resource intensive and VR amplifies this, the performance is very slow.

### Render Graph

HDRP now internally uses a Render Graph system. This has no impact on features available to you and it should improve overall memory usage significantly. In the new HDRP template, GPU memory usage decreased by 25%.
More information see [RenderGraph](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@10.2/manual/render-graph-system.html).

### Metallic Remapping

HDRP now uses range remapping for the metallic value when using a mask map on Lit Materials as well as Decals.

### Frame Settings

From 10.x, when you create a new [HDRP Asset](HDRP-Asset), HDRP enables the **Frame Setting** option **MSAA Within Forward** by default.

### Menu

HDRP 10.x renames various HDRP menu items in **Assets** > **Create** > **Shader** to **HD Render Pipeline** for consistency.

### Lighting

#### Spot Lights

From 10.x, when you create a Spot Light from the Editor menu, HDRP enables the **Reflector** property by default.

**Note**: If you create a Spot Light via a C# script HDRP automatically disables the **Reflector** property.

#### Backplate rendering

From 10.x, HDRP disables [Backplate rendering](Override-HDRI-Sky) for lighting cubemaps that aren’t compatible.

#### Reflection Probe interaction

From 10.x, [Screen Space Ambient Occlusion](Override-Ambient-Occlusion), [Screen Space Global Illumination](Override-Screen-Space-GI), [Screen Space Reflection](Override-Screen-Space-Reflection), [Ray Tracing Effects](Ray-Tracing-Getting-Started), and [Volumetric Reprojection](Override-Fog) don’t interact with Reflection Probes. This is because these effects take many frames to render correctly which makes the Reflection Probe’s cubemap appear incorrectly.

#### Static Lighting Sky

From 10.x, the Sky doesn’t affect baked lighting when you disable the sky override your project uses as the **Static Lighting Sky** in the **Lighting** window.

#### Cookies

From 10.x, HDRP includes aCubemap Array for Point Light cookies, and uses octahedral projection with a regular 2D-Cookie atlas. This is to allow a single path for light cookies and IES profiles. However, a low-resolution cube-cookie might create visual artifacts. For example, when you project pixel art data.

#### Color buffer texture format

From 10.x, the Texture format of the color buffer in the HDRP Asset also applies to [Planar Reflection Probes](Planar-Reflection-Probe). In earlier HDRP versions Planar Reflection Probes use a float16 rendertarget.

#### Light layer properties

HDRP 10.x moves the light layer properties from the **HDRP settings** to the **HDRP Default Settings** tab.

#### Sun disk intensity

From 10.x, when you use Physically Based Sky, the sun disk intensity is proportional to its size. In previous versions, the sun disk is incorrectly not driven by the sun disk size.

#### Screen Space Reflection color pyramid

From 10.x, the [Screen Space Reflection](Override-Screen-Space-Reflection) effect always uses the color pyramid HDRP generates after the Before Refraction transparent pass. This means the color buffer only includes transparent GameObjects that use the **BeforeRefraction** [Rendering Pass](Surface-Type). In previous versions, the content depends on whether the Distortion effect is active.

### Shader config file

HDRP 10.x moves the `HDShadowFilteringQuality` enum to `HDShadowManager.cs`.

10.x adds a new option named **ColoredShadow**. It allows you to control whether a shadow is chromatic or monochrome. HDRP enables **ColoredShadow** by default and currently only works with [Ray-traced shadows](Ray-Traced-Shadows).

**Note**: Colored shadows are more resource-intensive to process than standard shadows.

### Shader code

#### ModifyBakedDiffuseLighting()

In 10.x, the prototype for the function `ModifyBakedDiffuseLighting()` in the various materials changes from: `void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, SurfaceData surfaceData, inout BuiltinData builtinData)` to: `void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)`.

#### Rectangular area shadow evaluation

From 10.x, HDRP includes a new rectangular area shadow evaluation function, `EvaluateShadow_RectArea`.

HDRP renames the `GetAreaLightAttenuation()` function to `GetRectAreaShadowAttenuation()`.

HDRP renames the type `DirectionalShadowType` to `SHADOW_TYPE`.

#### Ray tracing multi-compile

HDRP 10.x introduces a new multi-compile for forward pass: `SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON`. This allows you to enable ray tracing without a shader config file.

#### SHADERPASS

From 10.x, `SHADERPASS` for `TransparentDepthPrepass` and `TransparentDepthPostpass` identification uses `SHADERPASS_TRANSPARENT_DEPTH_PREPASS` and `SHADERPASS_TRANSPARENT_DEPTH_POSTPASS`. Previous versions of  `SHADERPASS` use `SHADERPASS_DEPTH_ONLY`.

#### Decal.shader

10.x changes the shader code for the `Decal.shader`. It now uses four, rather than 16, passes to handle how HDRP renders different decal attributes:

* `DBufferProjector`
* `DecalProjectorForwardEmissive`
* `DBufferMesh`
* `DecalMeshForwardEmissive`

`DBufferProjector` and `DBufferMesh` use `multi_compile DECALS_3RT DECALS_4RT` to handle the different decal attributes.

#### <a name="shader-decal-properties"></a> Shader decal properties

HDRP 10.x renames various shader decal properties to match a new set of AffectXXX properties. This version changes:

* `_AlbedoMode` to `_AffectAlbedo`
* `_MaskBlendMode` to `_AffectNormal`
* `_MaskmapMetal` to `_AffectMetal`
* `_MaskmapAO` to `_AffectAO`
* `_MaskmapSmoothness` to `_AffectSmoothness`
* `_Emissive` to `_AffectEmission`

HDRP 10.x renames the keyword, `_ALBEDOCONTRIBUTION`, to `_MATERIAL_AFFECTS_ALBEDO` and adds two new keywords, `_MATERIAL_AFFECTS_NORMAL`, `_MATERIAL_AFFECTS_MASKMAP`.

These new properties match properties from the Decal Shader Graph, which you expose in the Material.

#### Raytracing Quality

HDRP 10.x renames the Raytracing keyword in Shader Graph to **Raytracing Quality**. This version also renames the defines:

* `RAYTRACING_SHADER_GRAPH_LOW` to `RAYTRACING_SHADER_GRAPH_DEFAULT`
* `RAYTRACING_SHADER_GRAPH_HIGH` to `RAYTRACING_SHADER_GRAPH_RAYTRACED`

### Raytracing

From Unity 2020.2, the Raytracing Node in shader graph applies the raytraced path (previously the low path) to all raytraced effects except path tracing.

### Local Volumetric Fog Mask Texture

From Unity 2020.2, you can use the **Texture Importer** to convert a 2D flipbook texture to the 3D format that Density Mask Textures require. For information on how to use the importer to convert the flipbook texture, see the [Local Volumetric Fog documentation](Local-Volumetric-Fog).

### Post Processing

From 2020.2, the Camera rotation clamp in the Motion Blur volume override isn’t active by default. To enable the Camera rotation clamp:

1. Open a GameObject with a volume component in the Inspector and add a **Motion Blur** override.
2. In the **Motion Blur** override, go to **Camera Velocity** and enable **Camera Clamp Mode**.
3. Set **Camera Clamp Mode** to **Rotation** and enable **Rotation Clamp**.

In previous versions of HDRP, the Camera rotation clamp is active by default.

## Removed

### Shader config file

From 10.x, HDRP removes `ShaderConfig.s_DeferredShadowFiltering` and `ShaderOptions.DeferredShadowFiltering` from the source code because they have no effect anymore.

HDRP 10.x removes the Ray Tracing option and equivalent shader macro `SHADEROPTIONS_RAYTRACING`. You no longer need to edit the shader config file to use raytraced effects in HDRP.

### Shader code

HDRP 10.x removes

* the defines `CUTOFF_TRANSPARENT_DEPTH_PREPASS` and `CUTOFF_TRANSPARENT_DEPTH_POSTPASS`.
* the macros `ENABLE_RAYTRACING`, `SHADEROPTIONS_RAYTRACING`, and `RAYTRACING_ENABLED`.
* the shader keywords `_BLENDMODE_ALPHA`, `_BLENDMODE_ADD` and `_BLENDMODE_PRE_MULTIPLY` and replaces them with `_Blendmode`.
* the shader keyword `_BLENDMODE_PRESERVE_SPECULAR_LIGHTING` and replaces it with `_EnableBlendModePreserveSpecularLighting`.

For example in Material.hlsl, this version replaces the following lines:

```C#
#if defined(_BLENDMODE_ADD) || defined(_BLENDMODE_ALPHA)
        return float4(diffuseLighting * opacity + specularLighting, opacity);
```

with

```C#
   if (_BlendMode == BLENDMODE_ALPHA || _BlendMode == BLENDMODE_ADDITIVE)
        return float4(diffuseLighting * opacity + specularLighting * (
#ifdef SUPPORT_BLENDMODE_PRESERVE_SPECULAR_LIGHTING
        _EnableBlendModePreserveSpecularLighting ? 1.0f :
#endif
            opacity), opacity);
```

This reduces the number of shader variants. If you are using custom shaders, you might have to move the include of Material.hlsl after the declaration of the property `_Blendmode`.

If you want the custom shader to support the blend mode preserve specular option, you need to define the `_EnableBlendModePreserveSpecularLighting` property and the `SUPPORT_BLENDMODE_PRESERVE_SPECULAR_LIGHTING` compile time constant.

## Issues resolved

For information on issues resolved in version 10 of HDRP, see the [changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@10.2/changelog/CHANGELOG.html).
