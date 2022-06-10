# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [12.1.7] - 2022-03-29

Version Updated
The version number for this package has increased due to a version update of a related graphics package.


## [12.1.6] - 2022-02-09

### Fixed
- Fixed incorrect shadow batching and shadow length [case 1387859](https://issuetracker.unity3d.com/issues/shadow-caster-2d-casts-artifacted-shadows)
- Fixed an issue where 2D global lights with shadows enabled could break light layer batching [case 1376487](https://issuetracker.unity3d.com/issues/2d-urp-upgrading-global-light-sets-shadow-settings-to-enabled)
- Fixed Light2D Sprite Light not updating when Sprite properties are modified [case 1396416][case 1396418][case 1396422]
- Fixed decal automatic technique to correctly work with webgl. [case 1370326](https://issuetracker.unity3d.com/issues/pink-textures-appear-on-decal-projector-when-building-to-webgl2-and-decal-technique-is-set-to-automatic)

## [12.1.5] - 2022-01-14

### Fixed
- Fixed FXAA quality issues when render scale is not 1.0.
- Fixed an issue in where the _ScreenParams is not setup correctly.

### Added
- Added support for user-selected upscaling filters. Current options are automatic, bilinear, and nearest-neighbor.
- Added support for FidelityFX Super Resolution 1.0 upscaling filter.

### Changed
- Re-added the menu button to be able to convert selected materials.

### Fixed
- Fixed incorrect blending of ParticleUnlit. [case 1373188](https://issuetracker.unity3d.com/product/unity/issues/guid/1373188/)
- Fixed max light count cpu/gpu mismatch in Editor with Android target. [case 1392965](https://issuetracker.unity3d.com/product/unity/issues/guid/1392965/)
- Fixed single channel compressed (BC4) cookies on main light.
- Fixed an issue with too many variants being included in ShaderGraph shaders used in URP. [[case 1378545](https://issuetracker.unity3d.com/issues/some-lit-shaders-are-having-huge-count-of-variants-which-leads-to-project-build-prevention)]
- Fixed several Native RenderPass issues regarding input attachments, DepthOnly pass, Decals.

## [12.1.4] - 2021-12-07

### Added
- Added Adaptive Performance Decals scaler access.

### Fixed
- Fix mismatch on some platforms between Editor-side and Runtime-side implementations of UnityEngine.Rendering.Universal.DecalRendererFeature.IsAutomaticDBuffer() [case 1364134]
- Fixed incorrect light indexing on Windows Editor with Android target. [case 1378103](https://issuetracker.unity3d.com/product/unity/issues/guid/1378103/)
- Fixed Lens Flare not accounting Render Scale setting. [case 1376820](https://issuetracker.unity3d.com/issues/urp-lens-flare-do-not-account-for-render-scale-setting)
- Fixed a performance regression in the 2D renderer regarding the PostProcessPass [case 1347893]
- Fixed a regression where filtering the scene view yielded incorrect visual results [case 1360233](https://issuetracker.unity3d.com/product/unity/issues/guid/1360233)
- VFX: Incorrect Decal rendering when rendescale is different than one [case 1343674](https://issuetracker.unity3d.com/product/unity/issues/guid/1343674/)
- Fixed decal compilation issue on mac.
- Fixed incorrect lighting attenuation on Editor when build target is a mobile platform [case 1387142]

## [12.1.3] - 2021-11-17

## Fixed
- Fixed broken soft shadow filtering. [case 1374960](https://issuetracker.unity3d.com/product/unity/issues/guid/1374960/)

## [12.1.2] - 2021-10-22

### Fixed
- Fixed an issue in where installing the Adaptive Performance package caused errors to the inspector UI [1368161](https://issuetracker.unity3d.com/issues/urp-package-throws-compilation-error-cs1525-when-imported-together-with-adaptive-performance-package)
- Fixed post processing with Pixel Perfect camera [case 1363763](https://issuetracker.unity3d.com/product/unity/issues/guid/1363763/)
- Fixed disabled debug lighting modes on Vulkan and OpenGL following a shader compiler fix. [case 1334240]
- Fixed incorrect behavior of Reflections with Smoothness lighting debug mode. [case 1374181]
- Fixed an issue in where the Convert Renderering Settings would cause a freeze. [case 1353885](https://issuetracker.unity3d.com/issues/urp-builtin-to-urp-render-pipeline-converter-freezes-the-editor-when-converting-rendering-settings)
- Fixed performance regression for 2D shaders where alpha discard was disabled. [case 1335648]
- Fixed an issue with MSAA falling back to the incorrect value when sample count 2 is not supported on some Android GPUs.

## [12.1.1] - 2021-10-04

### Fixed
- Fixed a regression bug where XR camera postion can not be modified in beginCameraRendering [case 1365000]
- Fix for rendering thumbnails. [case 1348209](https://issuetracker.unity3d.com/issues/preview-of-assets-do-not-show-in-the-project-window)
- Fix shadow rendering correctly to work with shader stripping in WebGl. [case 1381881](https://issuetracker.unity3d.com/issues/webgl-urp-mesh-is-not-rendered-in-the-scene-on-webgl-build)

## [12.1.0] - 2021-09-23

### Added
- URP global setting for stripping post processing shader variants.
- URP global setting for stripping off shader variants.

### Changed
- URP will no longer render via an intermediate texture unless actively required by a Renderer Feature. See the upgrade guide for compatibility options and how assets are upgraded.
- Main light shadow, additional light shadow and additional light keywords are now enabled based on urp setting instead of existence in scene. This allows better variant stripping.


### Fixed
- Fixed a Universal Targets in ShaderGraph not rendering correctly in game view [1352225]
- MaterialReimporter.ReimportAllMaterials and MaterialReimporter.ReimportAllHDShaderGraphs now batch the asset database changes to improve performance.
- Fix for rendering thumbnails. [case 1348209](https://issuetracker.unity3d.com/issues/preview-of-assets-do-not-show-in-the-project-window)
- Fixed ShaderGraph needing updated normals for ShadowCaster in URP.
- Fixed a regression bug where XR camera postion can not be modified in beginCameraRendering [case 1365000]

## [12.0.0] - 2021-01-11
### Added
- Added support for default sprite mask shaders for the 2D Renderer in URP.
- Added View Vector node to mimic old behavior of View Direction node in URP.
- Added support for the PlayStation 5 platform.
- Enabled deferred renderer in UI.
- Added support for light layers, which uses Rendering Layer Masks to make Lights in your Scene only light up specific Meshes.
- 2D Light Texture Node. A Shader Graph node that enable sampling of the Light Textures generated by the 2D Renderer in a lit scene.
- Fixed an error where multisampled texture being bound to a non-multisampled sampler in XR. [case 1297013](https://issuetracker.unity3d.com/issues/android-urp-black-screen-when-building-project-to-an-android-device-with-mock-hmd-enabled-and-multisampled-sampler-errors)
- Added _SURFACE_TYPE_TRANSPARENT keyword to URP shaders.
- Added Depth and DepthNormals passes to particles shaders.
- Added support for SSAO in Particle and Unlit shaders.
- Added Decal support. This includes new Decal Projector component, Decal renderer feature and Decal shader graph.
- Added a SpeedTree 8 Shader Graph but did not set it as the default when importing or upgrading Speed Tree 8 assets. Because URP doesn't yet support per-material culling, this Shader Graph does not yet behave in the same way as the existing handwritten SpeedTree 8 shader for URP.
- Added optional Depth Priming. Allows the forward opaque pass of the base camera to skip shading certain fragments if they don't contribute to the final opaque output.
- Added blending and box projection for reflection probes.
- Added 'Store Actions' option that enables bandwidth optimizations on mobile GPU architectures.
- Added "Allow Material Override" option to Lit and Unlit ShaderGraph targets.  When checked, allows Material to control the surface options (transparent/opaque, blend mode, etc).
- Added a new UI for Render Pipeline Converters. Used now for Built-in to Universal conversion.
- Added sections on Light Inspector.
- Reorder camera inspector to be in the same order as HDRP.
- Added new URP Debug Views under Window/Analysis/Rendering Debugger.
- Added support for controlling Volume Framework Update Frequency in UI on Cameras and URP Asset as well as through scripting.
- Added URP Global Settings Asset to the Graphics Settings - a common place for project-wide URP settings.
- Added possibility to rename light layer values.
- Added Light cookies support to directional, point and spot light. Directional light cookie is main light only feature.
- Added GetUniversalAdditionalLightData, a method that returns the additional data component for a given light or create one if it doesn't exist yet.
- VFX: Basic support of Lit output.
- Added Motion Vector render pass for URP.
- VFX: Fix light cookies integration.
- Added Lights 2D to the Light Explorer window.
- Two new URP specific scene templates, Basic which has a camera and directional light, then Standard which has the addition of a global volume with basic post effects setup.
- Added Render Settings Converter to the Render Pipeline Converter, this tool creates and assigns URP Assets based off rendering settings of a Builtin project.
- XR: Added Late Latching support to reduce VR latency (Quest).
- Fixed incorrect shadow fade in deferred rendering mode.
- Added a help button on material editor to show the shader documentation page
- URP global setting for stripping post processing shader variants.
- URP global setting for stripping off shader variants.

### Changed
- Moved fog evaluation from vertex shader to pixel shader. This improves rendering of fog for big triangles and fog quality. This can change the look of the fog slightly.
- UNITY_Z_0_FAR_FROM_CLIPSPACE now remaps to [0, far] range on all platforms consistently. Previously OpenGL platforms did not remap, discarding small amount of range [-near, 0].
- Moved all 2D APIs out of experimental namespace.
- ClearFlag.Depth does not implicitely clear stencil anymore. ClearFlag.Stencil added.
- The Forward Renderer asset is renamed to the Universal Renderer asset. The Universal Renderer asset contains the property Rendering Path that lets you select the Forward or the Deferred Rendering Path.
- Improved PixelPerfectCamera UI/UX
- Changed Pixel Snapping and Upscale Render Texture in the PixelPerfectCamera to a dropdown.
- Move Assets/Create/Rendering/Universal Render Pipeline/Pipeline Asset (2D Renderer) to Assets/Create/Rendering/URP Asset (with 2D Renderer)
- Move Assets/Create/Rendering/Universal Render Pipeline/2D Renderer to Assets/Create/Rendering/URP 2D Renderer
- Move Assets/Create/Rendering/Universal Render Pipeline/Renderer Feature to Assets/Create/Rendering/URP Renderer Feature
- Move Assets/Create/Rendering/Universal Render Pipeline/Post-process Data to Assets/Create/Rendering/URP Post-process Data
- Move Assets/Create/Rendering/Universal Render Pipeline/Pipeline Asset (Forward Renderer) to Assets/Create/Rendering/URP Asset (with Forward Renderer)
- Move Assets/Create/Rendering/Universal Render Pipeline/XR System Data to Assets/Create/Rendering/URP XR System Data
- Move Assets/Create/Rendering/Universal Render Pipeline/Forward Renderer to Assets/Create/Rendering/URP Forward Renderer
- Removing unused temporary depth buffers for Depth of Field and Panini Projection.
- Optimized the Bokeh Depth of Field shader on mobile by using half precision floats.
- Changed UniversalRenderPipelineCameraEditor to URPCameraEditor
- Made 2D shadow casting more efficient
- Reduced the size of the fragment input struct of the TerrainLitPasses and LitGBufferPass, SimpleLitForwardPass and SimpleLitGBufferPass lighting shaders.
- Bokeh Depth of Field performance improvement: moved some calculations from GPU to CPU.
- Advanced Options > Priority has been renamed to Sorting Priority
- Opacity as Density blending feature for Terrain Lit Shader is now disabled when the Terrain has more than four Terrain Layers. This is now similar to the Height-blend feature for the Terrain Lit Shader.
- DepthNormals passes now sample normal maps if used on the material, otherwise output the geometry normal.
- SSAO Texture is now R8 instead of ARGB32 if supported by the platform.
- Enabled subsurface scattering with GI on handwritten Universal ST8 shader.
- Material upgrader now also upgrades AnimationClips in the project that have curves bound to renamed material properties.
- 2D Lights now inherit from Light2DBase.
- The behavior of setting a camera's Background Type to "Dont Care" has changed on mobile. Previously, "Dont Care" would behave identically to "Solid Color" on mobile. Now, "Dont Care" corresponds to the render target being filled with arbitrary data at the beginning of the frame, which may be faster in some situations. Note that there are no guarantees for the exact content of the render target, so projects should use "Dont care" only if they are guaranteed to render to, or otherwise write every pixel every frame.
- Stripping shader variants per renderer features instead of combined renderer features.
- When MSAA is enabled and a depth texture is required, the opaque pass depth will be copied instead of scheduling a depth prepass.
- URP Asset Inspector - Advanced settings have been reordered under `Show Additional Properties` on each section.
- Changed the default name when a new urp asset is created.
- URP Asset Inspector - `General` section has been renamed to `Rendering`.
- Refactored some of the array resizing code around decal projector rendering to use new APIs in render core
- UniversalRendererData and ForwardRendererData GUIDs have been reversed so that users coming from 2019LTS, 2020LTS and 2021.1 have a smooth upgrade path, you may encounter issues coming from 2021.2 Alpha/Beta versions and are recommended to start with a fresh library if initial upgrade fails.
- VFX: New shadergraph support directly on Universal target.
- Changed `BaseShaderGUI.DrawAdditionalFoldouts`to `BaseShaderGUI.FillAdditionalFoldouts`.

### Fixed
- Fixed an issue in PostProcessPass causing OnGUI draws to not show on screen. [case 1346650]
- Fixed an issue with the blend mode in Sprite-Lit-Default shader causing alpha to overwrite the framebuffer. [case 1331392](https://issuetracker.unity3d.com/product/unity/issues/guid/1331392/)
- Fixed pixel perfect camera rect not being correctly initialized. [case 1312646](https://issuetracker.unity3d.com/product/unity/issues/guid/1312646/)
- Camera Inspector Stack list edition fixes.
- Fix indentation of Emission map on material editor.
- Fixed additional camera data help url
- Fixed additional light data help url
- Fixed Opacity as Density blending artifacts on Terrain that that caused Terrain to have modified splat weights of zero in some areas and greater than one in others. [case 1283124](https://issuetracker.unity3d.com/product/unity/issues/guid/1283124/)
- Fixed an issue where Sprite type Light2Ds would throw an exeception if missing a sprite
- Fixed an issue where Sprite type Light2Ds were missing a default sprite
- Fixed an issue where ShadowCasters were sometimes being rendered twice in the editor while in playmode.
- Fixed an issue where ShadowCaster2D was generating garbage when running in the editor. [case 1304158](https://issuetracker.unity3d.com/product/unity/issues/guid/1304158/)
- Fixed an issue where the 2D Renderer was not rendering depth and stencil in the normal rendering pass
- Fixed an issue where 2D lighting was incorrectly calculated when using a perspective camera.
- Fixed an issue where objects in motion might jitter when the Pixel Perfect Camera is used. [case 1300474](https://issuetracker.unity3d.com/issues/urp-characters-sprite-repeats-in-the-build-when-using-pixel-perfect-camera-and-2d-renderer)
- Fixed an issue where filtering in the scene view would not properly highlight the filtered objects. case 1324359
- Fixed an issue where the scene view camera was not correctly cleared for the 2D Renderer. [case 1311377](https://issuetracker.unity3d.com/product/unity/issues/guid/1311377/)
- Fixed an issue where the letter box/pillar box areas were not properly cleared when the Pixel Perfect Camera is used. [case 1291224](https://issuetracker.unity3d.com/issues/pixel-perfect-image-artifact-appear-between-the-reference-resolution-and-screen-resolution-borders-when-strech-fill-is-enabled)
- Fixed an issue where the Cinemachine Pixel Perfect Extension might cause the Orthographic Size of the Camera to jump to 1 when the Scene is loaded. [case 1249076](https://issuetracker.unity3d.com/issues/cinemachine-pixel-perfect-camera-extension-causes-the-orthogonal-size-to-jump-to-1-when-the-scene-is-loaded)
- Fixed an issue where 2D Shadows were casting to the wrong layers [case 1300753][https://issuetracker.unity3d.com/product/unity/issues/guid/1300753/]
- Fixed an issue where Light2D did not upgrade Shadow Strength, Volumetric Intensity, Volumetric Shadow Strength correctly [case 1317755](https://issuetracker.unity3d.com/issues/urp-lighting-missing-orange-tint-in-scene-background)
- Fixed an issue where render scale was breaking SSAO in scene view. [case 1296710](https://issuetracker.unity3d.com/issues/ssao-effect-floating-in-the-air-in-scene-view-when-2-objects-with-shadergraph-materials-are-on-top-of-each-other)
- Fixed GC allocations from XR occlusion mesh when using multipass.
- SMAA post-filter only clear stencil buffer instead of depth and stencil buffers.
- Fixed an issue where the inspector of Renderer Data would break after adding RenderObjects renderer feature and then adding another renderer feature.
- Fixed an issue where soft particles did not work with orthographic projection. [case 1294607](https://issuetracker.unity3d.com/product/unity/issues/guid/1294607/)
- Fixed wrong shader / properties assignement to materials created from 3DsMax 2021 Physical Material. (case 1293576)
- Normalized the view direction in Shader Graph to be consistent across Scriptable Render Pieplines.
- Fixed material upgrader to run in batch mode [case 1305402]
- Fixed gizmos drawing in game view. [case 1302504](https://issuetracker.unity3d.com/issues/urp-handles-with-set-ztest-do-not-respect-depth-sorting-in-the-game-view)
- Fixed an issue in shaderGraph target where the ShaderPass.hlsl was being included after SHADERPASS was defined
- Fixed base camera to keep render texture in sync with camera stacks. [case 1288105](https://issuetracker.unity3d.com/issues/srp-base-camera-rendering-to-render-texture-takes-overlay-camera-into-account-but-not-its-canvas)
- Fixed base camera to keep viewport in sync with camera stacks. [case 1311268](https://issuetracker.unity3d.com/issues/buttons-clickable-area-is-offset-when-canvas-render-camera-is-an-overlay-camera-and-viewport-rect-is-changed-on-base-camera)
- Fixed base camera to keep display index in sync with camera stacks. [case 1252265](https://issuetracker.unity3d.com/issues/universal-rp-overlay-camera-still-renders-to-displaya)
- Fixed base camera to keep display index in sync with camera stacks for canvas. [case 1291872](https://issuetracker.unity3d.com/issues/canvas-renders-only-on-the-display-1-when-its-set-to-screen-space-camera-or-world-space-and-has-overlay-type-camera-assigned)
- Fixed render pass reusage with camera stack on vulkan. [case 1226940](https://issuetracker.unity3d.com/issues/vulkan-each-camera-stack-layer-generate-a-render-pass-separately-when-render-pass-are-the-same)
- Fixed camera stack UI correctly work with prefabs. [case 1308717](https://issuetracker.unity3d.com/issues/the-prefab-apply-slash-revert-menu-cant-be-opened-by-right-clicking-on-the-stack-label-under-the-camera-component-in-the-inspector)
- Fixed an issue where Particle Lit shader had an incorrect fallback shader [case 1312459]
- Fixed an issue with backbuffer MSAA on Vulkan desktop platforms.
- Fixed shadow cascade blend culling factor.
- Fixed remove of the Additional Light Data when removing the Light Component.
- Fixed remove of the Additional Camera Data when removing the Camera Component.
- Fixed shadowCoord error when main light shadow defined in unlit shader graph [case 1175274](https://issuetracker.unity3d.com/issues/shadows-not-applying-when-using-file-in-a-custom-function-node-with-universal-rp)
- Removed Custom.meta which was causing warnings. [case 1314288](https://issuetracker.unity3d.com/issues/urp-warnings-about-missing-metadata-appear-after-installing)
- Fixed a case where shadow fade was clipped too early.
- Fixed an issue where SmoothnessSource would be upgraded to the wrong value in the material upgrader.
- Fixed multi editing of Bias property on lights. [case 1289620]
- Fixed an issue where bokeh dof is applied incorrectly when there is an overlay camera in the camera stack. [case 1303572](https://issuetracker.unity3d.com/issues/urp-bokeh-depth-of-field-is-applied-incorrectly-when-the-main-camera-has-an-overlay-camera-in-the-camera-stack)
- Fixed SafeNormalize returning invalid vector when using half with zero length. [case 1315956]
- Fixed lit shader property duplication issue. [case 1315032](https://issuetracker.unity3d.com/issues/shader-dot-propertytoid-returns-the-same-id-when-shaders-properties-have-the-same-name-but-different-type)
- Fixed undo issues for the additional light property on the UniversalRenderPipeline Asset. [case 1300367]
- Fixed an issue where SSAO would sometimes not render with a recently imported renderer.
- Fixed a regression where the precision was changed. [case 1313942](https://issuetracker.unity3d.com/issues/urp-shader-precision-is-reduced-to-half-when-scriptablerenderfeature-class-is-in-the-project)
- Fixed an issue where motion blur would allocate memory each frame. [case 1314613](https://issuetracker.unity3d.com/issues/urp-gc-alloc-increases-when-motion-blur-override-is-enabled-with-intensity-set-above-0)
- Fixed an issue where using Camera.targetTexture with Linear Color Space on an Android device that does not support sRGB backbuffer results in a RenderTexture that is too bright. [case 1307710]
- Fixed issue causing missing shaders on DirectX 11 feature level 10 GPUs. [case 1278390](https://issuetracker.unity3d.com/product/unity/issues/guid/1278390/)
- Fixed errors when the Profiler is used with XR multipass. [case 1313141](https://issuetracker.unity3d.com/issues/xr-urp-profiler-spams-errors-in-the-console-upon-entering-play-mode)
- Fixed materials being constantly dirty.
- Fixed double sided and clear coat multi editing shader.
- Fixed issue where copy depth depth pass for gizmos was being skipped in game view [case 1302504](https://issuetracker.unity3d.com/issues/urp-handles-with-set-ztest-do-not-respect-depth-sorting-in-the-game-view)
- Fixed an issue where transparent objects sampled SSAO.
- Fixed an issue where Depth Prepass was not run when SSAO was set to Depth Mode.
- Fixed an issue where changing camera's position in the BeginCameraRendering do not apply properly. [case 1318629] (https://issuetracker.unity3d.com/issues/camera-doesnt-move-when-changing-its-position-in-the-begincamerarendering-and-the-endcamerarendering-methods)
- Fixed depth of field pass usage on unsupported devices. [case 1327076](https://issuetracker.unity3d.com/issues/adreno-3xx-nothing-is-rendered-when-post-processing-is-enabled)
- Fixed an issue where SMAA did not work for OpenGL [case 1318214](https://issuetracker.unity3d.com/issues/urp-there-is-no-effect-when-using-smaa-in-urp-with-opengles-api)
- Fixed an issue with Shader Graph Lit shaders where the normalized view direction produced incorrect lighting. [1332804]
- Fixed return values from GetStereoProjectionMatrix() and SetStereoViewMatrix(). [case 1312813](https://issuetracker.unity3d.com/issues/xr-urp-begincamerarender-method-is-lagging-behind-when-using-urp)
- Fixed CopyDepthPass incorrectly always enqueued when deferred rendering mode was enabled when it should depends on the pipeline asset settings.
- Fixed renderer post processing option to work with asset selector re-assing. [case 1319454](https://issuetracker.unity3d.com/issues/urp-universal-renderer-post-processing-doesnt-enable-when-postprocessdata-reassigned-from-the-asset-selector-window)
- Fixed post processing to be enabled by default in the renderer when creating URP asset option. [case 1333461](https://issuetracker.unity3d.com/issues/post-processing-is-disabled-by-default-in-the-forward-renderer-when-creating-a-new-urp-asset)
- Fixed shaderGraph shaders to render into correct depthNormals passes when deferred rendering mode and SSAO are enabled.
- Fixed ordering of subshaders in the Unlit Shader Graph, such that shader target 4.5 takes priority over 2.0. [case 1328636](https://issuetracker.unity3d.com/product/unity/issues/guid/1328636/)
- Fixed issue where it will clear camera color if post processing is happening on XR [case 1324451]
- Fixed a case where camera dimension can be zero. [case 1321168](https://issuetracker.unity3d.com/issues/urp-attempting-to-get-camera-relative-temporary-rendertexture-is-thrown-when-tweening-the-viewport-rect-values-of-a-camera)
- Fixed renderer creation in playmode to have its property reloaded. [case 1333463]
- Fixed gizmos no longer allocate memory in game view. [case 1328852]
- Fixed an issue where shadow artefacts appeared between cascades on Terrain Detail objects.
- Fixed ShaderGraph materials to select render queue in the same way as handwritten shader materials by default, but allows for a user override for custom behavior. [case 1335795]
- Fixed sceneview debug mode rendering (case 1211436).
- URP Global Settings can now be unassigned in the Graphics tab (case 1343570).
- VFX: Fixed soft particles when HDR or Opaque texture isn't enabled
- VFX: Fixed OpenGL soft particles fallback when depth texture isn't available
- Fixed soft shadows shader variants not set to multi_compile_fragment on some shaders (gbuffer pass, speedtree shaders, WavingGrass shader).
- Fixed issue with legacy stereo matrices with XR multipass. [case 1342416]
- Fixed unlit shader function name ambiguity
- Fixed Terrain holes not appearing in shadows [case 1349305]
- VFX: Compilation issue with ShaderGraph and planar lit outputs [case 1349894](https://issuetracker.unity3d.com/product/unity/issues/guid/1349894/)
- Fixed an issue where TerrainLit was rendering color lighter than Lit [case 1340751] (https://issuetracker.unity3d.com/product/unity/issues/guid/1340751/)
- Fixed Camera rendering when capture action and post processing present. [case 1350313]
- Fixed artifacts in Speed Tree 8 billboard LODs due to SpeedTree LOD smoothing/crossfading [case 1348407]
- Fix sporadic NaN when using normal maps with XYZ-encoding [case 1351020](https://issuetracker.unity3d.com/issues/android-urp-vulkan-nan-pixels-and-bloom-post-processing-generates-visual-artifacts)
- Support undo of URP Global Settings asset assignation (case 1342987).
- Removed unsupported fields from Presets of Light and Camera [case 1335979].
- Fixed graphical artefact when terrain height map is used with rendering layer mask for lighting.
- Fixed an issue where _AfterPostProcessTexture was no longer being assigned in UniversalRenderer.
- Fixed UniversalRenderPipelineAsset now being able to use multiedit
- Fixed memory leak with XR combined occlusion meshes. [case 1366173]
- Added "Conservative Enclosing Sphere" setting to fix shadow frustum culling issue where shadows are erroneously culled in corners of cascades [case 1153151](https://issuetracker.unity3d.com/issues/lwrp-shadows-are-being-culled-incorrectly-in-the-corner-of-the-camera-viewport-when-the-far-clip-plane-is-small)
- Fixed an issue where screen space shadows has flickering with deferred mode [case 1354681](https://issuetracker.unity3d.com/issues/screen-space-shadows-flicker-in-scene-view-when-using-deferred-rendering)
- Fixed shadowCascadeBlendCullingFactor to be 1.0

### Changed
- Change Asset/Create/Shader/Universal Render Pipeline/Lit Shader Graph to Asset/Create/Shader Graph/URP/Lit Shader Graph
- Change Asset/Create/Shader/Universal Render Pipeline/Sprite Lit Shader Graph to Asset/Create/Shader Graph/URP/Sprite Lit Shader Graph
- Change Asset/Create/Shader/Universal Render Pipeline/Unlit Shader Graph to Asset/Create/Shader Graph/URP/Unlit Shader Graph
- Change Asset/Create/Shader/Universal Render Pipeline/Sprite Unlit Shader Graph to Asset/Create/Shader Graph/URP/Sprite Unlit Shader Graph
- Moved Edit/Render Pipeline/Universal Render Pipeline/Upgrade Project Materials to 2D Renderer Materials to Edit/Rendering/Materials/Convert All Built-in Materials to URP 2D Renderer
- Moved Edit/Render Pipeline/Universal Render Pipeline/Upgrade Scene Materials to 2D Renderer Materials to Edit/Rendering/Materials/Convert All Built-in Scene Materials to URP 2D Renderer
- Moved Edit/Render Pipeline/Universal Render Pipeline/Upgrade Project URP Parametric Lights to Freeform to Edit/Rendering/Lights/Convert Project URP Parametric Lights to Freeform
- Moved Edit/Render Pipeline/Universal Render Pipeline/Upgrade Scene URP Parametric Lights to Freeform to Edit/Rendering/Lights/Convert Scene URP Parametric Lights to Freeform
- Moved Edit/Render Pipeline/Universal Render Pipeline/Upgrade Project Materials to URP Materials to Edit/Rendering/Materials/Convert All Built-in Materials to URP
- Moved Edit/Render Pipeline/Universal Render Pipeline/Upgrade Selected Materials to URP Materials to Edit/Rendering/Materials/Convert Selected Built-in Materials to URP
- Deprecated GetShadowFade in Shadows.hlsl, use GetMainLightShadowFade or GetAdditionalLightShadowFade.
- Improved shadow cascade GUI drawing with pixel perfect, hover and focus functionalities.
- Shadow fade now uses border value for calculating shadow fade distance and fall off linearly.
- Improved URP profiling scopes. Remove low impact scopes from the command buffer for a small performance gain. Fix the name and invalid scope for context.submit() scope. Change the default profiling name of ScriptableRenderPass to Unnamed_ScriptableRenderPass.
- Using the same MaterialHeaderScope for material editor as HDRP is using

### Removed
- Code to upgrade from LWRP to URP was removed. This means if you want to upgrade from LWRP you must first upgrade to previous versions of URP and then upgrade to this version.

## [11.0.0] - 2020-10-21
### Added
- Added real-time Point Light Shadows.
- Added a supported MSAA samples count check, so the actual supported MSAA samples count value can be assigned to RenderTexture descriptors.
- Added the TerrainCompatible SubShader Tag. Use this Tag in your custom shader to tell Unity that the shader is compatible with the Terrain system.
- Added _CameraSortingLayerTexture global shader variable and related parameters
- Added preset shapes for creating a freeform light
- Added serialization of Freeform ShapeLight mesh to avoid CPU cost of generating them on the runtime.
- Added 2D Renderer Asset Preset for creating a Universal Renderer Asset
- Added an option to use faster, but less accurate approximation functions when converting between the sRGB and Linear color spaces.
- Added screen space shadow as renderer feature
- Added [DisallowMultipleRendererFeature] attribute for Renderer Features.
- Added support for Enlighten precomputed realtime Global Illumination.

### Changed
- Optimized 2D Renderer performance on mobile GPUs by reducing the number of render target switches.
- Optimized 2D Renderer performance by rendering the normal buffer at the same lower resolution as the light buffers.
- Improved Light2D UI/UX
- Improved 2D Menu layout
- Deprecated Light2D Parametric Light
- Deprecated Light2D point light cookie
- Renamed Light2D point light to spot light
- 2D Renderer: The per Blend Style render texture scale setting was replaced by a global scale setting for all Blend Styles.
- Optimized 2D Renderer performance by using a tiny light texture for layer/blend style pairs for which no light is rendered.
- Reorgnized the settings in 2D Renderer Data Inspector.
- FallOff Lookup Texture is now part of 2D RenderData.
- Creating a Shadow Caster 2D will use try and use sprite and physics bounds as the default shape
- Deleting all points in a Shadow Caster will cause the shape to use the bounds.
- Improved Geometry for Smooth Falloff of 2D Shape Lights.
- Updated the tooltips for Light 2D Inspector.
- Removed the Custom blend Mode option from the Blend Styles.
- New default Blend Styles when a new 2D Renderer Data asset is created.
- Added a supported MSAA samples count check, so the actual supported MSAA samples count value can be assigned to RenderTexture descriptors.
- Bloom in Gamma color-space now more closely matches Linear color-space, this will mean project using Bloom and Gamma color-space may need to adjust Bloom Intensity to match previous look.
- Autodesk Interactive Shader Graph files and folders containing them were renamed. The new file paths do not have spaces.
- Moved `FinalPostProcessPass` to `AfterRenderingPostProcessing` event from `AfterRendering`. This allows user pass to execute before and after `FinalPostProcessPass` and `CapturePass` to capture everything.
- Changed shader keywords of main light shadow from toggling to enumerating.
- Always use "High" quality normals, which normalizes the normal in pixel shader. "Low" quality normals looked too much like a bug.
- Re-enabled implicit MSAA resolve to backbuffer on Metal MacOS.
- Optimized 2D performance by rendering straight to the backbuffer if possible
- Changed Post Process Data to bool. When it is no enabled all post processing is stripped from build, when it is enabled you can still override resources there.
- Converted XR automated tests to use MockHMD.
- Improved 2D Renderer performance on mobile GPUs when using MSAA
- Reduced the size of the fragment input struct of the Terrain and Forward lighting shaders.

### Fixed
- Fixed an issue where additional lights would not render with WebGL 1
- Fixed an issue where the 2D Renderer was incorrectly rendering transparency with normal maps on an empty background.
- Fixed an issue that that caused a null error when creating a Sprite Light. [case 1307125](https://issuetracker.unity3d.com/issues/urp-nullreferenceexception-thrown-on-creating-sprite-light-2d-object-in-the-hierarchy)
- Fixed an issue where Sprites on one Sorting Layer were fully lit even when there's no 2D light targeting that layer.
- Fixed an issue where null reference exception was thrown when creating a 2D Renderer Data asset while scripts are compiling. [case 1263040](https://issuetracker.unity3d.com/issues/urp-nullreferenceexception-error-is-thrown-on-creating-2d-renderer-asset)
- Fixed an issue where no preview would show for the lit sprite master node in shadergraph
- Fixed an issue where no shader was generated for unlit sprite shaders in shadergraph
- Fixed an issue where Sprite-Lit-Default shader's Normal Map property wasn't affected by Tiling or Offset. [case 1270850](https://issuetracker.unity3d.com/issues/sprite-lit-default-shaders-normal-map-and-mask-textures-are-not-affected-by-tiling-and-offset-values)
- Fixed an issue where normal-mapped Sprites could render differently depending on whether they're dynamically-batched. [case 1286186](https://issuetracker.unity3d.com/issues/urp-2d-2d-light-on-a-rotated-sprite-is-skewed-when-using-normal-map-and-sorting-layer-is-not-default)
- Removed the warning about mis-matched vertex streams when creating a default Particle System. [case 1285272](https://issuetracker.unity3d.com/issues/particles-urp-default-material-shows-warning-in-inspector)
- Fixed latest mockHMD renderviewport scale doesn't fill whole view after scaling. [case 1286161] (https://issuetracker.unity3d.com/issues/xr-urp-renderviewportscale-doesnt-fill-whole-view-after-scaling)
- Fixed camera renders black in XR when user sets invalid MSAA value.
- Fixed an issue causing additional lights to stop working when set as the sun source. [case 1278768](https://issuetracker.unity3d.com/issues/urp-every-light-type-is-rendered-as-directional-light-if-it-is-set-as-sun-source-of-the-environment)
- Fixed an issue causing passthrough camera to not render. [case 1283894](https://issuetracker.unity3d.com/product/unity/issues/guid/1283894/)
- Fixed an issue that caused a null reference when Lift Gamma Gain was being displayed in the Inspector and URP was upgraded to a newer version.  [case 1283588](https://issuetracker.unity3d.com/issues/argumentnullexception-is-thrown-when-upgrading-urp-package-and-volume-with-lift-gamma-gain-is-focused-in-inspector)
- Fixed an issue where soft particles were not rendered when depth texture was disabled in the URP Asset. [case 1162556](https://issuetracker.unity3d.com/issues/lwrp-unlit-particles-shader-is-not-rendered-when-soft-particles-are-enabled-on-built-application)
- Fixed an issue where soft particles were rendered opaque on OpenGL. [case 1226288](https://issuetracker.unity3d.com/issues/urp-objects-that-are-using-soft-particles-are-rendered-opaque-when-opengl-is-used)
- Fixed an issue where the depth texture sample node used an incorrect texture in some frames. [case 1268079](https://issuetracker.unity3d.com/issues/urp-depth-texture-sample-node-does-not-use-correct-texture-in-some-frames)
- Fixed a compiler error in BakedLit shader when using Hybrid Renderer.
- Fixed an issue with upgrading material set to cutout didn't properly set alpha clipping. [case 1235516](https://issuetracker.unity3d.com/issues/urp-upgrade-material-utility-does-not-set-the-alpha-clipping-when-material-was-using-a-shader-with-rendering-mode-set-to-cutout)
- Fixed XR camera fov can be changed through camera inspector.
- Fixed an issue where Universal Render Pipeline with disabled antiAliasing was overwriting QualitySettings.asset on frequent cases. [case 1219159](https://issuetracker.unity3d.com/issues/urp-qualitysettings-dot-asset-file-gets-overwritten-with-the-same-content-when-the-editor-is-closed)
- Fixed a case where overlay camera with output texture caused base camera not to render to screen. [case 1283225](https://issuetracker.unity3d.com/issues/game-view-renders-a-black-view-when-having-an-overlay-camera-which-had-output-texture-assigned-in-the-camera-stack)
- Fixed an issue where the scene view camera ignored the pipeline assets HDR setting. [case 1284369](https://issuetracker.unity3d.com/issues/urp-scene-view-camera-ignores-pipeline-assets-hdr-settings-when-main-camera-uses-pipeline-settings)
- Fixed an issue where the Camera inspector was grabbing the URP asset in Graphics Settings rather than the currently active.
- Fixed an issue where the Light Explorer was grabbing the URP asset in Graphics Settings rather than the currently active.
- Fixed an issue causing materials to be upgraded multiple times.
- Fixed bloom inconsistencies between Gamma and Linear color-spaces.
- Fixed an issue in where all the entries in the Renderer List wasn't selectable and couldn't be deleted.
- Fixed Deferred renderer on some Android devices by forcing accurate GBuffer normals. [case 1288042]
- Fixed an issue where MSAA did not work in Editor Game View on Windows with Vulkan.
- Fixed issue where selecting and deselecting Forward Renderer asset would leak memory [case 1290628](https://issuetracker.unity3d.com/issues/urp-scriptablerendererfeatureeditor-memory-leak-while-interacting-with-forward-renderer-in-the-project-window)
- Fixed the default background color for previews to use the original color.
- Fixed an issue where the scene view would turn black when bloom was enabled. [case 1298790](https://issuetracker.unity3d.com/issues/urp-bloom-and-tonemapping-causes-the-screen-to-go-black-in-scene-mode)
- Fixed an issue where having "Opaque Texture" and MSAA enabled would cause the opaque texture to be rendered black on old Apple GPUs [case 1247423](https://issuetracker.unity3d.com/issues/urp-metal-opaque-objects-are-rendered-black-when-msaa-is-enabled)
- Fixed SAMPLE_TEXTURECUBE_ARRAY_LOD macro when using OpenGL ES. [case 1285132](https://issuetracker.unity3d.com/issues/urp-android-error-sample-texturecube-array-lod-is-not-supported-on-gles-3-dot-0-when-using-cubemap-array-shader-shaders)
- Fixed an issue such that it is now posible to enqueue render passes at runtime.
- Fixed SpeedTree LOD fade functionality. [case 1198135]

## [10.2.0] - 2020-10-19

### Changed
- Changed RenderObjectsFeature UI to only expose valid events. Previously, when selecting events before BeforeRenderingPrepasses objects would not be drawn correctly as stereo and camera setup only happens before rendering opaques objects.
- Transparent Lit ShaderGraph using Additive blending will now properly fade with alpha [1270344]

### Fixed
- Fixed the Unlit shader not being SRP Batcher compatible on OpenGLES/OpenGLCore. [case 1263720](https://issuetracker.unity3d.com/issues/urp-mobile-srp-batcher-is-not-visible-on-mobile-devices-in-frame-debugger)
- Fixed an issue with soft particles not rendering correctly for overlay cameras with post processing. [case 1241626](https://issuetracker.unity3d.com/issues/soft-particles-does-not-fade-out-near-the-opaque-surfaces-when-post-processing-is-enabled-on-a-stacked-camera)
- Fixed MSAA override on camera does not work in non-XR project if target eye is selected to both eye.

## [10.1.0] - 2020-10-12
- Added support for the Shadowmask Mixed Lighting Mode (Forward only), which supports up to four baked-shadow Lights.
- Added ComplexLit shader for advanced material features and deferred forward fallback.
- Added Clear Coat feature for ComplexLit shader and for shader graph.
- Added Parallax Mapping to the Lit shader (Lit.shader).
- Added the Detail Inputs setting group in the Lit shader (Lit.shader).
- Added Smooth shadow fading.
- Added SSAO support for deferred renderer.
- The pipeline now outputs a warning in the console when trying to access camera color or depth texture when those are not valid. Those textures are only available in the context of `ScriptableRenderPass`.
- Added a property to access the renderer from the `CameraData`.

### Changed
- Shader functions SampleSH9, SampleSHPixel, SampleSHVertex are now gamma corrected in gamma space. As result LightProbes are gamma corrected too.
- The maximum number of visible lights when using OpenGL ES 3.x on Android now depends on the minimum OpenGL ES 3.x version as configured in PlayerSettings.
- The default value of the HDR property of a newly created Universal Render Pipeline Asset, is now set to true.

### Fixed
- Fixed an issue where the CapturePass would not capture the post processing effects.
- Fixed an issue were the filter window could not be defocused using the mouse. [case 1242032](https://issuetracker.unity3d.com/issues/urp-volume-override-window-doesnt-disappear-when-clicked-on-the-other-windows-in-the-editor)
- Fixed camera backgrounds not matching between editor and build when background is set to 'Uninitialized'. [case 1224369](https://issuetracker.unity3d.com/issues/urp-uninitialized-camera-background-type-does-not-match-between-the-build-and-game-view)
- Fixed a case where main light hard shadows would not work if any other light is present with soft shadows.[case 1250829](https://issuetracker.unity3d.com/issues/main-light-shadows-are-ignored-in-favor-of-additional-lights-shadows)
- Fixed issue that caused color grading to not work correctly with camera stacking. [case 1263193](https://issuetracker.unity3d.com/product/unity/issues/guid/1263193/)
- Fixed an issue that caused an infinite asset database reimport when running Unity in command line with -testResults argument.
- Fixed ParticlesUnlit shader to use fog color instead of always black. [case 1264585]
- Fixed issue that caused some properties in the camera to not be bolded and highlighted when edited in prefab mode. [case 1230082](https://issuetracker.unity3d.com/issues/urp-camera-prefab-fields-render-type-renderer-background-type-are-not-bolded-and-highlighted-when-edited-in-prefab-mode)
- Fixed issue where blur would sometimes flicker [case 1224915](https://issuetracker.unity3d.com/issues/urp-bloom-effect-flickers-when-using-integrated-post-processing-feature-set)
- Fixed an issue in where the camera inspector didn't refresh properly when changing pipeline in graphic settings. [case 1222668](https://issuetracker.unity3d.com/issues/urp-camera-properties-not-refreshing-on-adding-or-removing-urp-pipeline-in-the-graphics-setting)
- Fixed depth of field to work with dynamic resolution. [case 1225467](https://issuetracker.unity3d.com/issues/dynamic-resolution-rendering-error-when-using-depth-of-field-in-urp)
- Fixed FXAA, SSAO, Motion Blur to work with dynamic resolution.
- Fixed an issue where Pixel lighting variants were stripped in builds if another URP asset had Additional Lights set to Per Vertex [case 1263514](https://issuetracker.unity3d.com/issues/urp-all-pixel-lighting-variants-are-stripped-in-build-if-at-least-one-urp-asset-has-additional-lights-set-to-per-vertex)
- Fixed an issue where transparent meshes were rendered opaque when using custom render passes [case 1262887](https://issuetracker.unity3d.com/issues/urp-transparent-meshes-are-rendered-as-opaques-when-using-lit-shader-with-custom-render-pass)
- Fixed regression from 8.x.x that increased launch times on Android with GLES3. [case 1269119](https://issuetracker.unity3d.com/issues/android-launch-times-increased-x4-from-urp-8-dot-1-0-to-urp-10-dot-0-0-preview-dot-26)
- Fixed an issue with a render texture failing assertion when chosing an invalid format. [case 1222676](https://issuetracker.unity3d.com/issues/the-error-occurs-when-a-render-texture-which-has-a-certain-color-format-is-applied-to-the-cameras-output-target)
- Fixed an issue that caused the unity_CameraToWorld matrix to have z flipped values. [case 1257518](https://issuetracker.unity3d.com/issues/parameter-unity-cameratoworld-dot-13-23-33-is-inverted-when-using-universal-rp-7-dot-4-1-and-newer)
- Fixed not using the local skybox on the camera game object when the Skybox Material property in the Lighting window was set to null.
- Fixed an issue where, if URP was not in use, you would sometimes get errors about 2D Lights when going through the menus.
- Fixed GC when using XR single-pass automated tests.
- Fixed an issue that caused a null reference when deleting camera component in a prefab. [case 1244430](https://issuetracker.unity3d.com/issues/urp-argumentnullexception-error-is-thrown-on-removing-camera-component-from-camera-prefab)
- Fixed resolution of intermediate textures when rendering to part of a render texture. [case 1261287](https://issuetracker.unity3d.com/product/unity/issues/guid/1261287/)
- Fixed indirect albedo not working with shadergraph shaders in some rare setups. [case 1274967](https://issuetracker.unity3d.com/issues/gameobjects-with-custom-mesh-are-not-reflecting-the-light-when-using-the-shader-graph-shaders)
- Fixed XR mirroView sRGB issue when color space is gamma.
- Fixed an issue where XR eye textures are recreated multiple times per frame due to per camera MSAA change.
- Fixed an issue wehre XR mirror view selector stuck.
- Fixed LightProbes to have gamma correct when using gamma color space. [case 1268911](https://issuetracker.unity3d.com/issues/urp-has-no-gamma-correction-for-lightprobes)
- Fixed GLES2 shader compilation.
- Fixed useless mip maps on temporary RTs/PostProcessing inherited from Main RT descriptor.
- Fixed issue with lens distortion breaking rendering when enabled and its intensity is 0.
- Fixed mixed lighting subtractive and shadowmask modes for deferred renderer.
- Fixed issue that caused motion blur to not work in XR.
- Fixed 2D renderer when using Linear rendering on Android directly to backbuffer.
- Fixed issue where multiple cameras would cause GC each frame. [case 1259717](https://issuetracker.unity3d.com/issues/urp-scriptablerendercontext-dot-getcamera-array-dot-resize-creates-garbage-every-frame-when-more-than-one-camera-is-active)
- Fixed Missing camera cannot be removed after scene is saved by removing the Missing camera label. [case 1252255](https://issuetracker.unity3d.com/issues/universal-rp-missing-camera-cannot-be-removed-from-camera-stack-after-scene-is-saved)
- Fixed MissingReferenceException when removing Missing camera from camera stack by removing Missing camera label. [case 1252263](https://issuetracker.unity3d.com/issues/universal-rp-missingreferenceexception-errors-when-removing-missing-camera-from-stack)
- Fixed slow down in the editor when editing properties in the UI for renderer features. [case 1279804](https://issuetracker.unity3d.com/issues/a-short-freeze-occurs-in-the-editor-when-expanding-or-collapsing-with-the-arrow-the-renderer-feature-in-the-forward-renderer)
- Fixed test 130_UnityMatrixIVP on OpenGL ES 3
- Fixed MSAA on Metal MacOS and Editor.

## [10.0.0] - 2020-06-10
### Added
- Added the option to strip Terrain hole Shader variants.
- Added support for additional Directional Lights. The amount of additional Directional Lights is limited by the maximum Per-object Lights in the Render Pipeline Asset.
- Added Package Samples: 2 Camera Stacking, 2 Renderer Features
- Added default implementations of OnPreprocessMaterialDescription for FBX, Obj, Sketchup and 3DS file formats.
- Added Transparency Sort Mode and Transparency Sort Axis to 2DRendererData.
- Added support for a user defined default material to 2DRendererData.
- Added the option to toggle shadow receiving on transparent objects.
- Added XR multipass rendering. Multipass rendering is a requirement on many VR platforms and allows graceful fallback when single-pass rendering isn't available.
- Added support for Camera Stacking when using the Forward Renderer. This introduces the Camera `Render Type` property. A Base Camera can be initialized with either the Skybox or Solid Color, and can combine its output with that of one or more Overlay Cameras. An Overlay Camera is always initialized with the contents of the previous Camera that rendered in the Camera Stack.
- Added AssetPostprocessors and Shadergraphs to handle Arnold Standard Surface and 3DsMax Physical material import from FBX.
- Added `[MainTexture]` and `[MainColor]` shader property attributes to URP shader properties. These will link script material.mainTextureOffset and material.color to `_BaseMap` and `_BaseColor` shader properties.
- Added the option to specify the maximum number of visible lights. If you set a value, lights are sorted based on their distance from the Camera.
- Added the option to control the transparent layer separately in the Forward Renderer.
- Added the ability to set individual RendererFeatures to be active or not, use `ScriptableRendererFeature.SetActive(bool)` to set whether a Renderer Feature will execute,  `ScriptableRendererFeature.isActive` can be used to check the current active state of the Renderer Feature.
 additional steps to the 2D Renderer setup page for quality and platform settings.
- If Unity Editor Analytics are enabled, Universal collects anonymous data about usage of Universal. This helps the Universal team focus our efforts on the most common scenarios, and better understand the needs of our customers.
- Added a OnCameraSetup() function to the ScriptableRenderPass API, that gets called by the renderer before rendering each camera
- Added a OnCameraCleanup() function to the ScriptableRenderPass API, that gets called by the renderer after rendering each camera
- Added Default Material Type options to the 2D Renderer Data Asset property settings.
- Added additional steps to the 2D Renderer setup page for quality and platform settings.
- Added option to disable XR autotests on test settings.
- Shader Preprocessor strips gbuffer shader variants if DeferredRenderer is not in the list of renderers in any Scriptable Pipeline Assets.
- Added an option to enable/disable Adaptive Performance when the Adaptive Performance package is available in the project.
- Added support for 3DsMax's 2021 Simplified Physical Material from FBX files in the Model Importer.
- Added GI to SpeedTree
- Added support for DXT5nm-style normal maps on Android, iOS and tvOS
- Added stencil override support for deferred renderer.
- Added a warning message when a renderer is used with an unsupported graphics API, as the deferred renderer does not officially support GL-based platforms.
- Added option to skip a number of final bloom iterations.
- Added support for [Screen Space Ambient Occlusion](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@10.0/manual/post-processing-ssao.html) and a new shader variant _SCREEN_SPACE_OCCLUSION.
- Added support for Normal Texture being generated in a prepass.
- Added a ConfigureInput() function to ScriptableRenderPass, so it is possible for passes to ask that a Depth, Normal and/or Opaque textures to be generated by the forward renderer.
- Added a float2 normalizedScreenSpaceUV to the InputData Struct.
- Added new sections to documentation: [Writing custom shaders](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@10.0/manual/writing-custom-shaders-urp.html), and [Using the beginCameraRendering event](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@10.0/manual/using-begincamerarendering.html).
- Added support for GPU instanced mesh particles on supported platforms.
- Added API to check if a Camera or Light is compatible with Universal Render Pipeline.

### Changed
- Moved the icon that indicates the type of a Light 2D from the Inspector header to the Light Type field.
- Eliminated some GC allocations from the 2D Renderer.
- Added SceneSelection pass for TerrainLit shader.
- Remove final blit pass to force alpha to 1.0 on mobile platforms.
- Deprecated the CinemachineUniversalPixelPerfect extension. Use the one from Cinemachine v2.4 instead.
- Replaced PlayerSettings.virtualRealitySupported with XRGraphics.tryEnable.
- Blend Style in the 2DRendererData are now automatically enabled/disabled.
- When using the 2D Renderer, Sprites will render with a faster rendering path when no lights are present.
- Particle shaders now receive shadows
- The Scene view now mirrors the Volume Layer Mask set on the Main Camera.
- Drawing order of SRPDefaultUnlit is now the same as the Built-in Render Pipline.
- Made MaterialDescriptionPreprocessors private.
- UniversalRenderPipelineAsset no longer supports presets. [Case 1197020](https://issuetracker.unity3d.com/issues/urp-reset-functionality-does-not-work-on-preset-of-universalrenderpipelineassets).
- The number of maximum visible lights is now determined by whether the platform is mobile or not.
- Renderer Feature list is now redesigned to fit more closely to the Volume Profile UI, this vastly improves UX and reliability of the Renderer Features List.
- Default color values for Lit and SimpleLit shaders changed to white due to issues with texture based workflows.
- You can now subclass ForwardRenderer to create a custom renderer based on it.
- URP is now computing tangent space per fragment.
- Optimized the 2D Renderer to skip rendering into certain internal buffers when not necessary.
- You can now subclass ForwardRenderer to create a custom renderer based on it.
- URP shaders that contain a priority slider now no longer have an offset of 50 by default.
- The virtual ScriptableRenderer.FrameCleanup() function has been marked obsolete and replaced by ScriptableRenderer.OnCameraCleanup() to better describe when the function gets invoked by the renderer.
- DepthOnlyPass, CopyDepthPass and CopyColorPass now use OnCameraSetup() instead of Configure() to set up their passes before executing as they only need to get their rendertextures once per camera instead of once per eye.
- Updated shaders to be compatible with Microsoft's DXC.
- Mesh GPU Instancing option is now hidden from the particles system renderer as this feature is not supported by URP.
- The 2D Renderer now supports camera stacking.
- 2D shaders now use half-precision floats whenever precise results are not necessary.
- Removed the ETC1_EXTERNAL_ALPHA variant from Shader Graph Sprite shaders.
- Eliminated some unnecessary clearing of render targets when using the 2D Renderer.
- The rendering of 2D lights is more effient as sorting layers affected by the same set of lights are now batched.
- Removed the 8 renderer limit from URP Asset.
- Merged the deferred renderer into the forward renderer.
- Changing the default value of Skip Iterations to 1 in Bloom effect editor
- Use SystemInfo to check if multiview is supported instead of being platform hardcoded
- Default attachment setup behaviour for ScriptableRenderPasses that execute before rendering opaques is now set use current the active render target setup. This improves performance in some situations.
- Combine XR occlusion meshes into one when using single-pass (multiview or instancing) to reduce draw calls and state changes.
- Shaders included in the URP package now use local Material keywords instead of global keywords. This increases the amount of available global user-defined Material keywords.

### Fixed
- Fixed an issue that caused WebGL to render blank screen when Depth texture was enabled [case 1240228](https://issuetracker.unity3d.com/issues/webgl-urp-scene-is-rendered-black-in-webgl-build-when-depth-texture-is-enabled)
- Fixed NaNs in tonemap algorithms (neutral and ACES) on platforms defaulting to lower precision.
- Fixed a performance problem with ShaderPreprocessor with large amount of active shader variants in the project
- Fixed an issue where linear to sRGB conversion occurred twice on certain Android devices.
- Fixed an issue where there were 2 widgets showing the outer angle of a spot light.
- Fixed an issue where Unity rendered fullscreen quads with the pink error shader when you enabled the Stop NaN post-processing pass.
- Fixed an issue where Terrain hole Shader changes were missing. [Case 1179808](https://issuetracker.unity3d.com/issues/terrain-brush-tool-is-not-drawing-when-paint-holes-is-selected).
- Fixed an issue where the Shader Graph `SceneDepth` node didn't work with XR single-pass (double-wide) rendering. See [case 1123069](https://issuetracker.unity3d.com/issues/lwrp-vr-shadergraph-scenedepth-doesnt-work-in-single-pass-rendering).
- Fixed Unlit and BakedLit shader compilations in the meta pass.
- Fixed an issue where the Bokeh Depth of Field shader would fail to compile on PS4.
- Fixed an issue where the Scene lighting button didn't work when you used the 2D Renderer.
- Fixed a performance regression when you used the 2D Renderer.
- Fixed an issue where the Freeform 2D Light gizmo didn't correctly show the Falloff offset.
- Fixed an issue where the 2D Renderer rendered nothing when you used shadow-casting lights with incompatible Renderer2DData.
- Fixed an issue where errors were generated when the Physics2D module was not included in the project's manifest.
- Fixed an issue where Prefab previews were incorrectly lit when you used the 2D Renderer.
- Fixed an issue where the Light didn't update correctly when you deleted a Sprite that a Sprite 2D Light uses.
- Fixed an issue where 2D Lighting was broken for Perspective Cameras.
- Fixed an issue where resetting a Freeform 2D Light would throw null reference exceptions. [Case 1184536](https://issuetracker.unity3d.com/issues/lwrp-changing-light-type-to-freeform-after-clicking-on-reset-throws-multiple-arguementoutofrangeexception).
- Fixed an issue where Freeform 2D Lights were not culled correctly when there was a Falloff Offset.
- Fixed an issue where Tilemap palettes were invisible in the Tile Palette window when the 2D Renderer was in use. [Case 1162550](https://issuetracker.unity3d.com/issues/adding-tiles-in-the-tile-palette-makes-the-tiles-invisible).
- Fixed issue where black emission would cause unneccesary inspector UI repaints. [Case 1105661](https://issuetracker.unity3d.com/issues/lwrp-inspector-window-is-being-repainted-when-using-the-material-with-emission-enabled-and-set-to-black-00-0).
- Fixed user LUT sampling being done in Linear instead of sRGB.
- Fixed an issue when trying to get the Renderer via API on the first frame. [Case 1189196](https://issuetracker.unity3d.com/product/unity/issues/guid/1189196/).
- Fixed a material leak on domain reload.
- Fixed an issue where deleting an entry from the Renderer List and then undoing that change could cause a null reference. [Case 1191896](https://issuetracker.unity3d.com/issues/nullreferenceexception-when-attempting-to-remove-entry-from-renderer-features-list-after-it-has-been-removed-and-then-undone).
- Fixed an issue where the user would get an error if they removed the Additional Camera Data component. [Case 1189926](https://issuetracker.unity3d.com/issues/unable-to-remove-universal-slash-hd-additional-camera-data-component-serializedobject-target-destroyed-error-is-thrown).
- Fixed post-processing with XR single-pass rendering modes.
- Fixed an issue where Cinemachine v2.4 couldn't be used together with Universal RP due to a circular dependency between the two packages.
- Fixed an issue that caused shaders containing `HDRP` string in their path to be stripped from the build.
- Fixed an issue that caused only selected object to render in SceneView when Wireframe drawmode was selected.
- Fixed Renderer Features UI tooltips. [Case 1191901](https://issuetracker.unity3d.com/issues/forward-renderers-render-objects-layer-mask-tooltip-is-incorrect-and-contains-a-typo).
- Fixed multiple issues where Shader Graph shaders failed to build for XR in the Universal RP.
- Fixed an issue when using the 2D Renderer where some types of renderers would not be assigned the correct material.
- Fixed inconsistent lighting between the forward renderer and the deferred renderer, that was caused by a missing normalize operation on vertex normals on some speedtree shader variants.
- Fixed issue where XR Multiview failed to render when using URP Shader Graph Shaders
- Fixed lazy initialization with last version of ResourceReloader
- Fixed broken images in package documentation.
- Fixed an issue where viewport aspect ratio was wrong when using the Stretch Fill option of the Pixel Perfect Camera. [case 1188695](https://issuetracker.unity3d.com/issues/pixel-perfect-camera-component-does-not-maintain-the-aspect-ratio-when-the-stretch-fill-is-enabled)
- Fixed an issue where setting a Normal map on a newly created material would not update. [case 1197217](https://issuetracker.unity3d.com/product/unity/issues/guid/1197217/)
- Fixed an issue where post-processing was not applied for custom renderers set to run on the "After Rendering" event [case 1196219](https://issuetracker.unity3d.com/issues/urp-post-processing-is-not-applied-to-the-scene-when-render-ui-event-is-set-to-after-rendering)
- Fixed an issue that caused an extra blit when using custom renderers [case 1156741](https://issuetracker.unity3d.com/issues/lwrp-performance-decrease-when-using-a-scriptablerendererfeature)
- Fixed an issue with transparent objects not receiving shadows when using shadow cascades. [case 1116936](https://issuetracker.unity3d.com/issues/lwrp-cascaded-shadows-do-not-appear-on-alpha-blended-objects)
- Fixed issue where using a ForwardRendererData preset would cause a crash. [case 1201052](https://issuetracker.unity3d.com/product/unity/issues/guid/1201052/)
- Fixed an issue where particles had dark outlines when blended together [case 1199812](https://issuetracker.unity3d.com/issues/urp-soft-particles-create-dark-blending-artefacts-when-intersecting-with-scene-geometry)
- Fixed an issue with deleting shader passes in the custom renderer features list [case 1201664](https://issuetracker.unity3d.com/issues/urp-remove-button-is-not-activated-in-shader-passes-list-after-creating-objects-from-renderer-features-in-urpassets-renderer)
- Fixed camera inverse view-projection matrix in XR mode, depth-copy and color-copy passes.
- Fixed an issue with the null check when `UniversalRenderPipelineLightEditor.cs` tries to access `SceneView.lastActiveSceneView`.
- Fixed an issue where the 'Depth Texture' drop down was incorrectly disabled in the Camera Inspector.
- Fixed an issue that caused errors if you disabled the VR Module when building a project.
- Fixed an issue where the default TerrainLit Material was outdated, which caused the default Terrain to use per-vertex normals instead of per-pixel normals.
- Fixed shader errors and warnings in the default Universal RP Terrain Shader. [case 1185948](https://issuetracker.unity3d.com/issues/urp-terrain-slash-lit-base-pass-shader-does-not-compile)
- Fixed an issue where the URP Material Upgrader tried to upgrade standard Universal Shaders. [case 1144710](https://issuetracker.unity3d.com/issues/upgrading-to-lwrp-materials-is-trying-to-upgrade-lwrp-materials)
- Fixed an issue where some Materials threw errors when you upgraded them to Universal Shaders. [case 1200938](https://issuetracker.unity3d.com/issues/universal-some-materials-throw-errors-when-updated-to-universal-rp-through-update-materials-to-universal-rp)
- Fixed issue where normal maps on terrain appeared to have flipped X-components when compared to the same normal map on a mesh. [case 1181518](https://fogbugz.unity3d.com/f/cases/1181518/)
- Fixed an issue where the editor would sometimes crash when using additional lights [case 1176131](https://issuetracker.unity3d.com/issues/mac-crash-on-processshadowcasternodevisibilityandcullwithoutumbra-when-same-rp-asset-is-set-in-graphics-and-quality-settings)
- Fixed RemoveComponent on Camera contextual menu to not remove Camera while a component depend on it.
- Fixed an issue where right eye is not rendered to. [case 1170619](https://issuetracker.unity3d.com/issues/vr-lwrp-terrain-is-not-rendered-in-the-right-eye-of-an-hmd-when-using-single-pass-instanced-stereo-rendering-mode-with-lwrp)
- Fixed issue where TerrainDetailLit.shader fails to compile when XR is enabled.
- Fixed an issue that allowed height-based blending on Terrains with more than 4 materials, which is not supported.
- Fixed an issue where opaque objects were outputting incorrect alpha values [case 1168283](https://issuetracker.unity3d.com/issues/lwrp-alpha-clipping-material-makes-other-materials-look-like-alpha-clipping-when-gameobject-is-shown-in-render-texture)
- Fixed an issue where a depth texture was always created when post-processing was enabled, even if no effects made use of it.
- Fixed incorrect light attenuation on some platforms.
- Fixed an issue where the Volume System would not use the Cameras Transform when no `Volume Trigger` was set.
- Fixed an issue where post processing disappeared when using custom renderers and SMAA or no AA
- Fixed an issue where the 2D Renderer upgrader did not upgrade using the correct default material
- Fixed an issue with soft particles having dark blending when intersecting with scene geometry [case 1199812](https://issuetracker.unity3d.com/issues/urp-soft-particles-create-dark-blending-artefacts-when-intersecting-with-scene-geometry)
- Fixed an issue with additive particles blending incorrectly [case 1215713](https://issuetracker.unity3d.com/issues/universal-render-pipeline-additive-particles-not-using-vertex-alpha)
- Fixed an issue where camera preview window was missing in scene view. [case 1211971](https://issuetracker.unity3d.com/issues/scene-view-urp-camera-preview-window-is-missing-in-the-scene-view)
- Fixed an issue with shadow cascade values were not readable in the render pipeline asset [case 1219003](https://issuetracker.unity3d.com/issues/urp-cascade-values-truncated-on-selecting-two-or-four-cascades-in-shadows-under-universalrenderpipelineasset)
- Fixed an issue where MSAA isn't applied until eye textures are relocated by changing their resolution. [case 1197958](https://issuetracker.unity3d.com/issues/oculus-quest-oculus-go-urp-msaa-isnt-applied-until-eye-textures-are-relocated-by-changing-their-resolution)
- Fixed an issue where camera stacking didn't work properly inside prefab mode. [case 1220509](https://issuetracker.unity3d.com/issues/urp-cannot-assign-overlay-cameras-to-a-camera-stack-while-in-prefab-mode)
- Fixed the definition of `mad()` in SMAA shader for OpenGL.
- Fixed an issue where partical shaders failed to handle Single-Pass Stereo VR rendering with Double-Wide Textures. [case 1201208](https://issuetracker.unity3d.com/issues/urp-vr-each-eye-uses-the-cameraopaquetexture-of-both-eyes-for-rendering-when-using-single-pass-rendering-mode)
- Fixed an issue that caused assets to be reimported if player prefs were cleared. [case 1192259](https://issuetracker.unity3d.com/issues/lwrp-clearing-playerprefs-through-a-script-or-editor-causes-delay-and-console-errors-to-appear-when-entering-the-play-mode)
- Fixed missing Custom Render Features after Library deletion. [case 1196338](https://issuetracker.unity3d.com/product/unity/issues/guid/1196338/)
- Fixed not being able to remove a Renderer Feature due to tricky UI selection rects. [case 1208113](https://issuetracker.unity3d.com/product/unity/issues/guid/1208113/)
- Fixed an issue where the Camera Override on the Render Object Feature would not work with many Render Features in a row. [case 1205185](https://issuetracker.unity3d.com/product/unity/issues/guid/1205185/)
- Fixed UI clipping issue in Forward Renderer inspector. [case 1211954](https://issuetracker.unity3d.com/product/unity/issues/guid/1211954/)
- Fixed a Null ref when trying to remove a missing Renderer Feature from the Forward Renderer. [case 1196651](https://issuetracker.unity3d.com/product/unity/issues/guid/1196651/)
- Fixed data serialization issue when adding a Renderer Feature to teh Forward Renderer. [case 1214779](https://issuetracker.unity3d.com/product/unity/issues/guid/1214779/)
- Fixed issue with AssetPostprocessors dependencies causing models to be imported twice when upgrading the package version.
- Fixed an issue where NullReferenceException might be thrown when creating 2D Lights. [case 1219374](https://issuetracker.unity3d.com/issues/urp-nullreferenceexception-threw-on-adding-the-light-2d-experimental-component-when-2d-render-data-not-assigned)
- Fixed an issue with a blurry settings icon. [case 1201895](https://issuetracker.unity3d.com/issues/urp-setting-icon-blurred-in-universalrendererpipelineasset)
- Fixed issue that caused the QualitySettings anti-aliasing changing without user interaction. [case 1195272](https://issuetracker.unity3d.com/issues/lwrp-the-anti-alias-quality-settings-value-is-changing-without-user-interaction)
- Fixed an issue where Shader Graph shaders generate undeclared identifier 'GetWorldSpaceNormalizeViewDir' error.
- Fixed an issue where rendering into RenderTexture with Single Pass Instanced renders both eyes overlapping.
- Fixed an issue where Renderscale setting has no effect when using XRSDK.
- Fixed an issue where renderScale != 1 or Display.main.requiresBlitToBackbuffer forced an unnecessary blit on XR.
- Fixed an issue that causes double sRGB correction on Quest. [case 1209292](https://issuetracker.unity3d.com/product/unity/issues/guid/1209292)
- Fixed an issue where terrain DepthOnly pass does not work for XR.
- Fixed an issue that caused depth texture to be flipped when sampling from shaders [case 1225362](https://issuetracker.unity3d.com/issues/game-object-is-rendered-incorrectly-in-the-game-view-when-sampling-depth-texture)
- Fixed an issue with URP switching such that every avaiable URP makes a total set of supported features such that all URPs are taken into consideration. [case 1157420](https://issuetracker.unity3d.com/issues/lwrp-srp-switching-doesnt-work-even-with-manually-adding-shadervariants-per-scene)
- Fixed an issue where XR multipass repeatedly throws error messages "Multi pass stereo mode doesn't support Camera Stacking".
- Fixed an issue with shadows not appearing on terrains when no cascades were selected [case 1226530](https://issuetracker.unity3d.com/issues/urp-no-shadows-on-terrain-when-cascades-is-set-to-no-cascades-in-render-pipeline-asset-settings)
- Fixed a shader issue that caused the Color in Sprite Shape to work improperly.
- Fixed an issue with URP switching such that every available URP makes a total set of supported features such that all URPs are taken into consideration. [case 1157420](https://issuetracker.unity3d.com/issues/lwrp-srp-switching-doesnt-work-even-with-manually-adding-shadervariants-per-scene)
- Metallic slider on the Lit shader is now linear meaning correct values are used for PBR.
- Fixed an issue where Post-Processing caused nothing to render on GLES2.
- Fixed an issue that causes viewport to not work correctly when rendering to textures. [case 1225103](https://issuetracker.unity3d.com/issues/urp-the-viewport-rect-isnt-correctly-applied-when-the-camera-is-outputting-into-a-rendertexture)
- Fixed an issue that caused incorrect sampling of HDR reflection probe textures.
- Fixed UI text of RenderObjects feature to display LightMode tag instead of Shader Pass Name. [case 1201696](https://issuetracker.unity3d.com/issues/render-feature-slash-pass-ui-has-a-field-for-shader-pass-name-when-it-actually-expects-shader-pass-lightmode)
- Fixed an issue when Linear -> sRGB conversion would not happen on some Android devices. [case 1226208](https://issuetracker.unity3d.com/issues/no-srgb-conversion-on-some-android-devices-when-using-the-universal-render-pipeline)
- Fixed issue where using DOF at the same time as Dynamic Scaling, the depth buffer was smapled with incorrect UVs. [case 1225467](https://issuetracker.unity3d.com/product/unity/issues/guid/1225467/)
- Fixed an issue where an exception would be thrown when resetting the ShadowCaster2D component. [case 1225339](https://issuetracker.unity3d.com/issues/urp-unassignedreferenceexception-thrown-on-resetting-the-shadow-caster-2d-component)
- Fixe an issue where using a Subtractive Blend Style for your 2D Lights might cause artifacts in certain post-processing effects. [case 1215584](https://issuetracker.unity3d.com/issues/urp-incorrect-colors-in-scene-when-using-subtractive-and-multiply-blend-mode-in-gamma-color-space)
- Fixed an issue where Cinemachine Pixel Perfect Extension didn't work when CinemachineBrain Update Method is anything other than Late Update.
- Fixed an issue where Sprite Shader Graph shaders weren't double-sided by default.
- Fixed an issue where particles using Sprite Shader Graph shaders were invisible.
- Fixed an issue where Scene objects might be incorrectly affected by 2D Lights from a previous Sorting Layer.
- Fixed an issue where errors would appear in the Console when entering Play Mode with a 2D Light selected in the Hierarchy. [Case 1226918](https://issuetracker.unity3d.com/issues/errors-appear-in-the-console-when-global-2d-light-is-selected-in-hierarchy)
- Fixed an issue that caused Android GLES to render blank screen when Depth texture was enabled without Opaque texture [case 1219325](https://issuetracker.unity3d.com/issues/scene-is-not-rendered-on-android-8-and-9-when-depth-texture-is-enabled-in-urp-asset)
- Fixed an issue that caused transparent objects to always render over top of world space UI. [case 1219877](https://issuetracker.unity3d.com/product/unity/issues/guid/1219877/)
- Fixed issue causing sorting fudge to not work between shadergraph and urp particle shaders. [case 1222762](https://issuetracker.unity3d.com/product/unity/issues/guid/1222762/)
- Fixed shader compilation errors when using multiple lights in DX10 level GPU. [case 1222302](https://issuetracker.unity3d.com/issues/urp-no-materials-apart-from-ui-are-rendered-when-using-direct3d11-graphics-api-on-a-dx10-gpu)
- Fixed an issue with shadows not being correctly calculated in some shaders.
- Fixed invalid implementation of one function in LWRP -> URP backward compatibility support.
- Fixed issue where maximum number of visible lights in C# code did not match maximum number in shader code on some platforms.
- Fixed OpenGL ES 3.0 support for URP ShaderGraph. [case 1230890](https://issuetracker.unity3d.com/issues/urptemplate-gles3-android-custom-shader-fails-to-compile-on-adreno-306-gpu)
- Fixed an issue where multi edit camera properties didn't work. [case 1230080](https://issuetracker.unity3d.com/issues/urp-certain-settings-are-not-applied-to-all-cameras-when-multi-editing-in-the-inspector)
- Fixed an issue where the emission value in particle shaders would not update in the editor without entering the Play mode.
- Fixed issues with performance when importing fbx files.
- Fixed issues with NullReferenceException happening with URP shaders.
- Fixed an issue that caused memory allocations when sorting cameras. [case 1226448](https://issuetracker.unity3d.com/issues/2d-renderer-using-more-than-one-camera-that-renders-out-to-a-render-texture-creates-gc-alloc-every-frame)
- Fixed an issue where grid lines were drawn on top of opaque objects in the preview window. [Case 1240723](https://issuetracker.unity3d.com/issues/urp-grid-is-rendered-in-front-of-the-model-in-the-inspector-animation-preview-window-when-depth-or-opaque-texture-is-enabled).
- Fixed an issue where objects in the preview window were affected by layer mask settings in the default renderer. [Case 1204376](https://issuetracker.unity3d.com/issues/urp-prefab-preview-is-blank-when-a-custom-forward-renderer-data-and-default-layer-mask-is-mixed-are-used).
- Fixed an issue with reflections when using an orthographic camera [case 1209255](https://issuetracker.unity3d.com/issues/urp-weird-reflections-when-using-lit-material-and-a-camera-with-orthographic-projection)
- Fixed issue that caused unity_AmbientSky, unity_AmbientEquator and unity_AmbientGround variables to be unintialized.
- Fixed issue that caused `SHADERGRAPH_AMBIENT_SKY`, `SHADERGRAPH_AMBIENT_EQUATOR` and `SHADERGRAPH_AMBIENT_GROUND` variables to be uninitialized.
- Fixed SceneView Draw Modes not being properly updated after opening new scene view panels or changing the editor layout.
- Fixed GLES shaders compilation failing on Windows platform (not a mobile platform) due to uniform count limit.
- Fixed an issue that caused the inverse view and projection matrix to output wrong values in some platforms. [case 1243990](https://issuetracker.unity3d.com/issues/urp-8-dot-1-breaks-unity-matrix-i-vp)
- Fixed an issue where the Render Scale setting of the pipeline asset didn't properly change the resolution when using the 2D Renderer. [case 1241537](https://issuetracker.unity3d.com/issues/render-scale-is-not-applied-to-the-rendered-image-when-2d-renderer-is-used-and-hdr-option-is-disabled)
- Fixed an issue where 2D lights didn't respect the Camera's Culling Mask. [case 1239136](https://issuetracker.unity3d.com/issues/urp-2d-2d-lights-are-ignored-by-camera-culling-mask)
- Fixed broken documentation links for some 2D related components.
- Fixed an issue where Sprite shaders generated by Shader Graph weren't double-sided. [case 1261232](https://issuetracker.unity3d.com/product/unity/issues/guid/1261232/)
- Fixed an issue where the package would fail to compile if the Animation module was disabled. [case 1227068](https://issuetracker.unity3d.com/product/unity/issues/guid/1227068/)
- Fixed an issue where Stencil settings wasn't serialized properly in sub object [case 1241218](https://issuetracker.unity3d.com/issues/stencil-overrides-in-urp-7-dot-3-1-render-objects-does-not-save-or-apply)
- Fixed an issue with not being able to remove Light Mode Tags [case 1240895](https://issuetracker.unity3d.com/issues/urp-unable-to-remove-added-lightmode-tags-of-filters-property-in-render-object)
- Fixed an issue where preset button could still be used, when it is not supposed to. [case 1246261](https://issuetracker.unity3d.com/issues/urp-reset-functionality-does-not-work-for-renderobject-preset-asset)
- Fixed an issue where Model Importer Materials used the Standard Shader from the Built-in Render Pipeline instead of URP Lit shader when the import happened at Editor startup.
- Fixed an issue where only unique names of cameras could be added to the camera stack.
- Fixed issue that caused shaders to fail to compile in OpenGL 4.1 or below.
- Fixed an issue where camera stacking with MSAA on OpenGL resulted in a black screen. [case 1250602](https://issuetracker.unity3d.com/issues/urp-camera-stacking-results-in-black-screen-when-msaa-and-opengl-graphics-api-are-used)
- Optimized shader compilation times by compiling different variant sets for vertex and fragment shaders.
- Fixed shadows for additional lights by limiting MAX_VISIBLE_LIGHTS to 16 for OpenGL ES 2.0 and 3.0 on mobile platforms. [case 1244391](https://issuetracker.unity3d.com/issues/android-urp-spotlight-shadows-are-not-being-rendered-on-adreno-330-and-320-when-built)
- Fixed Lit/SimpleLit/ParticlesLit/ParticlesSimpleLit/ParticlesUnlit shaders emission color not to be converted from gamma to linear color space. [case 1249615]
- Fixed missing unity_MatrixInvP for shader code and shaderGraph.
- Fixed XR support for deferred renderer.
- Fixing RenderObject to reflect name changes done at CustomForwardRenderer asset in project view. [case 1246256](https://issuetracker.unity3d.com/issues/urp-renderobject-name-does-not-reflect-inside-customforwardrendererdata-asset-on-renaming-in-the-inspector)
- Fixing camera overlay stacking adding to respect unity general reference restrictions. [case 1240788](https://issuetracker.unity3d.com/issues/urp-overlay-camera-is-missing-in-stack-list-of-the-base-camera-prefab)
- Fixed profiler marker errors. [case 1240963](https://issuetracker.unity3d.com/issues/urp-errors-are-thrown-in-a-console-when-using-profiler-to-profile-editor)
- Fixed issue that caused the pipeline to not create _CameraColorTexture if a custom render pass is injected. [case 1232761](https://issuetracker.unity3d.com/issues/urp-the-intermediate-color-texture-is-no-longer-created-when-there-is-at-least-one-renderer-feature)
- Fixed target eye UI for XR rendering is missing from camera inspector. [case 1261612](https://issuetracker.unity3d.com/issues/xr-cameras-target-eye-property-is-missing-when-inspector-is-in-normal-mode)
- Fixed an issue where terrain and speedtree materials would not get upgraded by upgrade project materials. [case 1204189](https://fogbugz.unity3d.com/f/cases/1204189/)
- Fixed an issue that caused renderer feature to not render correctly if the pass was injected before rendering opaques and didn't implement `Configure` method. [case 1259750](https://issuetracker.unity3d.com/issues/urp-not-rendering-with-a-renderer-feature-before-rendering-shadows)
- Fixed an issue where postFX's temp texture is not released properly.
- Fixed an issue where ArgumentOutOfRangeException errors were thrown after removing Render feature [case 1268147](https://issuetracker.unity3d.com/issues/urp-argumentoutofrangeexception-errors-are-thrown-on-undoing-after-removing-render-feature)
- Fixed an issue where depth and depth/normal of grass isn't rendered to depth texture.
- Fixed an issue that impacted MSAA performance on iOS/Metal [case 1219054](https://issuetracker.unity3d.com/issues/urp-ios-msaa-has-a-bigger-negative-impact-on-performance-when-using-urp-compared-to-built-in-rp)
- Fixed an issue that caused a warning to be thrown about temporary render texture not found when user calls ConfigureTarget(0). [case 1220871](https://issuetracker.unity3d.com/issues/urp-scriptable-render-passes-which-dont-require-a-bound-render-target-triggers-render-target-warning)
- Fixed performance issues in the C# shader stripper.

## [7.1.1] - 2019-09-05
### Upgrade Guide
- The render pipeline now handles custom renderers differently. You must now set up renderers for the Camera on the Render Pipeline Asset.
- Render Pipeline Assets upgrades automatically and either creates a default forward renderer in your project or links the existing custom one that you've assigned.
- If you have custom renderers assigned to Cameras, you must now add them to the current Render Pipeline Asset. Then you can select which renderer to use on the Camera.

### Added
- Added shader function `GetMainLightShadowParams`. This returns a half4 for the main light that packs shadow strength in x component and shadow soft property in y component.
- Added shader function `GetAdditionalLightShadowParams`. This returns a half4 for an additional light that packs shadow strength in x component and shadow soft property in y component.
- Added a `Debug Level` option to the Render Pipeline Asset. With this, you can control the amount of debug information generated by the render pipeline.
- Added ability to set the `ScriptableRenderer` that the Camera renders with via C# using `UniversalAdditionalCameraData.SetRenderer(int index)`. This maps to the **Renderer List** on the Render Pipeline Asset.
- Added shadow support for the 2D Renderer.
- Added ShadowCaster2D, and CompositeShadowCaster2D components.
- Added shadow intensity and shadow volume intensity properties to Light2D.
- Added new Gizmos for Lights.
- Added CinemachineUniversalPixelPerfect, a Cinemachine Virtual Camera Extension that solves some compatibility issues between Cinemachine and Pixel Perfect Camera.
- Added an option that disables the depth/stencil buffer for the 2D Renderer.
- Added manipulation handles for the inner cone angle for spot lights.
- Added documentation for the built-in post-processing solution and Volumes framework (and removed incorrect mention of the PPv2 package).

### Changed
- Increased visible lights limit for the forward renderer. It now supports 256 visible lights except in mobile platforms. Mobile platforms support 32 visible lights.
- Increased per-object lights limit for the forward renderer. It now supports 8 per-object lights in all platforms except GLES2. GLES2 supports 4 per-object lights.
- The Sprite-Lit-Default shader and the Sprite Lit Shader Graph shaders now use the vertex tangents for tangent space calculations.
- Temporary render textures for cameras rendering to render textures now use the same format and multisampling configuration as camera's target texture.
- All platforms now use R11G11B10_UFloat format for HDR render textures if supported.
- There is now a list of `ScriptableRendererData` on the Render Pipeline Asset as opposed to a renderer type. These are available to all Cameras and are included in builds.
- The renderer override on the Camera is now an enum that maps to the list of `ScriptableRendererData` on the Render Pipeline Asset.
- Pixel Perfect Camera now allows rendering to a render texture.
- Light2D GameObjects that you've created now have a default position with z equal to 0.
- Documentation: Changed the "Getting Started" section into "Install and Configure". Re-arranged the Table of Content.

### Fixed
- Fixed LightProbe occlusion contribution. [case 1146667](https://issuetracker.unity3d.com/product/unity/issues/guid/1146667/)
- Fixed an issue that caused a log message to be printed in the console when creating a new Material. [case 1173160](https://issuetracker.unity3d.com/product/unity/issues/guid/1173160/)
- Fixed an issue where OnRenderObjectCallback was never invoked. [case 1122420](https://issuetracker.unity3d.com/issues/lwrp-gl-dot-lines-and-debug-dot-drawline-dont-render-when-scriptable-render-pipeline-settings-is-set-to-lwrp)
- Fixed an issue where Sprite Masks didn't function properly when using the 2D Renderer. [case 1163474](https://issuetracker.unity3d.com/issues/lwrp-sprite-renderer-ignores-sprite-mask-when-lightweight-render-pipeline-asset-data-is-set-to-2d-renderer-experimental)
- Fixed memory leaks when using the Frame Debugger with the 2D Renderer.
- Fixed an issue where materials using `_Time` did not animate in the scene. [1175396](https://issuetracker.unity3d.com/product/unity/issues/guid/1175396/)
- Fixed an issue where the Particle Lit shader had artifacts when both soft particles and HDR were enabled. [1136285](https://issuetracker.unity3d.com/product/unity/issues/guid/1136285/)
- Fixed an issue where the Area Lights were set to Realtime, which caused them to not bake. [1159838](https://issuetracker.unity3d.com/issues/lwrp-template-baked-area-lights-do-not-work-if-project-is-created-with-lightweight-rp-template)
- Fixed an issue where the Disc Light did not generate any light. [1175097](https://issuetracker.unity3d.com/issues/using-lwrp-area-light-does-not-generate-light-when-its-shape-is-set-to-disc)
- Fixed an issue where the alpha was killed when an opaque texture was requested on an offscreen camera with HDR enabled [case 1163320](https://issuetracker.unity3d.com/issues/lwrp-mobile-secondary-camera-background-alpha-value-is-lost-when-hdr-and-opaque-texture-are-enabled-in-lwrp-asset).
- Fixed an issue that caused Orthographic camera with far plane set to 0 to span Unity console with errors. [case 1172269](https://issuetracker.unity3d.com/issues/orthographic-camera-with-far-plane-set-to-0-results-in-assertions)
- Fixed an issue causing heap allocation in `RenderPipelineManager.DoRenderLoop` [case 1156241](https://issuetracker.unity3d.com/issues/lwrp-playerloop-renderpipelinemanager-dot-dorenderloop-internal-gc-dot-alloc-allocates-around-2-dot-6kb-for-every-camera-in-the-scene)
- Fixed an issue that caused shadow artifacts when using large spot angle values [case 1136165](https://issuetracker.unity3d.com/issues/lwrp-adjusting-spot-angle-on-a-spotlight-produces-shadowmap-artifacts)
- Fixed an issue that caused self-shadowing artifacts when adjusting shadow near-plane on spot lights.
- Fixed an issue that caused specular highlights to disappear when the smoothness value was set to 1.0. [case 1161827](https://issuetracker.unity3d.com/issues/lwrp-hdrp-lit-shader-max-smoothness-value-is-incosistent-between-pipelines)
- Fixed an issue in the Material upgrader that caused transparent Materials to not upgrade correctly to Universal RP. [case 1170419](https://issuetracker.unity3d.com/issues/shader-conversion-upgrading-project-materials-causes-standard-transparent-materials-to-flicker-when-moving-the-camera).
- Fixed an issue causing shadows to be incorrectly rendered when a light was close to the shadow caster.
- Fixed post-processing for the 2D Renderer.
- Fixed an issue in Light2D that caused a black line to appear for a 360 degree spotlight.
- Fixed a post-processing rendering issue with non-fullscreen viewport. [case 1177660](https://issuetracker.unity3d.com/issues/urp-render-scale-slider-value-modifies-viewport-coordinates-of-the-screen-instead-of-the-resolution)
- Fixed an issue where **Undo** would not undo the creation of Additional Camera Data. [case 1158861](https://issuetracker.unity3d.com/issues/lwrp-additional-camera-data-script-component-appears-on-camera-after-manually-re-picking-use-pipeline-settings)
- Fixed an issue where selecting the same drop-down menu item twice would trigger a change event. [case 1158861](https://issuetracker.unity3d.com/issues/lwrp-additional-camera-data-script-component-appears-on-camera-after-manually-re-picking-use-pipeline-settings)
- Fixed an issue where selecting certain objects that use instancing materials would throw console warnings. [case 1127324](https://issuetracker.unity3d.com/issues/console-warning-is-being-spammed-when-having-lwrp-enabled-and-shader-with-gpu-instancing-present-in-the-scene)
- Fixed a GUID conflict with LWRP. [case 1179895](https://issuetracker.unity3d.com/product/unity/issues/guid/1179895/)
- Fixed an issue where the Terrain shader generated NaNs.
- Fixed an issue that caused the `Opaque Color` pass to never render at half or quarter resolution.
- Fixed and issue where stencil state on a `ForwardRendererData` was reset each time rendering happened.

## [7.0.1] - 2019-07-25
### Changed
- Platform checks now provide more helpful feedback about supported features in the Inspectors.

### Fixed
- Fixed specular lighting related artifacts on Mobile [case 1143049](https://issuetracker.unity3d.com/issues/ios-lwrp-rounded-cubes-has-graphical-artifacts-when-setting-pbr-shaders-smoothness-about-to-0-dot-65-in-shadergraph) and [case 1164822](https://issuetracker.unity3d.com/issues/lwrp-specular-highlight-becomes-hard-edged-when-increasing-the-size-of-an-object).
- Post-processing is no longer enabled in the previews.
- Unity no longer force-enables post-processing on a camera by default.
- Fixed an issue that caused the Scene to render darker in GLES3 and linear color space. [case 1169789](https://issuetracker.unity3d.com/issues/lwrp-android-scene-is-rendered-darker-in-build-when-graphics-api-set-to-gles3-and-color-space-set-to-linear)

## [7.0.0] - 2019-07-17
### Universal Render Pipeline
- LWRP has been renamed to the "Universal Render Pipeline" (UniversalRP).
- UniversalRP is the same as LWRP in terms of features and scope.
- Classes have moved to the Universal namespace (from LWRP).

### Upgrade Guide
- Upgrading to URP is designed to be almost seamless from the user side.
- LWRP package still exists, this forwards includes and classes to the UniversalRP Package.
- Please see the more involved upgrade guide (https://docs.google.com/document/d/1Xd5bZa8pYZRHri-EnNkyhwrWEzSa15vtnpcg--xUCIs/).

### Added
- Initial Stadia platform support.
- Added a menu option to create a new `ScriptableRendererFeature` script. To do so in the Editor, click on Asset > Create > Rendering > Lightweight Render Pipeline > Renderer Feature.
- Added documentation for SpeedTree Shaders in LWRP.
- Added extended features to LWRP Terrain Shader, so terrain assets can be forward-compatible with HDRP.
- Enabled per-layer advanced or legacy-mode blending in LWRP Terrain Shader.
- Added the documentation page "Rendering in LWRP", which describes the forward rendering camera loop.
- Added documentation overview for how Post Processing Version 2 works in LWRP.
- Added documentation notes and FAQ entry on the 2D Renderer affecting the LWRP Asset.

### Changed
- Replaced beginCameraRendering callbacks by non obsolete implementation in Light2D
- Updated `ScriptableRendererFeature` and `ScriptableRenderPass` API docs.
- Changed shader type Real to translate to FP16 precision on some platforms.

### Fixed
- Fixed a case where built-in Shader time values could be out of sync with actual time. [case 1142495](https://fogbugz.unity3d.com/f/cases/1142495/)
- Fixed an issue that caused forward renderer resources to not load properly when you upgraded LWRP from an older version to 7.0.0. [case 1154925](https://issuetracker.unity3d.com/issues/lwrp-upgrading-lwrp-package-to-7-dot-0-0-breaks-forwardrenderdata-asset-in-resource-files)
- Fixed GC spikes caused by LWRP allocating heap memory every frame.
- Fixed distortion effect on particle unlit shader.
- Fixed NullReference exception caused when trying to add a ScriptableRendererFeature.
- Fixed issue with certain LWRP shaders not showing when using forward/2D renderer.
- Fixed the shadow resolve pass and the final pass, so they're not consuming unnecessary bandwidth. [case 1152439](https://issuetracker.unity3d.com/issues/lwrp-mobile-increased-memory-usage-and-extra-rendering-steps)
- Added missing page for 2D Lights in LWRP.
- Tilemap tiles no longer appear black when you use the 2D renderer.
- Sprites in the preview window are no longer lit by 2D Scene lighting.
- Fixed warnings for unsupported shadow map formats for GLES2 API.
- Disabled shadows for devices that do not support shadow maps or depth textures.
- Fixed support for LWRP per-pixel terrain. [case 1110520](https://fogbugz.unity3d.com/f/cases/1110520)
- Fixed some basic UI/usability issues with LWRP terrain Materials (use of warnings and modal value changes).
- Fixed an issue where using LWRP and Sprite Shape together would produce meta file conflicts.
- Fixed specular calculation fp16 overflow on some platforms
- Fixed shader compilation errors for Android XR projects.
- Updated the pipeline Asset UI to cap the render scale at 2x so that it matches the render pipeline implementation limit.

## [6.7.0] - 2019-05-16
### Added
- Added SpeedTree Shaders.
- Added two Shader Graph master nodes: Lit Sprite and Unlit Sprite. They only work with the 2D renderer.
- Added documentation for the 2D renderer.

### Changed
- The 2D renderer and Light2D component received a number of improvements and are now ready to try as experimental features.
- Updated the [Feature Comparison Table](lwrp-builtin-feature-comparison.md) to reflect the current state of LWRP features.

### Fixed
- When in playmode, the error 'Non matching Profiler.EndSample' no longer appears. [case 1140750](https://fogbugz.unity3d.com/f/cases/1140750/)
- LWRP Particle Shaders now correctly render in stereo rendering modes. [case 1106699](https://fogbugz.unity3d.com/f/cases/1106699/)
- Shaders with 'debug' in the name are no longer stripped automatically. [case 1112983](https://fogbugz.unity3d.com/f/cases/1112983/)
- Fixed tiling issue with selection outline and baked cutout shadows.
- in the Shadergraph Unlit Master node, Premultiply no longer acts the same as Alpha. [case 1114708](https://fogbugz.unity3d.com/f/cases/1114708/)
- Fixed an issue where Lightprobe data was missing if it was needed per-pixel and GPU instancing was enabled.
- The Soft ScreenSpaceShadows Shader variant no longer gets stripped form builds. [case 1138236](https://fogbugz.unity3d.com/f/cases/1138236/)
- Fixed a typo in the Particle Unlit Shader, so Soft Particles now work correctly.
- Fixed emissive Materials not being baked for some meshes. [case 1145297](https://issuetracker.unity3d.com/issues/lwrp-emissive-materials-are-not-baked)
- Camera matrices are now correctly set up when you call rendering functions in EndCameraRendering. [case 1146586](https://issuetracker.unity3d.com/issues/lwrp-drawmeshnow-returns-wrong-positions-slash-scales-when-called-from-endcamerarendering-hook)
- Fixed GI not baking correctly while in gamma color space.
- Fixed a NullReference exception when adding a renderer feature that is contained in a global namespace. [case 1147068](https://issuetracker.unity3d.com/issues/scriptablerenderpipeline-inspector-ui-crashes-when-a-scriptablerenderfeature-is-not-in-a-namespace)
- Shaders are now set up for VR stereo instancing on Vulkan. [case 1142952](https://fogbugz.unity3d.com/f/cases/1142952/).
- VR stereo matrices and vertex inputs are now set up on Vulkan. [case 1142952](https://fogbugz.unity3d.com/f/cases/1142952/).
- Fixed the Material Upgrader so it's now run upon updating the LWRP package. [1148764](https://issuetracker.unity3d.com/product/unity/issues/guid/1148764/)
- Fixed a NullReference exception when you create a new Lightweight Render Pipeline Asset. [case 1153388](https://issuetracker.unity3d.com/product/unity/issues/guid/1153388/)

## [6.6.0] - 2019-04-01
### Added
- Added support for Baked Indirect mixed lighting.
- You can now use Light Probes for occlusion. This means that baked lights can now occlude dynamic objects.
- Added RenderObjects. You can add RenderObjects to a Renderer to perform custom rendering.
- (WIP) Added an experimental 2D renderer that implements a 2D lighting system.
- (WIP) Added a Light2D component that works with the 2D renderer to add lighting effects to 2D sprites.

### Fixed
- Fixed a project import issue in the LWRP template.
- Fixed the warnings that appear when you create new Unlit Shader Graphs using the Lightweight Render Pipeline.
- Fixed light attenuation precision on mobile platforms.
- Fixed split-screen rendering on mobile platforms.
- Fixed rendering when using an off-screen camera that renders to a depth texture.
- Fixed the exposed stencil render state in the renderer.
- Fixed the default layer mask so it's now applied to a depth pre-pass.
- Made several improvements and fixes to the render pass UI.
- Fixed artifacts that appeared due to precision errors in large scaled objects.
- Fixed an XR rendering issue where Unity required a depth texture.
- Fixed an issue that caused transparent objects to sort incorrectly.

## [6.5.0] - 2019-03-07
### Added
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
- Fixed y-flip in VR when post-processing is active.
- Fixed occlusion mesh for VR not rendering before rendering opaques.
- Enabling or disabling SRP Batcher in runtime works now.
- Fixed video player recorder when post-processing is enabled.

## [6.4.0] - 2019-02-21

## [6.3.0] - 2019-02-18

## [6.2.0] - 2019-02-15

### Changed
- Code refactor: all macros with ARGS have been swapped with macros with PARAM. This is because the ARGS macros were incorrectly named.

## [6.1.0] - 2019-02-13

## [6.0.0] - 2019-02-23
### Added
- You can now implement a custom renderer for LWRP. To do so, implement an `IRendererData` that contains all resources used in rendering. Then create an `IRendererSetup` that creates and queues `ScriptableRenderPass`. Change the renderer type either in the Pipeline Asset or in the Camera Inspector.
- LWRP now uses the Unity recorder extension. You can use this to capture the output of Cameras.
- You can now inject a custom render pass before LWRP renders opaque objects. To do so, implement an `IBeforeRender` interface.
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
- Added support for overriding terrain detail rendering shaders, via the render pipeline editor resources asset.

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
- Terrain detail rendering now works correctly when LWRP is installed but inactive.

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

## [3.3.0-preview] - 2018-01-01
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

## [3.2.0-preview] - 2018-01-01
### Changed
- Receive Shadows property is now exposed in the material instead of in the renderer.
- The UI for Lightweight asset has been updated with new categories. A more clean structure and foldouts has been added to keep things organized.

### Fixed
- Shadow casters are now properly culled per cascade. (case 1059142)
- Rendering no longer breaks when Android platform is selected in Build Settings. (case 1058812)
- Scriptable passes no longer have missing material references. Now they access cached materials in the renderer.(case 1061353)
- When you change a Shadow Cascade option in the Pipeline Asset, this no longer warns you that you've exceeded the array size for the _WorldToShadow property.
- Terrain shader optimizations.

## [3.1.0-preview] - 2018-01-01

### Fixed
- Fixed assert errors caused by multi spot lights
- Fixed LWRP-DirectionalShadowConstantBuffer params setting

## [3.0.0-preview] - 2018-01-01
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
- Fixed compilation errors on platforms with limited XRSetting support.

## [2.0.0-preview] - 2018-01-01

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

## [1.1.4-preview] - 2018-01-01

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

## [1.1.2-preview] - 2018-01-01

### Changed
 - Performance improvements in mobile

### Fixed
 - Shadows on GLES 2.0
 - CPU performance regression in shadow rendering
 - Alpha clip shadow issues
 - Unmatched command buffer error message
 - Null reference exception caused by missing resource in LWRP
 - Issue that was causing Camera clear flags was being ignored in mobile


## [1.1.1-preview] - 2018-01-01

### Added
 - Added Cascade Split selection UI
 - Added SHADER_HINT_NICE_QUALITY. If user defines this to 1 in the shader Lightweight pipeline will favor quality even on mobile platforms.

### Changed
 - Shadowmap uses 16bit format instead of 32bit.
 - Small shader performance improvements

### Fixed
 - Subtractive Mode
 - Shadow Distance does not accept negative values anymore


## [0.1.24] - 2018-01-01

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

## [0.1.23] - 2018-01-01

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

## [0.1.12] - 2018-01-01

### Added
 - Standard Unlit shader now has an option to sample GI.
 - Added Material Upgrader for stock Unity Mobile and Legacy Shaders.
 - UI improvements

### Changed
- Realtime shadow filtering was improved.

### Fixed
 - Fixed an issue that was including unreferenced shaders in the build.
 - Fixed a null reference caused by Particle System component lights.
