# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [5.7.2] - 2019-03-09
- Fixed gles2 build error.
- Fixed post-processing blit shader error in gles2.

## [5.7.1] - 2019-03-08
### Fixed
- Fixed a project import issue in the LWRP template.

## [5.7.0] - 2019-03-07
### Added
- Added support for overriding terrain detail rendering shaders, via the render pipeline editor resources asset
- You can now create a custom forward renderer by clicking on `Assets/Create/Rendering/Lightweight Render Pipeline/Forward Renderer`. This creates an Asset in your Project. You can add additional features to it and drag-n-drop the renderer to either the pipeline Asset or to a camera.
- You can now add `ScriptableRendererFeature`  to the `ScriptableRenderer` to extend it with custom effects. A feature is an `ScriptableObject` that can be drag-n-dropped in the renderer and adds one or more `ScriptableRenderPass` to the renderer.
- `ScriptableRenderer` now exposes interface to configure lights. To do so, implement `SetupLights` when you create a new renderer.
- `ScriptableRenderer` now exposes interface to configure culling. To do so, implement `SetupCullingParameters` when you create a new renderer.
- `ScriptableRendererData` contains rendering resources for `ScriptableRenderer`. A renderer can be overridden globally for all cameras or on a per-camera basis.
- `ScriptableRenderPass` now has a `RenderPassEvents`. This controls where in the pipeline the render pass is added.
- `ScriptableRenderPass` now exposes `ConfigureTarget` and `ConfigureClear`. This allows the renderer to automatically figure out the currently active rendering targets.
- `ScriptableRenderPass` now exposes `Blit`. This performs a blit and sets the active render target in the renderer.
- `ScriptableRenderPass` now exposes `RenderPostProcessing`. This renders post-processing and sets the active render target in the renderer.
- `ScriptableRenderPass` now exposes `CreateDrawingSettings` as a helper for render passes that need to call `ScriptableRenderContext.DrawRenderers`.

### Changed
- Removed `RegisterShaderPassName` from `ScriptableRenderPass`. Instead, `CreateDrawingSettings` now  takes one or a list of `ShaderTagId`. 
- Removed remaining experimental namespace from LWRP. All APIrelated to `ScriptableRenderer`, `ScriptableRenderPass`, and render pass injection is now out of preview.
- Removed `SetRenderTarget` from `ScriptableRenderPass`. You should never call it. Instead, call `ConfigureTarget`, and the renderer automatically sets up targets for you. 
- Removed `RenderFullscreenQuad` from `ScriptableRenderer`. Use `CommandBuffer.DrawMesh` and `RenderingUtils.fullscreenMesh` instead.
- Removed `RenderPostProcess` from `ScriptableRenderer`. Use `ScriptableRenderPass.RenderPostProcessing` instead.
- Removed `postProcessingContext` property from `ScriptableRenderer`. This is now exposed in `RenderingUtils.postProcessingContext`.
- Removed `GetCameraClearFlag` from `ScriptableRenderer`.

### Fixed
- Terrain detail rendering now works correctly when LWRP is installed but inactive.
- Fixed y-flip in VR when post-processing is active.
- Fixed occlusion mesh for VR not rendering before rendering opaques.
- Enabling or disabling SRP Batcher in runtime works now.
- Fixed video player recorder when post-processing is enabled.

## [5.6.0] - 2019-02-21

## [5.5.0] - 2019-02-18
### Changed
- Code refactor: all macros with ARGS have been swapped with macros with PARAM. This is because the ARGS macros were incorrectly named.

## [5.4.0] - 2019-02-11
### Fixed
- Fixed the SRP Batcher, so it now works with MacOS and iOS.

## [5.3.1] - 2019-01-28

### Fixed
- Fixed Per-object reflection probes.

## [5.3.0] - 2019-01-28
### Added
- LWRP now uses the Unity recorder extension. You can use this to capture the output of Cameras.
- You can now inject a custom render pass before LWRP renders opaque objects. To do so, implement an `IBeforeRender` interface.
- Baked Lit Shader, which uses global illumination via Light Probes and lightmaps, but no real-time lighting. 
- Distortion support in all Particle Shaders.
- An upgrade system for LWRP Materials with `MaterialPostprocessor`.
- An upgrade path for Unlit shaders
- Tooltips for Shaders.
- SRP Batcher support for Particle Shaders.
- Docs for these Shaders: Baked Lit, Particles Lit, Particles Simple Lit, and Particles Unlit.
- LWRP now supports dynamic resolution scaling. The target platform must also support it.
- LWRP now includes version defines for both C# and Shaders in the format of `LWRP_X_Y_Z_OR_NEWER`. For example, `LWRP_5_3_0_OR_NEWER` defines version 5.3.0.
- The Terrain Lit Shader now samples Spherical Harmonics if you haven't baked any lightmaps for terrain.
- Added a __Priority__ option, which you can use to tweak the rendering order. This is similar to render queue in the built-in render pipeline. These Shaders now have this option: Lit, Simple Lit, Baked Lit, Unlit, and all three Particle Shaders.

### Changed
- You can now only initialize a camera by setting a Background Type. The supported options are Skybox, Solid Color, and Don't Care.
- LWRP now uses non-square shadowmap textures when it renders directional shadows with 2 shadow cascades. 
- LWRP now uses RGB111110 as the HDR format on mobile devices, when this format is supported.
- Removed `IAfterDepthPrePass` interface.
- We’ve redesigned the Shader GUI. For example, all property names in Shaders are now inline across the board
- The Simple Lit Shader now has Smoothness, which can be stored in the alpha of specular or albedo maps.
- The Simple Lit and Particles Simple Lit Shaders now take shininess from the length (brightness) of the specular map.
- The __Double sided__ property is now __Render Face__. This means you can also do front face culling.
- Changed the docs for Lit Shader, Simple Lit Shader and Unlit Shader according to Shader GUI changes.
- When you create a new LWRP Asset, it will now be initialized with settings that favor performance on mobile platforms.
- Updated the [FAQ](faq.md) and the [Built-in/LWRP feature comparison table](lwrp-builtin-feature-comparison.md).

### Fixed
- Several tweaks to reduce bandwidth consumption on mobile devices.
- The foldouts in the Lightweight Asset inspector UI now remember their state.
- Added missing meta file for GizmosRenderingPass.cs.
- Fixed artifacts when using multiple or Depth Only cameras. [Case 1072615](https://issuetracker.unity3d.com/issues/ios-using-multiple-cameras-in-the-scene-in-lightweight-render-pipeline-gives-corrupted-image-in-ios-device)
- Fixed a typo in ERROR_ON_UNSUPPORTED_FUNCTION() that was causing the shader compiler to run out of memory in GLES2. [Case 1104271](https://issuetracker.unity3d.com/issues/mobile-os-restarts-because-of-high-memory-usage-when-compiling-shaders-for-opengles2)
- LWRP now renders shadows on scaled objects correctly. [Case 1109017](https://issuetracker.unity3d.com/issues/scaled-objects-render-shadows-and-specularity-incorrectly-in-the-lwrp-on-device)
- LWRP now allows some Asset settings to be changed at runtime. [Case 1105552](https://issuetracker.unity3d.com/issues/lwrp-changing-render-scale-in-runtime-has-no-effect-since-lwrp-3-dot-3-0)
- Realtime shadows now work in GLES2. [Case 1087251](https://issuetracker.unity3d.com/issues/android-lwrp-no-real-time-light-and-shadows-using-gles2)
- Framedebugger now renders correctly when stepping through drawcalls.
- Cameras that request MSAA and Opaque Textures now use less frame bandwidth when they render.
- Fixed rendering in the gamma color space, so it doesn't appear darker.
- Particles SImple Lit and Particles Unlit Shaders now work correctly.
- __Soft Particles__ now work correctly.
- Camera fading for particles.
- Fixed a typo in the Unlit `IgnoreProjector` tag.
- Particles render in both eyes with stereo instancing
- Fixed specular issues on mobile. [case 1109017](https://issuetracker.unity3d.com/issues/scaled-objects-render-shadows-and-specularity-incorrectly-in-the-lwrp-on-device)
- Fixed issue causing LWRP to create MSAA framebuffer even when MSAA setting was disabled.
- Post-processing in mobile VR is now forced to be disabled. It was causing many rendering issues.
- Fixed Editor Previews breaking in Play Mode when VR is enabled. [Case 1109009](https://issuetracker.unity3d.com/issues/lwrp-editor-previews-break-in-play-mode-if-vr-is-enabled)
- A camera's HDR enable flag is now respected when rendering in XR.

## [5.2.0] - 2018-11-27
### Added
- LWRP now handles blits that are required by the device when rendering to the backbuffer.
- You can now enable the SRP Batcher. To do so, go to the `Pipeline Asset`. Under `Advanced`, toggle `SRP Batcher`.

### Changed
- Renamed shader variable `unity_LightIndicesOffsetAndCount` to `unity_PerObjectLightData`.
- Shader variables `unity_4LightIndices0` and `unity_4LightIndices1` are now declared as `unity_PerObjectLightIndices` array.

## [5.1.0] - 2018-11-19
### Added
- The user documentation for LWRP is now in this GitHub repo, instead of in the separate GitHub wiki. You can find the most up-to-date pages in the [TableOfContents.md](TableOfCotents.md) file. Pages not listed in that file are still in progress.

### Changed
- The LWRP package is no longer in preview.
- LWRP built-in render passes are now internal.
- Changed namespace from `UnityEngine.Experimental.Rendering.LightweightPipeline` to `UnityEngine.Rendering.LWRP`.
- Changed namespace from `UnityEditor.Experimental.Rendering.LightweightPipeline` to `UnityEditor.Rendering.LWRP`.

### Fixed
- LWRP now respects the iOS Player setting **Force hard shadows**. When you enable this setting, hardware filtering of shadows is disabled.
- Scene view mode now renders baked lightmaps correctly. [Case 1092227](https://issuetracker.unity3d.com/issues/lwrp-scene-view-modes-render-objects-black)
- Shadow bias calculations are now correct for both Shader Graph and Terrain shaders.
- Blit shader now ignores culling.
- When you select __Per Vertex__ option for __Additional Lights__, the __Per Object Limit__ option is not greyed out anymore.
- When you change camera viewport height to values above 1.0, the Unity Editor doesn't freeze anymore. [Case 1097497](https://issuetracker.unity3d.com/issues/macos-lwrp-editor-freezes-after-changing-cameras-viewport-rect-values)
- When you use AR with LWRP, the following error message is not displayed in the console anymore: "The camera list passed to the render pipeline is either null or empty."

## [5.0.0-preview] - 2018-09-28
### Added
- Added occlusion mesh rendering/hookup for VR
- You can now configure default depth and normal shadow bias values in the pipeline asset.
- You can now add the `LWRPAdditionalLightData` component to a `Light` to override the default depth and normal shadow bias.
- You can now log the amount of shader variants in your build. To do so, go to the `Pipeline Asset`. Under `Advanced`, select and set the `Shader Variant Log Level`.
### Changed
- Removed the `supportedShaderFeatures` property from LWRP core. The shader stripper now figures out which variants to strip based on the current assigned pipeline Asset in the Graphics settings.
### Fixed
- The following error does not appear in console anymore: ("Begin/End Profiler section mismatch")
- When you select a material with the Lit shader, this no longer causes the following error in the console: ("Material doesn't have..."). [case 1092354](https://fogbugz.unity3d.com/f/cases/1092354/)
- In the Simple Lit shader, per-vertex additional lights are now shaded properly.
- Shader variant stripping now works when you're building a Project with Cloud Build. This greatly reduces build times from Cloud Build.
- Dynamic Objects now receive lighting when the light mode is set to mixed.
- MSAA now works on Desktop platforms.
- The shadow bias value is now computed correctly for shadow cascades and different shadow resolutions. [case 1076285](https://issuetracker.unity3d.com/issues/lwrp-realtime-directional-light-shadow-maps-exhibit-artifacts)
- When you use __Area Light__ with LWRP, __Cast Shadows__ no longer overlaps with other UI elements in the Inspector. [case 1085363](https://issuetracker.unity3d.com/issues/inspector-area-light-cast-shadows-ui-option-is-obscured-by-render-mode-for-lwrp-regression-in-2018-dot-3a3)

### Changed
Read/write XRGraphicsConfig -> Read-only XRGraphics interface to XRSettings. 

## [4.0.0-preview] - 2018-09-28
### Added
- When you have enabled Gizmos, they now appear correctly in the Game view.
- Added requiresDepthPrepass field to RenderingData struct to tell if the runtime platform requires a depth prepass to generate a camera depth texture.
- The `RenderingData` struct now holds a reference to `CullResults`.
- When __HDR__ is enabled in the Camera but disabled in the Asset, an information box in the Camera Inspector informs you about it.
- When __MSAA__ is enabled in the Camera but disabled in the Asset, an information box in the Camera Inspector informs you about it.
- Enabled instancing on the terrain shader.
- Sorting of opaque objects now respects camera `opaqueSortMode` setting.
- Sorting of opaque objects disables front-to-back sorting flag, when camera settings allow that and the GPU has hidden surface removal.
- LWRP now has a Custom Light Explorer that suits its feature set.
- LWRP now supports Vertex Lit shaders for detail meshes on terrain.
- LWRP now has three interactive Autodesk shaders: Autodesk Interactive, Autodesk Interactive Masked and Autodesk Interactive Transparent.
- [Shader API] The `GetMainLight` and `GetAdditionalLight` functions can now compute shadow attenuation and store it in the new `shadowAttenuation` field in `LightData` struct.
- [Shader API] Added a `VertexPositionInputs` struct that contains vertex position in difference spaces (world, view, hclip).
- [Shader API] Added a `GetVertexPositionInputs` function to get an initialized `VertexPositionInputs`.
- [Shader API] Added a `GetPerObjectLightIndex` function to return the per-object index given a for-loop index.
- [Shader API] Added a `GetShadowCoord` function that takes a `VertexPositionInputs` as input.
- [ShaderLibrary] Added VertexNormalInputs struct that contains data for per-pixel normal computation.
- [ShaderLibrary] Added GetVertexNormalInputs function to return an initialized VertexNormalInputs.

### Changed
- The `RenderingData` struct is now read-only.
- `ScriptableRenderer`always performs a Clear before calling `IRendererSetup::Setup.` 
- `ScriptableRenderPass::Execute` no longer takes `CullResults` as input. Instead, use `RenderingData`as input, since that references `CullResults`.
- `IRendererSetup_Setup` no longer takes `ScriptableRenderContext` and `CullResults` as input.
- Shader includes are now referenced via package relative paths instead of via the deprecated shader export path mechanism https://docs.unity3d.com/2018.3/Documentation/ScriptReference/ShaderIncludePathAttribute.html.
- The LWRP Asset settings were re-organized to be more clear.
- Vertex lighting now controls if additional lights should be shaded per-vertex or per-pixel.
- Renamed all `Local Lights` nomenclature to `Additional Lights`.
- Changed shader naming to conform to our SRP shader code convention.
- [Shader API] Renamed `SpotAttenuation` function to `AngleAttenuation`.
- [Shader API] Renamed `_SHADOWS_ENABLED` keyword to `_MAIN_LIGHT_SHADOWS`
- [Shader API] Renamed `_SHADOWS_CASCADE` keyword to `_MAIN_LIGHT_SHADOWS_CASCADE`
- [Shader API] Renamed `_VERTEX_LIGHTS` keyword to `_ADDITIONAL_LIGHTS_VERTEX`.
- [Shader API] Renamed `_LOCAL_SHADOWS_ENABLED` to `_ADDITIONAL_LIGHT_SHADOWS`
- [Shader API] Renamed `GetLight` function to `GetAdditionalLight`.
- [Shader API] Renamed `GetPixelLightCount` function to `GetAdditionalLightsCount`.
- [Shader API] Renamed `attenuation` to `distanceAttenuation` in `LightData`.
- [Shader API] Renamed `GetLocalLightShadowStrength` function to `GetAdditionalLightShadowStrength`.
- [Shader API] Renamed `SampleScreenSpaceShadowMap` functions to `SampleScreenSpaceShadowmap`.
- [Shader API] Renamed `MainLightRealtimeShadowAttenuation` function to `MainLightRealtimeShadow`.
- [Shader API] Renamed light constants from `Directional` and `Local` to `MainLight` and `AdditionalLights`.
- [Shader API] Renamed `GetLocalLightShadowSamplingData` function to `GetAdditionalLightShadowSamplingData`.
- [Shader API] Removed OUTPUT_NORMAL macro.
- [Shader API] Removed `lightIndex` and `substractiveAttenuation` from `LightData`.
- [Shader API] Removed `ComputeShadowCoord` function. `GetShadowCoord` is provided instead.
- All `LightweightPipeline` references in API and classes are now named `LightweightRenderPipeline`.
- Files no longer have the `Lightweight` prefix.
- Renamed Physically Based shaders to `Lit`, `ParticlesLit`, and `TerrainLit`.
- Renamed Simple Lighting shaders to `SimpleLit`, and `ParticlesSimpleLit`.
- [ShaderLibrary] Renamed `InputSurfacePBR.hlsl`, `InputSurfaceSimple.hlsl`, and `InputSurfaceUnlit` to `LitInput.hlsl`, `SimpleLitInput.hlsl`, and `UnlitInput.hlsl`. These files were moved from the `ShaderLibrary` folder to the`Shaders`.
- [ShaderLibrary] Renamed `LightweightPassLit.hlsl` and `LightweightPassLitSimple.hlsl` to `LitForwardPass.hlsl` and `SimpleLitForwardPass.hlsl`. These files were moved from the `ShaderLibrary` folder to `Shaders`.
- [ShaderLibrary] Renamed `LightweightPassMetaPBR.hlsl`, `LightweightPassMetaSimple.hlsl` and `LighweightPassMetaUnlit` to `LitMetaPass.hlsl`, `SimpleLitMetaPass.hlsl` and `UnlitMetaPass.hlsl`. These files were moved from the `ShaderLibrary` folder to `Shaders`.
- [ShaderLibrary] Renamed `LightweightPassShadow.hlsl` to `ShadowCasterPass.hlsl`. This file was moved to the `Shaders` folder.
- [ShaderLibrary] Renamed `LightweightPassDepthOnly.hlsl` to `DepthOnlyPass.hlsl`. This file was moved to the `Shaders` folder.
- [ShaderLibrary] Renamed `InputSurfaceTerrain.hlsl` to `TerrainLitInput.hlsl`. This file was moved to the `Shaders` folder.
- [ShaderLibrary] Renamed `LightweightPassLitTerrain.hlsl` to `TerrainLitPases.hlsl`. This file was moved to the `Shaders` folder.
- [ShaderLibrary] Renamed `ParticlesPBR.hlsl` to `ParticlesLitInput.hlsl`. This file was moved to the `Shaders` folder.
- [ShaderLibrary] Renamed `InputSurfacePBR.hlsl` to `LitInput.hlsl`. This file was moved to the `Shaders` folder.
- [ShaderLibrary] Renamed `InputSurfaceUnlit.hlsl` to `UnlitInput.hlsl`. This file was moved to the `Shaders` folder.
- [ShaderLibrary] Renamed `InputBuiltin.hlsl` to `UnityInput.hlsl`.
- [ShaderLibrary] Renamed `LightweightPassMetaCommon.hlsl` to `MetaInput.hlsl`.
- [ShaderLibrary] Renamed `InputSurfaceCommon.hlsl` to `SurfaceInput.hlsl`.
- [ShaderLibrary] Removed LightInput struct and GetLightDirectionAndAttenuation. Use GetAdditionalLight function instead.
- [ShaderLibrary] Removed ApplyFog and ApplyFogColor functions. Use MixFog and MixFogColor instead.
- [ShaderLibrary] Removed TangentWorldToNormal function. Use TransformTangentToWorld instead.
- [ShaderLibrary] Removed view direction normalization functions. View direction should always be normalized per pixel for accurate results.
- [ShaderLibrary] Renamed FragmentNormalWS function to NormalizeNormalPerPixel.

### Fixed
- If you have more than 16 lights in a scene, LWRP no longer causes random glitches while rendering lights.
- The Unlit shader now samples Global Illumination correctly.
- The Inspector window for the Unlit shader now displays correctly.
- Reduced GC pressure by removing several per-frame memory allocations.
- The tooltip for the the camera __MSAA__ property now appears correctly.
- Fixed multiple C# code analysis rule violations.
- The fullscreen mesh is no longer recreated upon every call to `ScriptableRenderer.fullscreenMesh`.

## [3.3.0-preview]
### Added
- Added callbacks to LWRP that can be attached to a camera (IBeforeCameraRender, IAfterDepthPrePass, IAfterOpaquePass, IAfterOpaquePostProcess, IAfterSkyboxPass, IAfterTransparentPass, IAfterRender)

###Changed
- Clean up LWRP creation of render textures. If we are not going straight to screen ensure that we create both depth and color targets.
- UNITY_DECLARE_FRAMEBUFFER_INPUT and UNITY_READ_FRAMEBUFFER_INPUT macros were added. They are necessary for reading transient attachments.
- UNITY_MATRIX_I_VP is now defined.
- Renamed LightweightForwardRenderer to ScriptableRenderer.
- Moved all light constants to _LightBuffer CBUFFER. Now _PerCamera CBUFFER contains all other per camera constants.
- Change real-time attenuation to inverse square.
- Change attenuation for baked GI to inverse square, to match real-time attenuation.
- Small optimization in light attenuation shader code.

### Fixed
- Lightweight Unlit shader UI doesn't throw an error about missing receive shadow property anymore.

## [3.2.0-preview]
### Changed
- Receive Shadows property is now exposed in the material instead of in the renderer.
- The UI for Lightweight asset has been updated with new categories. A more clean structure and foldouts has been added to keep things organized.

### Fixed
- Shadow casters are now properly culled per cascade. (case 1059142)
- Rendering no longer breaks when Android platform is selected in Build Settings. (case 1058812)
- Scriptable passes no longer have missing material references. Now they access cached materials in the renderer.(case 1061353)
- When you change a Shadow Cascade option in the Pipeline Asset, this no longer warns you that you've exceeded the array size for the _WorldToShadow property.
- Terrain shader optimizations.

## [3.1.0-preview]

### Fixed
- Fixed assert errors caused by multi spot lights
- Fixed LWRP-DirectionalShadowConstantBuffer params setting

## [3.0.0-preview]
### Added
- Added camera additional data component to control shadows, depth and color texture.
- pipeline now uses XRSEttings.eyeTextureResolutionScale as renderScale when in XR.
- New pass architecture. Allows for custom passes to be written and then used on a per camera basis in LWRP

### Changed
- Shadow rendering has been optimized for the Mali Utgard architecture by removing indexing and avoiding divisions for orthographic projections. This reduces the frame time by 25% on the Overdraw benchmark.
- Removed 7x7 tent filtering when using cascades.
- Screenspace shadow resolve is now only done when rendering shadow cascades.
- Updated the UI for the Lighweight pipeline asset.
- Update assembly definitions to output assemblies that match Unity naming convention (Unity.*).

### Fixed
- Post-processing now works with VR on PC.
- PS4 compiler error
- Fixed VR multiview rendering by forcing MSAA to be off. There's a current issue in engine that breaks MSAA and Texture2DArray.
- Fixed UnityPerDraw CB layout
- GLCore compute buffer compiler error
- Occlusion strength not being applied on LW standard shaders
- CopyDepth pass is being called even when a depth from prepass is available
- GLES2 shader compiler error in IntegrationTests
- Can't set RenderScale and ShadowDistance by script
- VR Single Pass Instancing shadows
- Fixed compilation errors on Nintendo Switch (limited XRSetting support).

## [2.0.0-preview]

### Added
- Explicit render target load/store actions were added to improve tile utilization
- Camera opaque color can be requested on the pipeline asset. It can be accessed in the shader by defining a _CameraOpaqueTexture. This can be used as an alternative to GrabPass.
- Dynamic Batching can be enabled in the pipeline asset
- Pipeline now strips unused or invalid variants and passes based on selected pipeline capabilities in the asset. This reduces build and memory consuption on target.
- Shader stripping settings were added to pipeline asset

### Changed
#### Pipeline
- Pipeline code is now more modular and extensible. A ForwardRenderer class is initialized by the pipeline with RenderingData and it's responsible for enqueueing and executing passes. In the future pluggable renderers will be supported.
- On mobile 1 directional light + up to 4 local lights (point or spot) are computed
- On other platforms 1 directional light + up to 8 local lights are computed
- Multiple shadow casting lights are supported. Currently only 1 directional + 4 spots light shadows.
#### Shading Framework
- Directional Lights are always considered a main light in shader. They have a fast shading path with no branching and no indexing.
- GetMainLight() is provided in shader to initialize Light struct with main light shading data. 
- Directional lights have a dedicated shadowmap for performance reasons. Shadow coord always comes from interpolator.
- MainLigthRealtimeShadowAttenuation(float4 shadowCoord) is provided to compute main light realtime shadows.
- Spot and Point lights are always shaded in the light loop. Branching on uniform and indexing happens when shading them.
- GetLight(half index, float3 positionWS) is provided in shader to initialize Light struct for spot and point lights.
- Spot light shadows are baked into a single shadow atlas.
- Shadow coord for spot lights is always computed on fragment.
- Use LocalLightShadowAttenuation(int lightIndex, float3 positionWS) to comppute realtime shadows for spot lights.

### Fixed
- Issue that was causing VR on Android to render black
- Camera viewport issues
- UWP build issues
- Prevent nested camera rendering in the pipeline

## [1.1.4-preview]

### Added
 - Terrain and grass shaders ported
 - Updated materials and shader default albedo and specular color to midgrey.
 - Exposed _ScaledScreenParams to shader. It works the same as _ScreenParams but takes pipeline RenderScale into consideration
 - Performance Improvements in mobile

### Fixed
 - SRP Shader library issue that was causing all constants to be highp in mobile
 - shader error that prevented LWRP to build to UWP
 - shader compilation errors in Linux due to case sensitive includes
 - Rendering Texture flipping issue
 - Standard Particles shader cutout and blending modes
 - crash caused by using projectors
 - issue that was causing Shadow Strength to not be computed on mobile
 - Material Upgrader issue that caused editor to SoftLocks
 - GI in Unlit shader
 - Null reference in the Unlit material shader GUI

## [1.1.2-preview]

### Changed
 - Performance improvements in mobile  

### Fixed
 - Shadows on GLES 2.0
 - CPU performance regression in shadow rendering
 - Alpha clip shadow issues
 - Unmatched command buffer error message
 - Null reference exception caused by missing resource in LWRP
 - Issue that was causing Camera clear flags was being ignored in mobile


## [1.1.1-preview]

### Added
 - Added Cascade Split selection UI
 - Added SHADER_HINT_NICE_QUALITY. If user defines this to 1 in the shader Lightweight pipeline will favor quality even on mobile platforms.

### Changed
 - Shadowmap uses 16bit format instead of 32bit.
 - Small shader performance improvements

### Fixed
 - Subtractive Mode
 - Shadow Distance does not accept negative values anymore


## [0.1.24]

### Added
 - Added Light abstraction layer on lightweight shader library.
 - Added HDR global setting on pipeline asset. 
 - Added Soft Particles settings on pipeline asset.
 - Ported particles shaders to SRP library

### Changed
 - HDR RT now uses what format is configured in Tier settings.
 - Refactored lightweight standard shaders and shader library to improve ease of use.
 - Optimized tile LOAD op on mobile.
 - Reduced GC pressure
 - Reduced shader variant count by ~56% by improving fog and lightmap keywords
 - Converted LW shader library files to use real/half when necessary.

### Fixed
 - Realtime shadows on OpenGL
 - Shader compiler errors in GLES 2.0
 - Issue sorting issues when BeforeTransparent custom fx was enabled.
 - VR single pass rendering.
 - Viewport rendering issues when rendering to backbuffer.
 - Viewport rendering issues when rendering to with MSAA turned off.
 - Multi-camera rendering.

## [0.1.23]

### Added
 - UI Improvements (Rendering features not supported by LW are hidden)

### Changed
 - Shaders were ported to the new SRP shader library. 
 - Constant Buffer refactor to use new Batcher
 - Shadow filtering and bias improved.
 - Pipeline now updates color constants in gamma when in Gamma colorspace.
 - Optimized ALU and CB usage on Shadows.
 - Reduced shader variant count by ~33% by improving shadow and light classification keywords
 - Default resources were removed from the pipeline asset.

### Fixed
 - Fixed shader include path when using SRP from package manager.
 - Fixed spot light attenuation to match Unity Built-in pipeline.
 - Fixed depth pre-pass clearing issue.

## [0.1.12]

### Added
 - Standard Unlit shader now has an option to sample GI.
 - Added Material Upgrader for stock Unity Mobile and Legacy Shaders.
 - UI improvements

### Changed
- Realtime shadow filtering was improved. 

### Fixed
 - Fixed an issue that was including unreferenced shaders in the build.
 - Fixed a null reference caused by Particle System component lights.


## [0.1.11]
 Initial Release.
