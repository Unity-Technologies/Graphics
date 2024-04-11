# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [14.0.10] - 2024-04-03

This version is compatible with Unity 2022.3.24f1.

### Changed
- Improved performance entering and leaving playmode for scenes containing large numbers of decal projectors.
- Improved scene culling performance when APV is enabled in the project.

### Fixed
- Removed screen space overlay UI being rendered in offscreen camera.
- Fixed XR texture 2D creation failure due to invalid slice configuration. The slice is misconfigured to 2 when creating Texture2D, causing internal failures.
- Optimize the OnDisable of DecalProjector component when disabling a lot of decals at the same time.
- Removed the error message "Decal texture atlas out of space..." in release builds (it now only appears in the Editor or Development Builds).
- Fixed artifacts on low resolution SSGI when dynamic resolution values are low.
- Fixed internally created Game Objects being deallocated on scene changes.
- Fixed misuse of ternary operators in shaders.
- Fixed a NaN issue in volumetric fog reprojection causing black to propagate in the fog.
- Fixed invalid AABB error in the console when using the APV with reflection probes.
- Fixed a scaling issue with the recorder.
- Restore `EditorGUIUtility.labelWidth` to default after drawing Material GUI.
- Fix specular blend in premultiplied alpha
- Fixed screen node not returning correct resolution after post-processing when dynamic resolution is enabled.

## [14.0.9] - 2023-12-21

This version is compatible with Unity 2022.3.18f1.

### Changed
- Added a warning to the HDRP Wizard if a users project contains materials that cant be upgraded.
- Improved skyContext caching when the sky renderer changes.

### Fixed
- Allowed users to change the maximum amount of lights used in a local neighborhood in the HDRP path tracer through the shader config mechanism.
- Fixed layered lit displacement.
- Improved VolumetricSky caching and Reduced significantly memory allocation for scenes with multiple realtime reflection probes.
- Gray out the UI of light cluster override and show the same message as path tracing if raytracing is disabled.
- Fixed an issue where non directional light could react to "interact with sky" flag.
- Fixed crash when cleaning up the reflection probe camera cache.
- Fixed a SetData error when using more lights in a scene than the configured max light count settings.
- Fixed blending between cascaded shadowmaps and shadowmask as well as cascades border ranges.
- Fixed Turkish OS incorrectly deducing DLSS is not available.
- Fixed an issue where Reflection Proxy Volume would cause artifacts to cover the editor on Apple Silicone devices.
- Ensure documentation clearly lists lack of support for Box Lights in path tracing.
- The lightShadowCasterMode property on Light now only affects shadow caster culling when baked lighting includes shadow mask, as intended.
- Added in which space custom velocity should be computed.
- Updated decal projector draw distances when global draw distance changes.
- Added additional documentation for cached shadows of directional lights.
- Fixed performance issue with reflection probe inspector.
- Fixed XR SPI is not disabled after processing the render request.
- Fixed potential leaks when using dynamic resolution and objects with refraction.
- Corrected dynamic resolution settings for offscreen UI.
- Added missing texture array global mip bias override for texture array grad samplers.
- Fixed ShaderGraph being dirty when opened just after the creation of the asset.
- Added index seed mode for path tracing to avoid "sticky" noise patterns when using path tracing in conjunction with Recorder.
- Fixed time step of watersystem for recorder.
- Fixed issues with hardware DRS on console (manifestation is usually bright qnan pixels on the right of the screen) when using half resolution transparent.
- Fixed triplanar on alpha clipped geometry.
- Flares now respect the cameras culling mask and the game objects layer (Occlusion and Rendering).
- Optimize PBR sky precomputation and memory usage.
- Fix Blackman-Harris filter for temporal AA.
- Fix ShaderGraph with motion vectors enabled overwriting interpolators with previous frames data
- Fixed inverted shadows from transparent objects in HDRP path tracer.
- Fixed sentence in "Ray Tracing: Getting started" documentation
- Fix Console errors with ReflectionProxyVolume component Gizmo
- Fixed a culling result sharing issue between custom passes and the camera rendering them.
- Increase HDRP's maximum cube reflection probes on screen
- Fix exception thrown when running projects for an extended amount of time
- Fixed post-processing when the LUT size is not a power of 2
- Fix creating mirror Gameobject not being placed in prefab hierarchy
- Fix Disk Light's property not being updated when changing it's radius using the gizmo in the scene.

## [14.0.8] - 2023-09-27

This version is compatible with Unity 2022.3.11f1.

### Changed
- Improved CPU performances by disabling "QuantizedFrontToBack" sorting in opaque rendering.
- Avoid clamping to integers for HDR manipulation.
- Reduced GC Alloc when using raytracing and HDRP.
- Updated description of Decal Projector Draw Distance setting to mention HDRP asset setting.

### Fixed
- Enabling raytracing no longer disable screen space lighting effect (SSAO, SSR) async compute
- Made HDRP RenderPIpelineSettings public to enable customizing the HDRP asset.
- Properly take into account sky attenuation for baking.
- Updated HDRenderPipelineResources file.
- Fixed HDProbes to support custom resolutions for all rendering modes.
- Fixed TAA aliasing edge issues on alpha output for recorder / green screen. This fix does the following:
* Removes history rejection when the current alpha value is 0. Instead it does blend with the history color when alpha value is 0 on the current plane.
* The reasoning for blending again with the history when alpha is 0 is because we want the color to blend a bit with opacity, which is the main reason for the alpha values. sort of like a precomputed color
* As a safety, we set the color to black if alpha is 0. This results in better image quality when alpha is enabled.
- Added check to ensure gismos arent rendered when they shouldnt be.
- Fixed quad overdraw debug at high resolution.
- Fixed cloud layer rotation does not allow for smooth rotation.
- Fixed GetScaledSize when not using scaling.
- Fixed VT init to avoid RTHandle allocation outside of HDRP rendering loop.
- Upgrading from DLSS 2.4 to DLSS 3.0 for upscaling part.
- [Backport] Fix the incorrect base color of decals for transparency.
- Fixed error when camera goes underwater.
- Fixed shaders stripping for Lens Flares.
- Fixed color pyramid history buffer logic when history is reset and the color pyramid is not required.
- Fixed scene template dependencies.
- Minor fix to HDRP UI when Raytraced AO is enabled.
- Added a new custom pass injection after opaque and sky finished rendering.
- Fixed D3D validation error for area lights in HDShadowAtlas.
- Fixed baked light being wrongly put in the cached shadow atlas.
- Improving DLSS ghosting artifacts a little bit, by using a better pre-exposure parameter. Fixing reset history issues on DLSS camera cuts.
- Added an helpbox for local custom pass volumes that doesn't have a collider attached.
- Respect the transparent reflections settings when using raytracing.
- Show base color texture on decal materials if Affect BaseColor is disabled.
- Fixed inconsistent documentation about hardware supporting raytracing.
- Fixed wrong metapass when using planar/triplanar projection in HDRP.
- Fixed fireflies in path traced volume scattering using MIS. Add support for anisotropic fog.
- When HDRP is disabled, Compute Shaders are being stripped.
- Fixed recovering the current Quality level when migrating a HDRP Asset.
- Added warning to reflection probe editor to prevent user from baking in a low quality level.
- Fixed Decal additive normal blending on shadergraph materials.
- Fixed custom pass injection point "After Opaque And Sky" happening after cloud rendering.
- Fixed FTLP (Fine Tiled Light Pruning) Shader Options max light count. Previous support only supported up to 63 These changes allow to go up to 255 with higher instability as numbers per tile approach 255.
For support greater than 255, do it at your own risk! (and expect some flickering).
- Mixed runtime lights were not considering the intensity multiplier during bakes. These changes fix this behaviour and make bakes more intuitive.
- Fixed the incorrect size of the material preview texture.
- Removing linq and complexity on light units validation
Light units validation was using Linq, which is full of memory allocations and very expensive in the CPU.
Instead, opting to use a simple bitmask to check wether light unit is valid or not for a certain light type.
Caching also managed arrays to avoid in frame allocations.
- Fixed prefab preview rendering dark until moved.
- Fixed material previews being rendered black.
- Fixed: realtime Reflection probe makes volumetrics clouds wind stop.
- Fixed error on water inspector when no SRP is active.
- Fixed preview for refractive materials with MSAA.
- Allow the game to switch HDR on or off during run time.
- Fixed GraphicsBuffer leak from APV binding code.
- Re-enabled HDR output on Mac (Was disabled).
- Fixed Volumetric Fog rendering before the injection point "AfterOpaqueAndSky".
- Fixed an issue where an async pass would try to sync to a culled pass mistakenly.
- Fixed the logic used to set up materials featuring displacement mapping that would sometimes result in artifacts or suboptimal performance.
- Mixed tracing mode for transparent screenspace reflections now mixes both tracing modes as expected, instead of only using ray traced reflections.
- Fixed custom post process volume component example in doc.
- Fixed ShaderGraph Decal material position issue by using world space position.
- Fixed error when assigning non water material to water

## [14.0.7] - 2023-05-23

This version is compatible with Unity 2022.2.22f1.

### Changed
- Changed references of Diffusion Profile in the HDRP Wizard check by the ones in the HDRP Package.
- Enabled Extend Shadow Culling in Raytracing by default.
- Fixed usage of FindObjectsOfType to use FindObjectsByType(FindObjectsSortMode.None).
- Added a script to drive dynamic resolution scaling in HDRP.
- Added "WorldSpacePosition" to fullscreen debug modes.

### Fixed
- Fixed water simulation time in playmode.
- Fixed emissive decals not working on shaders based on LayeredLit, LayeredLitTesselation, LitTesselation, TerrainLit, TerrainLit_Basemap.
- Fixed UI issues in Render Graph Viewer.
- Fixed the volumetric clouds presets so it now propagates their values if changed by script.
- Fixed an issue with ray tracing initialization when switching between render pipeline assets.
- Added error when MSAA and non-MSAA buffers are bound simultaneously in custom passes.
- Fixed the label and improved documentation for After Post Process depth test flag to give more detail about "Depth Test" being automatically disabled in some cases.
- Fixed the low resolution transparents using Shader Graph.
- Fixed the albedo and specular color override so it is now considered as sRGB.
- Fixed the exposure for SSR debug rendering.
- Fixed the raytraced reflections for box lights so they are no longer cut off if the range is too small.
- Better Reflection Probe Debug_"Icon".
- Fixed an issue with Mac and HDR so it now shows correct results when HDR is enabled.
- Fixed a glitch in one frame in the Editor when using path tracing.
- Fixed HDSceneDepth triggering errors for uninitialized values.
- Enabled path tracing to now produce correct results when dynamic resolution is enabled.
- Fixed some colliders being disabled when cancelling an APV bake.
- Fixed the init order that could cause DXR setup to fail after using the HDRP wizard to enable DXR on an existing HDRP project.
- Fixed an issue occuring on TAAU when the camera rect is adjusted.
- Enabled the volumetric clouds to be synced per camera. Previously, the clouds were synced through a global time, leading to discrepencies with cameras that update at different rates.
- Fixed the PrefabStage with Lensflare not included in the object, include the lensflare only if it was included on the prefab (children included).
- Enabled the correct light position when changing distance on a Light Anchor.
- Fixed material upgrader when executing tests.
- Improved the console warning message when the maximum number of shadows is reached in the view.
- Clamp mouse pixel coords in tile debug view.
- Fixed an issue where LOD-related frame render settings UI on the camera component would not reflect the current global default settings.
- Fixed ray-traced emissive reflections.
- Fixed swapped tooltips on decal materials for ambient occlusion and smoothness.
- Fixed issue with Light Probe Proxy Volume not rendering correctly when Bounding Box Mode is Automatic World.
- Fixed transparent decal textures being added into atlas even if the material properties have disabled them.
- Fixed Volumetric Clouds jittering when the sun was not casting shadow.
- Fixed memory leak in HDLightRenderDatabase when switching between editor and play and no lights are in the scene.
- Fixed DLSS Ultra performance setting which was not calculating the correct resolution. The setting was not pushing the correct resolution due to a typo in the code.
- Fixed keyword clear when creating shadergraph material.
- Fixed a shader compilation issue on fog volumes when Turkish language is installed as locale.
- Fixed an issue where the quality settings tags were displayed cut-off.
- Fixed the default value of _ZTestDepthEqualForOpaque in unlit ShaderGraphs.
- Fixed free CullingGroups still being used during culling.
- Fixed APV brick placement when multiple probe volumes with different object layer mask and subdivision levels overlaps.
- Fixed ShaderGraph materials using SSS.
- Fixed HDRP Decal Emisive Map is drawn incorrectly when Decal is at a certain distance from Camera and specific "Clipping Planes" property values are set under the "Camera" component.
- Updated some missing HDRP component documentation URLs.
- Fixed the shadow culling planes for box-shaped spot lights.
- Fixed square artifacts on 1/4 res pbr dof and warning during player builds.
- Enabling raytracing no longer disable screen space lighting effect (SSAO, SSR) async compute
- Made HDRP RenderPIpelineSettings public to enable customizing the HDRP asset.
- Properly take into account sky attenuation for baking.

## [14.0.6] - 2023-03-24

This version is compatible with Unity 2022.2.13f1.

### Added
- DecalShaderGraphGUI.SetupDecalKeywordsAndPass - Adding back a public API used to validate shadergraph materials by user scripts.

### Fixed
- Fixed diffusion profile list upgrade.
- Fixed usage of HDMaterial.ValidateMaterial for materials created from ShaderGraphs.
- Fixed LightList keywords showing errors in the log when strict variant matching is enabled.
- Fixed a serialization issue affecting other objects.
- Fixed time determinism for water surfaces
- Fixed world position offset in water CPU simulation
- Fix error with water and dynamic pass culling

## [14.0.5] - 2022-12-12

This version is compatible with Unity 2022.2.4f1.

### Fixed
- Fixed shadergraph using derivatives and Raytracing Quality keyword.
- Removed misleading part of a LensFlare tooltip.
- Removed unused voluimetric clouds volume component on new scenes templates.
- Fixed texture wrapping of cloud layer.
- Fixed custom pass scaling issues with dynamic resolution.
- Fixed an issue with low resolution depth of field producing a cropped result in some scenarios.
- Fixed transmission on directional lights.
- Fixed slight change of color in background when changing scene hierarchy.
- Fixed color grading so it no longer outputs negative colors.
- Fixed range of spill removal parameter in graphics compositor UI.
- Fixed exposure of recorded frames with path tracing and auto exposure.
- Fixed volumetric cloud incorrectly display in lighting debug mode
- Fixed Ray Tracing Mixed Mode Collisions.
- Fixed minor bug in the shadow ray culling for the cone spot light.
- Fixed zfighting artifacts for ray tracing.
- Fixed the indirect diffuse lighting in ray tracing so it now always works properly.
- Added clarification on HDR Output not supporting Scene View.
- Fixed Missing tooltip for Screen Weight Distance in Screen Space Refraction Override.
- Fixed missing tag on decal shader properties.
- Fixed mouse position in debug menu with scaled screens in editor.
- Fixed null reference error in the rendering debugger when no camera is available.
- Fixed a black screen issue with master builds on HDRP.
- Fixed the fallback section so it now disappears when hiding the additional data.
- Fixed black line in ray traced reflections.
- Renamed IOR output in Eye shadergraph for clarification.
- Fixed Text alignment in Transparency Inputs section.
- Fixed an issue with Bloom and Depth of Field in game view when filtering in the hierarchy.
- Fixed the ray tracing shadow denoiser s it no longer produces leaks at the edge of spotlight shadows.
- Re-enabled XR tests for 004-CloudsFlaresDecals and 005-DistortCloudsParallax.
- Improved the script linking the directional light to a Custom Render Target calling the RenderPipeline function GetMainLight().
- Enabled SSR transparent in default framesettings.
- Fixed scalarization issues on Gamecore.

## [14.0.4] - 2022-11-04

This version is compatible with Unity 2022.2.2f1.

### Changed
- Further improved the consistency of non-physical depth of field at varying native rendering resolutions and resolution scales.
- Added a new sample for fullscreen master node.
- Improved Material sample by adding 3 scenes about stacking transparent materials in the 3 different rendering path (Raster, Ray Tracing, Path Tracing).
- Improved area light soft shadow - when the new **Very High** shadow filtering quality level is selected, area lights use improved PCSS filtering for wider penumbras and better contacts.
- Added Screen space for the Transform node.
- Changed the reflection probes cube texture array cache to be a 2D texture atlas in the octahedral projection.
* Reflection probe resolution resolution can be chosen individually.
* Reflection probes and planar reflection probes atlas's are combined in a single atlas.
- Removed diffusion profiles from global settings.
- Changed `DiffusionProfileOverride` to accumulate profiles instead of replacing when interpolating at runtime.
- Added a new iteration on the water system.
- Changed ACES luminance fit to allow pure whites.
- Displayed selection for common values on the Rendering Debugger for Material -> Material.
- Updated an out-of-date guide in TextureStack.hlsl.
- Added Volumetric Material samples.

### Fixed
- Fixed the blend mode label field.
- Fixed a render error when you disable both motion vectors and opaques.
- Fixed a render graph error when you open a project with default lighting enabled and clouds in the scene.
- Fixed a NaN resulting from path traced hair materials with certain base color inputs.
- Improved the default state of newly created Planar Reflection Probes.
- Fixed accumulation when shutter interval is zero.
- Added  a mechanism in HDRP to strip `FragInputs`, to strip some interpolators in the pixel shader for shader graphs.
- Updated accumulation API scripts to solve issue with screen shot capture in certain Unity Editor workflows.
- Fixed custom post-processes not released correctly when you switch HDRP assets.
- Fixed a render graph error when using Output AOV in non-dev builds.
- Fixed errors in CPU lights so that `includeForRaytracing` and `lightDimmer` work for `HDAdditionalLightData` and camera relative rendering.
- Fixed path tracing to better match transparent object behavior observed in rasterization.
- Fixed an issue with the ray traced screen space shadows slots/indices.
- Fixed some artifacts that happened when ray traced shadows are evaluated for a surface that is far from the camera.
- Allowed you to select the multiscattering method from SG advanced settings.
- Fixed Volumetric Clouds texture input fields.
- Fixed the documentation for recursive rendering so it's clear enough for the smoothness' behavior.
- Disabled camera jittering in path tracing.
- Fixed errors in the HDR comparison doc.
- Fixed Cloud Layer rendering on Nvidia GPUs.
- Fixed errors when you switch to SMAA.
- Fixed a performance issue with Single Shadow debug mode.
- Fixed an issue with decals not scaling with a parent transform.
- Fixed the Texture2D and Texture3D parameters to accept `None` as value.
- Fixed scalarization issues on Gamecore.
- Fixed the ambient probe for the volumetric clouds for the sky cubemap so they're ready at the first frame.
- Fixed artifacts on the edges of the screen when you enable volumetric clouds.
- Added a blendable perceptual blending parameter on volumetric clouds to get rid of over exposure artifacts.
- Fixed the SSR so it works properly on deferred with tiles with multiple variants.
- Fixed artifacts on the volumetric clouds when you enable the fog.
- Fixed realtime reflection probes using a mode of every frame to update at most once per frame.
- Added the ability to remap the occlusion to anything via a curve for `LensFlareOcclusionSRP` (DataDriven).
- Fixed reflection probes allowing clear depth.
- Fixed the HDRP path tracer denoising when resetting denoising with AOVs is enabled.
- Fixed the HDRP path tracer denoising temporal mode when rt handle scale is not one.
- Virtual texturing streaming loading no longer hindered by transparent materials. Transparent materials, depending on their transmitance or alpha, will let the VT streaming system requests textures appropiately.
- Fixed blending artifacts with physically based DoF.
- Fixed a crash on standalone profiler executing the HDRP Upgrader
- Added a check to prevent users from clicking the denoising package install button multiple times while waiting for the installation to finish.
- Fixed the clamp happening on the sum of ray tracing samples instead of per sample.
- Fixed quad artifacts on TAA and fixed an issue on bicubic filtering.
- Added a specular occlusion fallback on normal when bent normal is not available.
- Fixed an issue by greying out the profile list button instead of throwing an error.
- Fixed a rounding issue in ray traced reflections at half resolution.
- Fixed a discrepency between recursive rendering and path tracing for refraction models.
- Fixed duplicated code sample in the custom pass documentation.
- Disabled Volumetric Clouds for Default Sky Volumes.
- Fixed the default DXR volume not having any DXR effects enabled.
- Updated misleading tooltip in the environment lighting in HDRP.
- Fixed isssue with up direction of the light anchor tool sometime getting wrong.
- Fixed reflection issue upon scene filtering.
- Fixed leaks in ray tracing effects due to missing ambient probe for ray tracing effects.
- Fixed artifacts on PBR DOF camera cuts such as the COC sticking around with blurry values.
- Fixed a render graph error when rendering a scene with no opaque objects in forward.
- Fixed an issue with DOTS and Look Dev tool causing entities in the tool to be drawn in the game view.
- Fixed noisy top shadows when using 'High' Filtering Quality with Tesselated Meshes (Lit Tesselation).
- Fixed incorrect false positives in shadow culling.
- Added async compute support doc.
- Fixed Decal Layer Texture lifetime in rendergraph.
- Added missing using statements in one of the example scripts in the documentation for the accumulation API.
- Fixed SSGI using garbage outside the frustum.
- Fixed broken denoiser for ray traced shadows in HDRP.
- Fixed history transform management not being properly handeled for ray traced area shadows.
- Fixed an issue with RTHandle sampling out of bounds on previous frame pyramid color. This occasionally caused bad pixel values to be reflected.
- Initialize DLSS at loading of HDRP asset. Previously intialization was too late (ad HDRP pipeline constructor). Moved initialization to OnEnable of SRP asset.
- Fixed transmission for box lights.
- Fixed upperHemisphereLuxValue when changing HDRI Sky.
- Fixed the ray traced ambient occlusion not rejecting the history when it should leading to severe ghosting.
- Fixed tonemapping not being applied when using the Show Cascades debug view.
- Fixed Geometric AA tooltip.
- Fixed shadow dimmer not affecting screen space shadows.
- Fixed HDR Output behaviour when platform doesn't give back properly detected data.
- Fixed sky rendering in the first frame of path tracing. This also fixes issues with auto-exposure.
- Fixed bad undo behaviour with light layers and shadows.
- Fixed a null ref exception when destroying a used decal material.
- Fixed color grading issue when multiple cameras have same volume properties but different frame settings.
- Fixed discrepency in the fog in RT reflections and RTGI between perf and quality.
- Fixed tessellation in XR.
- Improved SRP Lens flares occlusion.
- Shadow near plane can now be set to 0, clamped to 0.01 only on Cone, Pyramid and Point Lights.
- Fixed shader compilation errors when adding OpenGL in an HDRP project.
- Fixed saving after auto register of a diffusion profile.
- Fixed material preview with SSS.
- Fixed stencil state not set correctly when depth write is enabled in custom passes.
- Fixed an issue that the Shaders now correctly fallback to error shader.
- SRP Lens flares occlusion improvements.
- Fixed the new ray tracing quality nodes not working.
- Clear custom pass color and depth buffers when the fullscreen debug modes are enabled.
- Fixed SpeedTree importer when shader has no diffusion profile.
- Fixed the error message saying the HDRP is not supported on a certain platform.
- Removed "Sprite Mask" from scene view draw modes as it is not supported by HDRP.
- Fixed custom pass UI not refreshed when changing the injection point.
- Fixed Depth Of Field compute shader warnings on metal.
- Fixed refraction fallback when object center is out of screen.
- Fixed the material rendering pass not correctly changed with multi-selection.
- Clamp negative absorption distance.
- Fixed spams in the console related to the water inspector.
- Fixed the display of the water surfaces in prefab mode.
- Fixed unwanted menu item related to water amplitude generation.
- Fixed a regression issue which breaks XR font rendering.
- Fixed light layers when using motion vectors.
- Display Stats is now always shown in the first position on the Rendering Debugger.
- Fixed an issue to initialize Volume before diffusion profile list.
- Fixed nullref exception when trying to use the HD Wizard "fix all" button.
- Fixed the majority of GCAllocs with realtime planar probes and on demand update of reflection probes.
- Fixed shadows in transparent unlit shadow matte.
- Fixed interleaved sampling for secondary rays in the path tracer.
- Fixed sky/background alpha in path tracing (affects post processing).
- Fixed issues with ray traced area light shadows.
- Fixed hard edges on volumetric clouds.
- Fixed missing RT shader passes for the LOD terrain mesh.
- Fixed scroll speed in Local Density Volumes not updating.
- Fixed an error regarding the max number of visible local volumetric fog in the view.
- Fixed volumetric fog samples description docs.
- Fixed sky in path traced interleaved tiled rendering.
- Fixed bug in path tracing dirtiness check.
- Fixed Ray Tracing leaks due to not supporting APV.
- Fixed an implementation problem related to the environment lights and ray tracing.
- Fixed the water system causing exceptions when enabling MSAA.
- Fixed APV leaking into preview rendering.
- Fixed unlit pixels contributing to screen space effects.
- Fixed missing limitation in path tracing documentation regarding Local Volumetric Fog.
- Fixed the ray tracing reflection denoiser being partially broken.
- Fixed ambient probe for volumetric clouds.
- Removed unwanted RTAO effect on indirect specular lighting.
- Fixed a number of outdated reference to "HDRP Default Settings" in the UI.
- Fixed unnecessary loss of precision when all post processing are disabled.
- Fixed decal material validation after saving.
- Fixed Layer List is not duplicated when duplicating a LayeredLit Material.
- Worked around exception when enabling raytracing when resources for raytracing have not been built.
- Fixed graphics issues with sky and fog in game view when filtering objects in the hierarchy.
- Fixed performance when volumetric fog is disabled.
- Improved the motion and receiver rejection tooltips for RTGI and RTAO.
- Fixed black dots when clouds rendered in local mode.
- Fixed embedding the config package when it's not a direct dependency.
- Fixed scene depth node not working in the Decal ShaderGraph material type.
- Fixed migration of diffusion profiles on read only packages.

## [14.0.3] - 2021-05-09

This version is compatible with Unity 2022.2.0b15.

### Changed
- Added a new/clone button to lens flare data picker.
- Added an error message in the custom pass volume editor when custom passes are disabled in the HDRP asset.
- Fixed a missing menu item to create reflection proxy volume.
- Renamed the *Ambient Occlusion Volume Component* to *Screen Space Ambient Occlusion*.
- Fixed so the Rendering Debugger isn't recreated on UI changes.
- Used anti leaking in reflection probes and changed the default values for APV.
- Added clamp to reflection probe normalization factor computed from APV.
- Changed default size of the debug APV probes, moved the menu entry for the APV baking window and clarified the tooltip for APV simplification levels.
- Changed the way HDRP volumetric clouds presets are handled to allow blending.

### Fixed
- Fixed a compilation error when editor color space is gamma.
- Fixed min percentage of dynamic resolution in HDRenderPipeline not clamped (case 1408155).
- Updated frame diagram image in documentation (missing Flim grain and Dithering).
- Fixed custom pass material editor not displaying correctly read-only materials.
- Fixed HDRP Wizard windows duplicated when entering in play mode.
- Fixed error on lens flare enabled causing motion vectors to be faulty.
- Fixed error on LitTessellation material editor when Planar or Triplanar UVMapping is selected.
- Fixed issue with overblown exposure when doing scene filtering.
- Fixed pivot position in the eye material sample scene.
- Fixed volumetric fog being clamped by the max shadow distance on metal.
- Added injection points options for DLSS (After Depth of Field and After Post) which should mitigate some of the performance cost in post process.
- Fixed an issue so you can build and run a HDRP Project for the first time without an error.
- Fixed prefab mode context visibility so it hides passes, decals, and local volumetric fog objects.
- Fixed volumetric fog being clamped by the max shadow distance on metal.
- Fixed MinMax of Transparent Thickness.
- Improved error reporting from exceptional RenderGraph states.
- Fixed an invalid undo step when you edit some values on the Rendering Debugger.
- Fixed so that when you perform a reset on the rendering debugger, it hides elements.
- Fixed errors when cancelling an APV bake and trigger a bake right after.
- Fixed the unloading of scenes so it doesn't cause the unload of APV data of other scenes.
- Fixed a compilation warning when installing the procedural sky HDRP sample.
- Fixed a warning on first frame when a realtime probe is set to OnEnable.
- Fixed raytrace shader not working in final player build.
- Fixed constant repaint when static sky set to none.
- Fixed label for background clouds in Environment Lighting tab.
- Fixed Planar Probe not rendering when sky is None.
- Fixed black screen with MSAA and TAAU both enabled.
- Added the volumetric clouds to the feature list of HDRP.
- Fixed decal angle fade for decal projectors.
- Fixed the UX for baked reflection probes.
- Removed clamping for ray traced reflections on transparent objects.
- Fixed an issue regarding the scaling of texture read from the after-post-process injection point.
- Fixed so volumetric fog color doesn't height fog when disabled.
- Fixed issue with specular occlusion being wrongly quantized when APV is enabled in HDRP.
- Added the option to automatically register diffusion profiles on import.
- Fixed water simulation so it stops when you enter pause mode in the unity editor.
- Fixed path tracer denoising in player builds.
- Changed the Box Model name to Planar Model.
- Updated DLSS test images for a driver update.

## [14.0.2] - 2021-02-04

This version is compatible with Unity 2022.2.0a14.

### Added
- Added denoising for the path tracer.
- Added an initial version of under water rendering for the water system.
- Added option to animate APV sample noise to smooth it out when TAA is enabled.
- Added default DOTS compatible loading shader (MaterialLoading.shader)
- Add #pragma editor_sync_compilation directive to MaterialError.shader
- Added the culling matrix and near plane for lights, so that they can be custom-culled with the BatchRenderGroup API.
- Added an optional CPU simulation for the water system.
- Added new Unity material ball matching the new Unity logo.
- Adding injection points options for DLSS (After Depth of Field and After Post) which should mitigate some of the performance cost in post process.

### Changed
- Moved custom Sensor Lidar path tracing code to the SensorSDK package.
- Optimized real time probe rendering by avoid an unnecessary copy per face.

### Fixed
- Fixed couple bugs in the volumetric clouds shader code.
- Fixed PBR Dof using the wrong resolution for COC min/max filter, and also using the wrong parameters when running post TAAU stabilization. (case 1388961)
- Fixed the list of included HDRP asset used for stripping in the build process.
- Fixed HDRP camera debug panel rendering foldout.
- Fixed issue with Final Image Histogram displaying a flat histogram on certain GPUs and APIs.
- Fixed various issues with render graph viewer when entering playmode.
- Fixed Probe Debug view misbehaving with fog.
- Fixed issue showing controls for Probe Volumes when Enlighten is enabled and therefore Probe Volumes are not supported.
- Fixed null reference issue in CollectLightsForRayTracing (case 1398381)
- Fixed camera motion vector pass reading last frame depth texture
- Fixed issue with shader graph custom velocity and VFX (case 1388149)
- Fixed motion vector rendering with shader graph with planar primitive (case 1398313)
- Fixed issue in APV with scenes saved only once when creating them.
- Fixed probe volume baking not generating any probes on mac.
- Fix a few UX issues in APV.
- Fixed issue with detail normals when scale is null (case 1399548).
- Fixed compilation errors on ps5 shaders.

## [14.0.1] - 2021-12-07

### Added
- Added an option on the lit shader to perform Planar and Triplanar mapping in Object Space.
- Added a button in the Probe Volume Baking window to open the Probe Volume debug panel.
- Added importance sampling of the sky in path tracing (aka environment sampling).
- Added the overlay render queue to custom passes.
- Added a callback to override the View matrix of Spot Lights.
- Added Expose SSR parameters to control speed rejection from Motion Vector including computing Motion Vector in World Space.
- Added a Layer Mask in the Probe Volume Settings window to filter which renderers to consider when placing the probes.
- Added Refract Node, Fresnel Equation Node and Scene-Difference-Node (https://jira.unity3d.com/browse/HDRP-1599)
- Added Remap alpha channel of baseColorMap for Lit and LayeredLit
- Added an option for culling objects out of the ray tracing acceleration structure.
- Added more explicit error messages when trying to use HDSceneColor, NormalFromHeight, DDX, DDY or DDXY shader graph nodes in ray tracing.
- Added public API for Diffusion Profile Override volume Component.
- Added time slicing support for realtime reflection probes.

### Changed
- Render Graph object pools are now cleared with render graph cleanup
- Updated Physically Based Sky documentation with more warnings about warmup cost.
- Force Alpha To Coverage to be enabled when MSAA is enabled. Remove the Alpha to Mask UI control.
- Improved the probe placement of APV when dealing with scenes that contains objects smaller than a brick.
- Replaced the geometry distance offset in the Probe Volume component by a minimum renderer volume threshold to ignore small objects when placing probes.
- Small improvement changes in the UX for the Unlit Distortion field.
- Improvements done to the water system (Deferred, Decals, SSR, Foam, Caustics, etc.).
- Changed the behavior the max ray length for recursive rendering to match RTR and rasterization.
- Moved more internals of the sky manager to proper Render Graph passes.
- Disabled the "Reflect Sky" feature in the case of transparent screen space reflections for the water system.
- Renamed the Exposure field to Exposure Compensation in sky volume overrides (case 1392530).
- Disabled the volumetric clouds for the indoor template scenes (normal and DXR) (case 1381761).
- Post Process can now be edited in the default frame settings.
- Disallow "Gradient Diffusion" parameter to be negative for the "Gradient Sky".
- Disabled volumetric clouds in lens flares sample indoor scene.
- Make Vertical gate fit the default for physical camera.
- Changed how the ambient probe is sent to the volumetric clouds trace pass (case 1381761).

### Fixed
- Fixed build warnings due to the exception in burst code (case 1382827).
- Fixed SpeedTree graph compatibility by removing custom interpolators.
- Fixed default value of "Distortion Blur" from 1 to 0 according to the doc.
- Fixed FOV change when enabling physical camera.
- Fixed spot light shadows near plane
- Fixed unsupported material properties show when rendering pass is Low Resolution.
- Fixed auto-exposure mismatch between sky background and scene objects in path tracing (case 1385131).
- Fixed option to force motion blur off when in XR.
- Fixed write to VT feedback in debug modes (case 1376874)
- Fixed the water system not working on metal.
- Fixed the missing debug menus to visualize the ray tracing acceleration structure (case 1371410).
- Fixed compilation issue related to shader stripping in ray tracing.
- Fixed flipped UV for directional light cookie on PBR Sky (case 1382656).
- Fixing missing doc API for RTAS Debug display.
- Fixed AO dissapearing when DRS would be turned off through a camera, while hardware drs is active in DX12 or Vulkan (case 1383093).
- Fixed misc shader warnings.
- Fixed a shader warning in UnityInstancing.hlsl
- Fixed for APV debug mode breaking rendering when switching to an asset with APV disabled.
- Fixed potential asymmetrical resource release in the volumetric clouds (case 1388218).
- Fixed the fade in mode of the clouds not impacting the volumetric clouds shadows (case 1381652).
- Fixed the rt screen space shadows not using the correct asset for allocating the history buffers.
- Fixed the intensity of the sky being reduced signficantly even if there is no clouds (case 1388279).
- Fixed a crash with render graph viewer when render graph is not provided with an execution name.
- Fixed rendering in the editor when an incompatible API is added (case 1384634).
- Fixed issue with typed loads on RGBA16F in Volumetric Lighting Filtering.
- Fixed Tile/Cluster Debug in the Rendering Debugger for Decal and Local Volumetric Fog
- Fixed timeline not updating PBR HDAdditionalLightData parameters properly.
- Fixed NeedMotionVectorForTransparent checking the wrong flag.
- Fixed debug probe visualization affecting screen space effects.
- Fixed issue of index for APV running out space way before it should.
- Fixed issue during reloading scenes in a set when one of the scenes has been renamed.
- Fixed Local Volumetric Fog tooltips.
- Fixed issue with automatic RendererList culling option getting ignored (case 1388854).
- Fixed an issue where APV cells were not populated properly when probe volumes have rotations
- Fixed issue where changes to APV baking set lists were not saved.
- Fixed Correlated Color Temperature not being applied in Player builds for Enlighten realtime GI lights (case 1370438);
- Fixed artifacts on gpu light culling when the frustum near and far are very spread out (case 1386436)
- Fixed missing unit in ray tracing related tooltips and docs (case 1397491).
- Fixed errors spamming when in player mode due to ray tracing light cluster debug view (case 1390471).
- Fixed warning upon deleting APV data assets.
- Fixed an issue in the instance ID management for tesselation shaders.
- Fixed warning when an APV baking set is renamed.
- Fixed issue where scene list was not refreshed upon deleting an APV baking set.
- Fixed a null ref exception in Volume Explorer
- Fixed one frame flicker on hardware DRS - (case 1398085)
- Fixed using the wrong coordinate to compute the sampling direction for the screen space global illumination.
- Fixed an issue where forced sky update (like PBR sky amortized updated) would not update ambient probe.
- Fixed static lighting sky update when using an HDRI sky with a render texture in parameter.
- Fixed sky jittering when TAA is enabled.
- Fixed Normal Map assiignation when importing FBX Materials.
- Fixed issue with HDRI Sky and shadow filtering quality set to high.
- Fixed the default custom pass buffer format from R8G8B8A8_SNorm to R8G8B8A8_UNorm. Additionally, an option in the custom pass buffer format settings is available to use the old format.
- Fixed cached directional light shadows disappearing without reappearing when the going outside of the range of shadow validity.
- Fixed an issue where sometimes full screen debug would cause render graph errors.
- Fixed a nullref exception when creating a new scene while LightExplorer is open.
- Fixed issue that caused the uber post process to run even if nothing is to be done, leading to different results when disabling every post process manually vs disabling the whole post-processing pipeline.
- Fixed issue that placed an OnDemand shadow in the atlas before it was ever rendered.
- Fixed issue at edge of screen on some platforms when SSAO is on.
- Fixed reflection probe rendering order when visible in multiple cameras.
- Fixed performance penalty when hardware DRS was used between multiple views like editor or other gameviews (case 1354382)
- Fixed Show/Hide all Additional Properties
- Fixed errors about incorrect color spaces in the console when using the Wizzard to fix the project setup (case 1388222).
- Fixed custom pass name being cut when too long in the inspector.
- Fixed debug data for probes to not longer be cleared every time a cell is added/removed. This helps performance with streaming.
- Fixed APV loading data outside of the relevant area containing probes.
- Fixed the roughness value used for screen space reflections and ray traced reflections to match environment lighting (case 1390916).
- Fixed editor issue with the LiftGammaGain and ShadowsMidtonesHighlights volume components.
- Fixed using the wrong directional light data for clouds and the definition of current Sun when the shadow pass is culled (case 1399000).
- Fixed vertex color mode Add name whicgh was misleading, renamed to AddSubstract.
- Fixed screen space shadow when multiple lights cast shadows.
- Fixed issue with accumulation motion blur and depth of field when path tracing is enabled.
- Fixed issue with dynamic resolution and low res transparency sampling garbage outside of the render target.
- Fixed issue with raytraced shadows being visible alongside shadowmask.
- Fixed RTGI potentially reading from outside the half res pixels due to missing last pixel during the upscale pass (case 1400310).

## [14.0.0] - 2021-11-17

### Added
- Added FSR sharpness override to camera and pipeline asset.

### Fixed
- Fixed some XR devices: Pulling camera world space position from mainViewConstants instead of transform.
- Fixed Xbox Series X compilation issue with DoF shader
- Fixed references to reflection probes that wouldn't be cleared when unloading a scene. (case 1357459)
- Fixed issue with Stacklit raytrace reflection
- Fixed various issues with using SSR lighting with IBL fallback for Lit shader with clear coat(case 1380351)
- Fixed stackLit coat screen space reflection and raytrace reflection light hierarchy and IBL fallback
- Fixed compilation errors from Path Tracing on the PS5 build.
- Fixed custom shader GUI for material inspector.
- Fixed custom pass utils Blur and Copy functions in XR.
- Fixed the ray tracing acceleration structure build marker not being included in the ray tracing stats (case 1379383).
- Fixed missing information in the tooltip of affects smooth surfaces of the ray traced reflections denoiser (case 1376918).
- Fixed broken debug views when dynamic resolution was enabled (case 1365368).
- Fixed shader graph errors when disabling the bias on texture samplers.
- Fixed flickering / edge aliasing issue when DoF and TAAU or DLSS are enabled (case 1381858).
- Fixed options to trigger cached shadows updates on light transform changes.
- Fixed objects belonging to preview scenes being marked as dirty during migration (case 1367204).
- Fixed interpolation issue with wind orientation (case 1379841).
- Fixed range fields for depth of field (case 1376609).
- Fixed exception on DLSS when motion vectors are disabled (case # 1377986).
- Fixed decal performances when they use different material and the same draw order.
- Fixed alpha channel display in color picker in Local Volumetric Fog component (the alpha is not used for the fog) (case 1381267).
- Fixed Nans happening due to volumetric clouds when the pixel color is perfectly black (case 1379185).
- Fixed for screen space overlay rendered by camera when HDR is disabled.
- Fixed dirtiness handling in path tracing, when using multiple cameras at once (case 1376940).
- Fixed taa jitter for after post process materials (case 1380967).
- Fixed rasterized accumulation motion blur when DoF is enabled (case 1378497).
- Fixed light mode not available after switching a light to area "Disc" or "Tube" (case 1372588).
- Fixed CoC size computation when dynamic resolution is enabled
- Fixed shadow cascade transition not working properly with bias.
- Fixed broken rendering when duplicating a camera while the Rendering Debugger is opened.
- Fixed screen space shadow debug view not showing when no shadows is available.
- Fixed nullref from debug menu in release build (case 1381556).
- Fixed debug window reset.
- Fixed camera bridge action in release build (case 1367866).
- Fixed contact shadow disappearing when shadowmask is used and no non-static object is available.
- Fixed atmospheric scattering being incorrectly enabled when scene lighting is disabled.
- Fixed for changes of color curves not being applied immediately.
- Fixed edges and ghosting appearing on shadow matte due to the shadow being black outside the range of the light (case 1371441).
- Fixed the ray tracing fallbacks being broken since an Nvidia Driver Update.
- Fixed layer lit shader UI.
- Fixed a warning because of a null texture in the lens flare pass.
- Fixed a nullref when enabling raycount without ray tracing.
- Fixed error thrown when layered lit material has an invalid material type.
- Fixed HDRP build issues with DOTS_INSTANCING_ON shader variant.
- Fixed default value of "Distortion Blur" from 1 to 0 according to the doc.
- Fixed Transparent Depth Pre/Post pass by default for the built-in HDRP Hair shader graph.
- Fixed NullReferenceException when opening a Volume Component with a Diffusion Profile with any inspector.

### Changed
- Converted most TGA textures files to TIF to reduce the size of HDRP material samples.
- Changed sample scene in HDRP material samples: add shadow transparency (raster, ray-traced, path-traced).
- Support for encoded HDR cubemaps, configurable via the HDR Cubemap Encoding project setting.
- The rendering order of decals that have a similar draw order value was modified. The new order should be the reverse of the previous order.

## [13.1.2] - 2021-11-05

### Added
- Added minimal picking support for DOTS 1.0 (on parity with Hybrid Renderer V2)
- Implemented an initial version of the HDRP water system.

### Fixed
- Fixed compilation errors when using Elipse, Rectangle, Polygon, Checkerboard, RoundedPolygon, RoundedRectangle in a ray tracing shader graph (case 1377610).
- Fixed outdated documentation about supported GPUs for ray tracing (case 1375895).
- Fixed outdated documentation about recursie ray tracing effects support (case 1374904).
- Fixed Shadow Matte not appearing in ray tracing effects (case 1364005).
- Fixed Crash issue when adding an area light on its own.
- Fixed rendertarget ColorMask in Forward with virtual texturing and transparent motion vectors.
- Fixed light unit conversion after changing mid gray value.
- Fixed Focus distance in path traced depth of field now takes into account the focus mode setting (volume vs camera).
- Fixed stencil buffer resolve when MSAA is enabled so that OR operator is used instead of picking the last sample.
- Fixed Lens Flare visible when being behind a camera with Panini Projection on (case 1370214);

### Changed
- Optimizations for the physically based depth of field.
- Volumetric Lighting now uses an ambient probe computed directly on the GPU to avoid latency.

## [13.1.1] - 2021-10-04

### Added
- Added support for orthographic camera in path tracing.
- Added public API to edit materials from script at runtime.
- Added new functions that sample the custom buffer in custom passes (CustomPassSampleCustomColor and CustomPassLoadCustomColor) to handle the RTHandleScale automatically.
- Added new panels to Rendering Debugger Display Stats panel, displaying improved CPU/GPU frame timings and bottlenecks.
- Added API to edit diffusion profiles and set IES on lights.
- Added public API to reset path tracing accumulation, and check its status.
- Added support for SensorSDK's Lidar and camera models in path tracing.

### Fixed
- Fixed decal position when created from context menu. (case 1368987)
- Fixed the clouds not taking properly into account the fog when in distant mode and with a close far plane (case 1367993).
- Fixed overwriting of preview camera background color. [case 1357004](https://issuetracker.unity3d.com/product/unity/issues/guid/1361557/)
- Fixed selection of light types (point, area, directional) for path-traced Unlit shadow mattes.
- Fixed precision issues with the scene voxelization for APV, especially with geometry at the origin.
- Fixed the volumetric clouds debug view not taking into account the exposure and leading to Nans (case 1365054).
- Fixed area light cookie field to use the same style as the other cookie fields
- Fixed the dependency between transparent SSR and transparent depth prepass being implicit (case 1365915).
- Fixed depth pyramid being incorrect when having multiple cameras (scene view and gameview) and when hardware DRS was activated.
- Fixed the cloudlayer not using depth buffer.
- Fixed crossfade not working on the HD ST8 ShaderGraph [case 1369586](https://fogbugz.unity3d.com/f/cases/1369586/)
- Fixed range compression factor being clamped. (case 1365707)
- Fixed tooltip not showing on labels in ShaderGraphs (1358483).
- Fix API warnings in Matcap mode on Metal.
- Fix D3D validation layer errors w.r.t shadow textures when an atlas is not used.
- Fixed anchor position offset property for the Light Anchor component. (case 1362809)
- Fixed minor performance issues in SSGI (case 1367144).
- Fixed scaling issues with dynamic resolution and the CustomPassSampleCameraColor function.
- Fixed compatibility message not displayed correctly when switching platforms.
- Fixed support for interleaved tiling in path tracing.
- Fixed robustness issues with the stacklit material in path tracing (case 1373971).
- Fixed custom pass injection point not visible in the UI when using the Camera mode.
- Fixed film grain & dithering when using spatial upscaling methods for DRS.
- Fixed a regression that was introduced in the diffuse denoiser in a previous PR.
- Fixed pyramid blur being corrupted when hardware DRS was on (case # 1372245)
- Fixed sky override layer mask having no effect.
- Fixed a memory leak in the template tutorial (1374640).
- Fixed a build-time warning regarding light loop variants (case 1372256).
- Fixed an infinite import loop of materials when there is no HDMetaData generated by the ShaderGraph.
- Fixed issue with path traced shadows and layer masks (case 1375638).
- Fixed Z axis orientation when sampling 3D textures in local volumetric fog.
- Fixed geometry scale issue with the Eye Shader.
- Fixed motion vector buffer not accessible from custom passes in the BeforeTransparent, BeforePreRefraction and AfterDepthAndNormal injection points.
- Fixed the point distribution for the diffuse denoiser sometimes not being properly intialized.
- Fixed the bad blending between the sun and the clouds (case 1373282).
- Fixed and optimize distance shadowmask fade.

### Changed
- Use RayTracingAccelerationStructure.CullInstances to filter Renderers and populate the acceleration structure with ray tracing instances for improved CPU performance on the main thread.
- Changed the max distance for Light Anchors to avoid unstability with high values (case 1362802).
- PrepareLightsForGPU CPU Light loop performance improvement (40% to 70% faster), utilizing burst and optimized. Utilizing better sorting, distributing work in jobs and improving cache access of light data.
- In path tracing, camera ray misses now return a null value with Minimum Depth > 1.
- HD's SpeedTree 8 upgrader now sets up CullModeForward as well.
- Restructured data under Display Stats panel to use column layout.
- Added controls for the users to manually feed the ray tracing acceleration structure that should be used for a given camera (case 1370678).
- Depth of Field is now disabled in orthographic cameras - it was using the hidden perspective settings (case 1372582).
- Modified HDRP to use common FSR logic from SRP core.
- Optimized FSR by merging the RCAS logic into the FinalPass shader.
- Integrate a help box to inform users of the potential dependency to directional lights when baking.
- Changed default numbder of physically based sky bounce from 8 to 3
- Shader Variant Log Level moved from Miscellaneous section to Shader Stripping section on the HDRP Global Settings.

## [13.1.0] - 2021-09-24

### Added
- Added a SG node to get the main directional light direction.
- Added support for orthographic camera in path tracing.
- Added public API to edit materials from script at runtime.
- Added new configuration ShderOptions.FPTLMaxLightCount in ShaderConfig.cs for maximum light count per fine pruned tile.
- Added support for orthographic camera in path tracing.
- Support for "Always Draw Dynamic" option for directional light cached shadows.

### Changed
- MaterialReimporter.ReimportAllMaterials and MaterialReimporter.ReimportAllHDShaderGraphs now batch the asset database changes to improve performance.

### Fixed
- Fixed the volume not being assigned on some scene templates.
- Fixed corruption in player with lightmap uv when Optimize Mesh Data is enabled [1357902]
- Fixed a warning to Rendering Debugger Runtime UI when debug shaders are stripped.
- Fixed Probe volume debug exposure compensation to match the Lighting debug one.
- Fixed lens flare occlusion issues with TAA. (1365098)
- Fixed misleading text and improving the eye scene material samples. (case 1368665)
- Fixed missing DisallowMultipleComponent annotations in HDAdditionalReflectionData and HDAdditionalLightData (case 1365879).
- Fixed ambient occlusion strenght incorrectly using GTAOMultiBounce
- Maximum light count per fine prunned tile (opaque deferred) is now 63 instead of 23.

## [13.0.0] - 2021-09-01

### Added

- Added support for HDR output devices.
- Added option to use full ACES tonemap instead of the approximation.

### Fixed

- Fixed impossibility to release the cursor in the template.
- Fixed assert failure when enabling the probe volume system for the first time.
- Significantly improved performance of APV probe debug.
- Removed DLSS keyword in settings search when NVIDIA package is not installed. (case 1358409)
- Fixed light anchor min distance value + properties not working with prefabs (case 1345509).
- Fixed specular occlusion sharpness and over darkening at grazing angles.
- Fixed edge bleeding when rendering volumetric clouds.
- Fixed the performance of the volumetric clouds in non-local mode when large occluders are on screen.
- Fixed a regression that broke punctual and directional raytraced shadows temporal denoiser (case 1360132).
- Fixed regression in the ambient probe intensity for volumetric clouds.
- Fixed the sun leaking from behind fully opaque clouds.
- Fixed artifacts in volumetric cloud shadows.
- Fixed the missing parameter to control the sun light dimmer (case 1364152).
- Fixed regression in the clouds presets.
- Fixed the way we are handling emissive for SSGI/RTGI/Mixed and APV and remove ForceForwardEmissive code
- Fixed EmissiveLighting Debug Light mode not managing correctly emissive for unlit
- Fixed remove of the Additional Light Data when removing the Light Component.
- Fixed remove of the Additional Camera Data when removing the Camera Component.
- Fixed remove of the Additional Light Data when removing the Light Component.
- Fixed remove of the Additional Camera Data when removing the Camera Component.
- Fixed a null ref exception when no opaque objects are rendered.
- Fixed issue with depth slope scale depth bias when a material uses depth offset.
- Fixed shadow sampling artifact when using the spot light shadow option 'custom spot angle'
- Fixed issue with fading in SSR applying fade factor twice, resulting in darkening of the image in the transition areas.
- Fixed path traced subsurface scattering for transmissive surfaces (case 1329403)
- Fixed missing context menu for “Post Anti-Aliasing” in Camera (1357283)
- Fixed error when disabling opaque objects on a camera with MSAA.
- Fixed double camera preview.
- Fixed the volumetric clouds cloud map not being centered over the world origin (case 1364465).
- Fixed the emissive being overriden by ray traced sub-surface scattering (case 1364456).
- Fixed support of directional light coloring from physical sky in path tracing.
- Fixed disabled menu item for volume additional properties.
- Fixed Shader advanced options for lit shaders.
- Fixed Dof, would sometimes get corrupted when DLSS was on caused by TAA logic accidentally being on for DOF (1357722)
- Fixed shadowmask editable when not supported.
- Fixed sorting for mesh decals.
- Fixed a warning when enabling tile/cluster debug.
- Fix recursive rendering transmittance over the sky (case 1323945).
- Fixed specular anti aliasing for layeredlit shader.
- Fixed lens flare occlusion issues with transparent depth. It had the wrong depth bound (1365098)
- Fixed double contribution from the clear coat when having SSR or RTR on the Lit and StackLit shaders (case 1352424).
- Fixed texture fields for volume parameters accepting textures with wrong dimensions.
- Fixed Realtime lightmap not working correctly in player with various lit shader (case 1360021)
- Fixed unexpectedly strong contribution from directional lights in path-traced volumetric scattering (case 1304688).
- Fixed memory leak with XR combined occlusion meshes (case 1366173).
- Fixed diffusion profile being reset to default on SpeedTree8 materials with subsurface scattering enabled during import.
- Fixed support for light/shadow dimmers (volumetric or not) in path tracing.

### Changed
- Visual Environment ambient mode is now Dynamic by default.
- Surface ReflectionTypeLoadExceptions in HDUtils.GetRenderPipelineMaterialList(). Without surfacing these exceptions, developers cannot act on any underlying reflection errors in the HDRP assembly.
- Improved the DynamicArray class by adding several utility APIs.
- Moved AMD FidelityFX shaders to core
- Improved sampling of overlapping point/area lights in path-traced volumetric scattering (case 1358777).
- Path-traced volumetric scattering now takes fog color into account, adding scattered contribution on top of the non-scattered result (cases 1346105, 1358783).
- Fixed minor readability issues in the ray tracing code.
- Optimized color grading LUT building.
- Made ACEScg the default color space for color grading.


## [12.0.0] - 2021-01-11

### Added
- Added support for XboxSeries platform.
- Added pivot point manipulation for Decals (inspector and edit mode).
- Added UV manipulation for Decals (edit mode).
- Added color and intensity customization for Decals.
- Added a history rejection criterion based on if the pixel was moving in world space (case 1302392).
- Added the default quality settings to the HDRP asset for RTAO, RTR and RTGI (case 1304370).
- Added TargetMidGrayParameterDrawer
- Added an option to have double sided GI be controlled separately from material double-sided option.
- Added new AOV APIs for overriding the internal rendering format, and for outputing the world space position.
- Added browsing of the documentation of Compositor Window
- Added a complete solution for volumetric clouds for HDRP including a cloud map generation tool.
- Added a Force Forward Emissive option for Lit Material that forces the Emissive contribution to render in a separate forward pass when the Lit Material is in Deferred Lit shader Mode.
- Added new API in CachedShadowManager
- Added an additional check in the "check scene for ray tracing" (case 1314963).
- Added shader graph unit test for IsFrontFace node
- API to allow OnDemand shadows to not render upon placement in the Cached Shadow Atlas.
- Exposed update upon light movement for directional light shadows in UI.
- Added a setting in the HDRP asset to change the Density Volume mask resolution of being locked at 32x32x32 (HDRP Asset > Lighting > Volumetrics > Max Density Volume Size).
- Added a Falloff Mode (Linear or Exponential) in the Density Volume for volume blending with Blend Distance.
- Added support for screen space shadows (directional and point, no area) for shadow matte unlit shader graph.
- Added support for volumetric clouds in planar reflections.
- Added deferred shading debug visualization
- Added a new control slider on RTR and RTGI to force the LOD Bias on both effects.
- Added missing documentation for volumetric clouds.
- Added the support of interpolators for SV_POSITION in shader graph.
- Added a "Conservative" mode for shader graph depth offset.
- Added an error message when trying to use disk lights with realtime GI (case 1317808).
- Added support for multi volumetric cloud shadows.
- Added a Scale Mode setting for Decals.
- Added LTC Fitting tools for all BRDFs that HDRP supports.
- Added Area Light support for Hair and Fabric master nodes.
- Added a fallback for the ray traced directional shadow in case of a transmission (case 1307870).
- Added support for Fabric material in Path Tracing.
- Added help URL for volumetric clouds override.
- Added Global settings check in Wizard
- Added localization on Wizard window
- Added an info box for micro shadow editor (case 1322830).
- Added support for alpha channel in FXAA (case 1323941).
- Added Speed Tree 8 shader graph as default Speed Tree 8 shader for HDRP.
- Added the multicompile for dynamic lightmaps to support enlighten in ray tracing (case 1318927).
- Added support for lighting full screen debug mode in automated tests.
- Added a way for fitting a probe volume around either the scene contents or a selection.
- Added support for mip bias override on texture samplers through the HDAdditionalCameraData component.
- Added Lens Flare Samples
- Added new checkbox to enable mip bias in the Dynamic Resolution HDRP quality settings. This allows dynamic resolution scaling applying a bias on the frame to improve on texture sampling detail.
- Added a toggle to render the volumetric clouds locally or in the skybox.
- Added the ability to control focus distance either from the physical camera properties or the volume.
- Added the ability to animate many physical camera properties with Timeline.
- Added a mixed RayMarching/RayTracing mode for RTReflections and RTGI.
- Added path tracing support for stacklit material.
- Added path tracing support for AxF material.
- Added support for surface gradient based normal blending for decals.
- Added support for tessellation for all master node in shader graph.
- Added ValidateMaterial callbacks to ShaderGUI.
- Added support for internal plugin materials and HDSubTarget with their versioning system.
- Added a slider that controls how much the volumetric clouds erosion value affects the ambient occlusion term.
- Added three animation curves to control the density, erosion, and ambient occlusion in the custom submode of the simple controls.
- Added support for the camera bridge in the graphics compositor
- Added slides to control the shape noise offset.
- Added two toggles to control occluder rejection and receiver rejection for the ray traced ambient occlusion (case 1330168).
- Added the receiver motion rejection toggle to RTGI (case 1330168).
- Added info box when low resolution transparency is selected, but its not enabled in the HDRP settings. This will help new users find the correct knob in the HDRP Asset.
- Added support for Unlit shadow mattes in Path Tracing (case 1335487).
- Added a shortcut to HDRP Wizard documentation.
- Added support of motion vector buffer in custom postprocess
- Added tooltips for content inside the Rendering Debugger window.
- Added support for reflection probes as a fallback for ray traced reflections (case 1338644).
- Added a minimum motion vector length to the motion vector debug view.
- Added a better support for LODs in the ray tracing acceleration structure.
- Added a property on the HDRP asset to allow users to avoid ray tracing effects running at too low percentages (case 1342588).
- Added dependency to mathematics and burst, HDRP now will utilize this to improve on CPU cost. First implementation of burstified decal projector is here.
- Added warning for when a light is not fitting in the cached shadow atlas and added option to set maximum resolution that would fit.
- Added a custom post process injection point AfterPostProcessBlurs executing after depth of field and motion blur.
- Added the support of volumetric clouds for baked and realtime reflection probes.
- Added a property to control the fallback of the last bounce of a RTGI, RTR, RR ray to keep a previously existing side effect on user demand (case 1350590).
- Added a parameter to control the vertical shape offset of the volumetric clouds (case 1358528).
- Added an option to render screen space global illumination in half resolution to achieve real-time compatible performance in high resolutions (case 1353727).
- Added a built-in custom pass to draw object IDs.
- Added an example in the documentation that shows how to use the accumulation API for high quality antialiasing (supersampling).

### Fixed
- Fixed Intensity Multiplier not affecting realtime global illumination.
- Fixed an exception when opening the color picker in the material UI (case 1307143).
- Fixed lights shadow frustum near and far planes.
- The HDRP Wizard is only opened when a SRP in use is of type HDRenderPipeline.
- Fixed various issues with non-temporal SSAO and rendergraph.
- Fixed white flashes on camera cuts on volumetric fog.
- Fixed light layer issue when performing editing on multiple lights.
- Fixed an issue where selection in a debug panel would reset when cycling through enum items.
- Fixed material keywords with fbx importer.
- Fixed lightmaps not working properly with shader graphs in ray traced reflections (case 1305335).
- Fixed skybox for ortho cameras.
- Fixed crash on SubSurfaceScattering Editor when the selected pipeline is not HDRP
- Fixed model import by adding additional data if needed.
- Fix screen being over-exposed when changing very different skies.
- Fixed pixelated appearance of Contrast Adaptive Sharpen upscaler and several other issues when Hardware DRS is on
- VFX: Debug material view were rendering pink for albedo. (case 1290752)
- VFX: Debug material view incorrect depth test. (case 1293291)
- VFX: Fixed LPPV with lit particles in deferred (case 1293608)
- Fixed incorrect debug wireframe overlay on tessellated geometry (using littessellation), caused by the picking pass using an incorrect camera matrix.
- Fixed nullref in layered lit shader editor.
- Fix issue with Depth of Field CoC debug view.
- Fixed an issue where first frame of SSAO could exhibit ghosting artefacts.
- Fixed an issue with the mipmap generation internal format after rendering format change.
- Fixed multiple any hit occuring on transparent objects (case 1294927).
- Cleanup Shader UI.
- Indentation of the HDRenderPipelineAsset inspector UI for quality
- Spacing on LayerListMaterialUIBlock
- Generating a GUIContent with an Icon instead of making MaterialHeaderScopes drawing a Rect every time
- Fixed sub-shadow rendering for cached shadow maps.
- Fixed PCSS filtering issues with cached shadow maps.
- Fixed performance issue with ShaderGraph and Alpha Test
- Fixed error when increasing the maximum planar reflection limit (case 1306530).
- Fixed alpha output in debug view and AOVs when using shadow matte (case 1311830).
- Fixed an issue with transparent meshes writing their depths and recursive rendering (case 1314409).
- Fixed issue with compositor custom pass hooks added/removed repeatedly (case 1315971).
- Fixed: SSR with transparent (case 1311088)
- Fixed decals in material debug display.
- Fixed Force RGBA16 when scene filtering is active (case 1228736)
- Fix crash on VolumeComponentWithQualityEditor when the current Pipeline is not HDRP
- Fixed WouldFitInAtlas that would previously return wrong results if any one face of a point light would fit (it used to return true even though the light in entirety wouldn't fit).
- Fixed issue with NaNs in Volumetric Clouds on some platforms.
- Fixed update upon light movement for directional light rotation.
- Fixed issue that caused a rebake of Probe Volume Data to see effect of changed normal bias.
- Fixed loss of persistency of ratio between pivot position and size when sliding by 0 in DecalProjector inspector (case 1308338)
- Fixed nullref when adding a volume component in a Volume profile asset (case 1317156).
- Fixed decal normal for double sided materials (case 1312065).
- Fixed multiple HDRP Frame Settings panel issues: missing "Refraction" Frame Setting. Fixing ordering of Rough Distortion, it should now be under the Distortion setting.
- Fixed Rough Distortion frame setting not greyed out when Distortion is disabled in HDRP Asset
- Fixed issue with automatic exposure settings not updating scene view.
- Fixed issue with velocity rejection in post-DoF TAA. Fixing this reduces ghosting (case 1304381).
- Fixed missing option to use POM on emissive for tessellated shaders.
- Fixed an issue in the planar reflection probe convolution.
- Fixed an issue with debug overriding emissive material color for deferred path (case 1313123).
- Fixed a limit case when the camera is exactly at the lower cloud level (case 1316988).
- Fixed the various history buffers being discarded when the fog was enabled/disabled (case 1316072).
- Fixed resize IES when already baked in the Atlas 1299233
- Fixed ability to override AlphaToMask FrameSetting while camera in deferred lit shader mode
- Fixed issue with physically-based DoF computation and transparent materials with depth-writes ON.
- Fixed issue of accessing default frame setting stored in current HDRPAsset instead fo the default HDRPAsset
- Fixed SSGI frame setting not greyed out while SSGI is disabled in HDRP Asset
- Fixed ability to override AlphaToMask FrameSetting while camera in deferred lit shader mode
- Fixed Missing lighting quality settings for SSGI (case 1312067).
- Fixed HDRP material being constantly dirty.
- Fixed wizard checking FrameSettings not in HDRP Global Settings
- Fixed error when opening the default composition graph in the Graphics Compositor (case 1318933).
- Fixed gizmo rendering when wireframe mode is selected.
- Fixed issue in path tracing, where objects would cast shadows even if not present in the path traced layers (case 1318857).
- Fixed SRP batcher not compatible with Decal (case 1311586)
- Fixed wrong color buffer being bound to pre refraction custom passes.
- Fixed issue in Probe Reference Volume authoring component triggering an asset reload on all operations.
- Fixed grey screen on playstation platform when histogram exposure is enabled but the curve mapping is not used.
- Fixed HDRPAsset loosing its reference to the ray tracing resources when clicking on a different quality level that doesn't have ray tracing (case 1320304).
- Fixed SRP batcher not compatible with Decal (case 1311586).
- Fixed error message when having MSAA and Screen Space Shadows (case 1318698).
- Fixed Nans happening when the history render target is bigger than the current viewport (case 1321139).
- Fixed Tube and Disc lights mode selection (case 1317776)
- Fixed preview camera updating the skybox material triggering GI baking (case 1314361/1314373).
- The default LookDev volume profile is now copied and referenced in the Asset folder instead of the package folder.
- Fixed SSS on console platforms.
- Assets going through the migration system are now dirtied.
- Fixed warning fixed on ShadowLoop include (HDRISky and Unlit+ShadowMatte)
- Fixed SSR Precision for 4K Screens
- Fixed issue with gbuffer debug view when virtual texturing is enabled.
- Fixed volumetric fog noise due to sun light leaking (case 1319005)
- Fixed an issue with Decal normal blending producing NaNs.
- Fixed issue in wizard when resource folder don't exist
- Fixed issue with Decal projector edge on Metal (case 1286074)
- Fixed Exposure Frame Settings control issues on Planar reflection probes (case 1312153). Dynamic reflections now keep their own exposure relative to their parent camera.
- Fixed multicamera rendering for Dynamic Resolution Scaling using dx12 hardware mode. Using a planar reflection probe (another render camera) should be safe.
- Fixed Render Graph Debug UI not refreshing correctly in the Rendering Debugger.
- Fixed SSS materials in planar reflections (case 1319027).
- Fixed Decal's pivot edit mode 2D slider gizmo not supporting multi-edition
- Fixed missing Update in Wizard's DXR Documentation
- Fixed issue were the final image is inverted in the Y axis. Occurred only on final Player (non-dev for any platform) that use Dynamic Resolution Scaling with Contrast Adaptive Sharpening filter.
- Fixed a bug with Reflection Probe baking would result in an incorrect baking reusing other's Reflection Probe baking
- Fixed volumetric fog being visually chopped or missing when using hardware Dynamic Resolution Scaling.
- Fixed generation of the packed depth pyramid when hardware Dynamic Resolution Scaling is enabled.
- Fixed issue were the final image is inverted in the Y axis. Occurred only on final Player (non-dev for any platform) that use Dynamic Resolution Scaling with Contrast Adaptive Sharpening filter.
- Fixed a bug with Reflection Probe baking would result in an incorrect baking reusing other's Reflection Probe baking
- Fixed Decal's UV edit mode with negative UV
- Fixed issue with the color space of AOVs (case 1324759)
- Fixed issue with history buffers when using multiple AOVs (case 1323684).
- Fixed camera preview with multi selection (case 1324126).
- Fix potential NaN on apply distortion pass.
- Fixed the camera controller in the template with the old input system (case 1326816).
- Fixed broken Lanczos filter artifacts on ps4, caused by a very aggressive epsilon (case 1328904)
- Fixed global Settings ignore the path set via Fix All in HDRP wizard (case 1327978)
- Fixed issue with an assert getting triggered with OnDemand shadows.
- Fixed GBuffer clear option in FrameSettings not working
- Fixed usage of Panini Projection with floating point HDRP and Post Processing color buffers.
- Fixed a NaN generating in Area light code.
- Fixed CustomPassUtils scaling issues when used with RTHandles allocated from a RenderTexture.
- Fixed ResourceReloader that was not call anymore at pipeline construction
- Fixed undo of some properties on light editor.
- Fixed an issue where auto baking of ambient and reflection probe done for builtin renderer would cause wrong baking in HDRP.
- Fixed some reference to old frame settings names in HDRP Wizard.
- Fixed issue with constant buffer being stomped on when async tasks run concurrently to shadows.
- Fixed migration step overriden by data copy when creating a HDRenderPipelineGlobalSettings from a HDRPAsset.
- Fixed null reference exception in Raytracing SSS volume component.
- Fixed artifact appearing when diffuse and specular normal differ too much for eye shader with area lights
- Fixed LightCluster debug view for ray tracing.
- Fixed issue with RAS build fail when LOD was missing a renderer
- Fixed an issue where sometime a docked lookdev could be rendered at zero size and break.
- Fixed an issue where runtime debug window UI would leak game objects.
- Fixed NaNs when denoising pixels where the dot product between normal and view direction is near zero (case 1329624).
- Fixed ray traced reflections that were too dark for unlit materials. Reflections are now more consistent with the material emissiveness.
- Fixed pyramid color being incorrect when hardware dynamic resolution is enabled.
- Fixed SSR Accumulation with Offset with Viewport Rect Offset on Camera
- Fixed material Emission properties not begin animated when recording an animation (case 1328108).
- Fixed fog precision in some camera positions (case 1329603).
- Fixed contact shadows tile coordinates calculations.
- Fixed issue with history buffer allocation for AOVs when the request does not come in first frame.
- Fix Clouds on Metal or platforms that don't support RW in same shader of R11G11B10 textures.
- Fixed blocky looking bloom when dynamic resolution scaling was used.
- Fixed normals provided in object space or world space, when using double sided materials.
- Fixed multi cameras using cloud layers shadows.
- Fixed HDAdditionalLightData's CopyTo and HDAdditionalCameraData's CopyTo missing copy.
- Fixed issue with velocity rejection when using physically-based DoF.
- Fixed HDRP's ShaderGraphVersion migration management which was broken.
- Fixed missing API documentation for LTC area light code.
- Fixed diffusion profile breaking after upgrading HDRP (case 1337892).
- Fixed undo on light anchor.
- Fixed invalid cast exception on HDProbe.
- Fixed some depth comparison instabilities with volumetric clouds.
- Fixed AxF debug output in certain configurations (case 1333780).
- Fixed white flash when camera is reset and SSR Accumulation mode is on.
- Fixed an issue with TAA causing objects not to render at extremely high far flip plane values.
- Fixed a memory leak related to not disposing of the RTAS at the end HDRP's lifecycle.
- Fixed overdraw in custom pass utils blur and Copy functions (case 1333648);
- Fixed invalid pass index 1 in DrawProcedural error.
- Fixed a compilation issue for AxF carpaints on Vulkan (case 1314040).
- Fixed issue with hierarchy object filtering.
- Fixed a lack of syncronization between the camera and the planar camera for volumetric cloud animation data.
- Fixed for wrong cached area light initialization.
- Fixed unexpected rendering of 2D cookies when switching from Spot to Point light type (case 1333947).
- Fixed the fallback to custom went changing a quality settings not workings properly (case 1338657).
- Fixed ray tracing with XR and camera relative rendering (case 1336608).
- Fixed the ray traced sub subsurface scattering debug mode not displaying only the RTSSS Data (case 1332904).
- Fixed for discrepancies in intensity and saturation between screen space refraction and probe refraction.
- Fixed a divide-by-zero warning for anisotropic shaders (Fabric, Lit).
- Fixed VfX lit particle AOV output color space.
- Fixed path traced transparent unlit material (case 1335500).
- Fixed support of Distortion with MSAA
- Fixed contact shadow debug views not displaying correctly upon resizing of view.
- Fixed an error when deleting the 3D Texture mask of a local volumetric fog volume (case 1339330).
- Fixed some aliasing issues with the volumetric clouds.
- Fixed reflection probes being injected into the ray tracing light cluster even if not baked (case 1329083).
- Fixed the double sided option moving when toggling it in the material UI (case 1328877).
- Fixed incorrect RTHandle scale in DoF when TAA is enabled.
- Fixed an incompatibility between MSAA and Volumetric Clouds.
- Fixed volumetric fog in planar reflections.
- Fixed error with motion blur and small render targets.
- Fixed issue with on-demand directional shadow maps looking broken when a reflection probe is updated at the same time.
- Fixed cropping issue with the compositor camera bridge (case 1340549).
- Fixed an issue with normal management for recursive rendering (case 1324082).
- Fixed aliasing artifacts that are related to numerical imprecisions of the light rays in the volumetric clouds (case 1340731).
- Fixed exposure issues with volumetric clouds on planar reflection
- Fixed bad feedback loop occuring when auto exposure adaptation time was too small.
- Fixed an issue where enabling GPU Instancing on a ShaderGraph Material would cause compile failures [1338695].
- Fixed the transparent cutoff not working properly in semi-transparent and color shadows (case 1340234).
- Fixed object outline flickering with TAA.
- Fixed issue with sky settings being ignored when using the recorder and path tracing (case 1340507).
- Fixed some resolution aliasing for physically based depth of field (case 1340551).
- Fixed an issue with resolution dependence for physically based depth of field.
- Fixed sceneview debug mode rendering (case 1211436)
- Fixed Pixel Displacement that could be set on tessellation shader while it's not supported.
- Fixed an issue where disabled reflection probes were still sent into the the ray tracing light cluster.
- Fixed nullref when enabling fullscreen passthrough in HDRP Camera.
- Fixed tessellation displacement with planar mapping
- Fixed the shader graph files that was still dirty after the first save (case 1342039).
- Fixed cases in which object and camera motion vectors would cancel out, but didn't.
- Fixed HDRP material upgrade failing when there is a texture inside the builtin resources assigned in the material (case 1339865).
- Fixed custom pass volume not executed in scene view because of the volume culling mask.
- When the HDProjectSettings was being loaded on some cases the load of the ScriptableObject was calling the method `Reset` from the HDProjectSettings, simply rename the method to avoid an error log from the loading.
- Fixed remapping of depth pyramid debug view
- Fixed an issue with asymmetric projection matrices and fog / pathtracing. (case 1330290).
- Fixed rounding issue when accessing the color buffer in the DoF shader.
- HD Global Settings can now be unassigned in the Graphics tab if HDRP is not the active pipeline(case 1343570).
- Fix diffusion profile displayed in the inspector.
- HDRP Wizard can still be opened from Windows > Rendering, if the project is not using a Render Pipeline.
- Fixed override camera rendering custom pass API aspect ratio issue when rendering to a render texture.
- Fixed the incorrect value written to the VT feedback buffer when VT is not used.
- Fixed support for ray binning for ray tracing in XR (case 1346374).
- Fixed exposure not being properly handled in ray tracing performance (RTGI and RTR, case 1346383).
- Fixed the RTAO debug view being broken.
- Fixed an issue that made camera motion vectors unavailable in custom passes.
- Fixed the possibility to hide custom pass from the create menu with the HideInInspector attribute.
- Fixed support of multi-editing on custom pass volumes.
- Fixed possible QNANS during first frame of SSGI, caused by uninitialized first frame data.
- Fixed various SSGI issues (case 1340851, case 1339297, case 1327919).
- Prevent user from spamming and corrupting installation of nvidia package.
- Fixed an issue with surface gradient based normal blending for decals (volume gradients weren't converted to SG before resolving in some cases).
- Fixed distortion when resizing the graphics compositor window in builds (case 1328968).
- Fixed custom pass workflow for single camera effects.
- Fixed gbuffer depth debug mode for materials not rendered during the prepass.
- Fixed Vertex Color Mode documentation for layered lit shader.
- Fixed wobbling/tearing-like artifacts with SSAO.
- Fixed white flash with SSR when resetting camera history (case 1335263).
- Fixed VFX flag "Exclude From TAA" not working for some particle types.
- Spot Light radius is not changed when editing the inner or outer angle of a multi selection (case 1345264)
- Fixed Dof and MSAA. DoF is now using the min depth of the per-pixel MSAA samples when MSAA is enabled. This removes 1-pixel ringing from in focus objects (case 1347291).
- Fixed parameter ranges in HDRP Asset settings.
- Fixed CPU performance of decal projectors, by a factor of %100 (walltime) on HDRP PS4, by burstifying decal projectors CPU processing.
- Only display HDRP Camera Preview if HDRP is the active pipeline (case 1350767).
- Prevent any unwanted light sync when not in HDRP (case 1217575)
- Fixed missing global wind parameters in the visual environment.
- Fixed fabric IBL (Charlie) pre-convolution performance and accuracy (uses 1000x less samples and is closer match with the ground truth)
- Fixed screen-space shadows with XR single-pass and camera relative rendering (1348260).
- Fixed ghosting issues if the exposure changed too much (RTGI).
- Fixed failures on platforms that do not support ray tracing due to an engine behavior change.
- Fixed infinite propagation of nans for RTGI and SSGI (case 1349738).
- Fixed access to main directional light from script.
- Fixed an issue with reflection probe normalization via APV when no probes are in scene.
- Fixed Volumetric Clouds not updated when using RenderTexture as input for cloud maps.
- Fixed custom post process name not displayed correctly in GPU markers.
- Fixed objects disappearing from Lookdev window when entering playmode (case 1309368).
- Fixed rendering of objects just after the TAA pass (before post process injection point).
- Fixed tiled artifacts in refraction at borders between two reflection probes.
- Fixed the FreeCamera and SimpleCameraController mouse rotation unusable at low framerate (case 1340344).
- Fixed warning "Releasing render texture that is set to be RenderTexture.active!" on pipeline disposal / hdrp live editing.
- Fixed a null ref exception when adding a new environment to the Look Dev library.
- Fixed a nullref in volume system after deleting a volume object (case 1348374).
- Fixed the APV UI loosing focus when the helpbox about baking appears in the probe volume.
- Fixed enabling a lensflare in playmode.
- Fixed white flashes when history is reset due to changes on type of upsampler.
- Fixed misc TAA issue: Slightly improved TAA flickering, Reduced ringing of TAA sharpening, tweak TAA High quality central color filtering.
- Fixed TAA upsampling algorithm, now work properly
- Fixed custom post process template not working with Blit method.
- Fixed support for instanced motion vector rendering
- Fixed an issue that made Custom Pass buffers inaccessible in ShaderGraph.
- Fixed some of the extreme ghosting in DLSS by using a bit mask to bias the color of particles. VFX tagged as Exclude from TAA will be on this pass.
- Fixed banding in the volumetric clouds (case 1353672).
- Fixed CustomPassUtils.Copy function not working on depth buffers.
- Fixed a nullref when binding a RTHandle allocated from a RenderTextureIdentifier with CoreUtils.SetRenderTarget.
- Fixed off by 1 error when calculating the depth pyramid texture size when DRS is on.
- Fixed CPU performance on DLSS, avoiding to recreate state whenever a target can fall into the safe min/max resolution specified by the system.
- Fixed TAA upsampling algorithm, now work properly.
- Fixed for allowing to change dynamic resolution upscale filter via script.
- Fixed update order in Graphics Compositor causing jumpy camera updates (case 1345566).
- Fixed material inspector that allowed setting intensity to an infinite value.
- Fixed issue when switching between non-persistent cameras when path tarcing is enabled (case 1337843).
- Fixed issue with the LayerMaskParameter class storing an erroneous mask value (case 1345515).
- Fixed issue with vertex color defaulting to 0.0 when not defined, in ray/path tracing (case 1348821).
- Fix issue with a compute dispatch being with 0 threads on extremely small resolutions.
- Fixed an issue with volumetric clouds on vulkan (case 1354802).
- Fixed volume interpolation issue with ScalableSettingLevelParameter.
- Fix issue with change in lens model (perfect or imperfect) wouldn't be taken into account unless the HDRP asset was rebuilt.
- Fixed custom pass delete operation (case 1354871).
- Fixed viewport size when TAA is executed after dynamic res upscale (case 1348541).
- Fixed the camera near plane not being taken into account when rendering the clouds (case 1353548).
- Fixed controls for clouds fade in (case 1353548).
- Reduced the number shader variants for the volumetric clouds.
- Fixed motion vector for custom meshes loaded from compute buffer in shader graph (like Hair)
- Fixed incorrect light list indexing when TAA is enabled (case 1352444).
- Fixed Additional Velocity for Alembic not taking correctly into account vertex animation
- Fixed wrong LUT initialization in Wireframe mode.
- Support undo of HDRP Global Settings asset assignation (case 13429870).
- Fixed an inconsistency between perf mode and quality mode for sky lighting (case 1350590).
- Fixed an inconsistency between perf mode and quality mode for material simplification in RTGI (case 1350590).
- Fixed an issue that clamped the volumetric clouds offset value (case 1357318).
- Fixed the volumetric clouds having no control over the vertical wind (case 1354920).
- Fixed the fallback sun for volumetric clouds having a non null intensity (case 1353955).
- Removed unsupported fields from Presets of Light, Camera, and Reflection Probes (case 1335979).
- Added a new property to control the ghosting reduction for volumetric clouds (case 1357702).
- Fixed the earth curvature not being properly taken into account when evaluating the sun attenuation (case 1357927).
- Reduced the volumetric clouds pattern repetition frequency (case 1358717).
- Fixed the clouds missing in the ambient probe and in the static and dynamic sky.
- Fixed lens flare not rendering correctly with TAAU or DLSS.
- Fixed case where the SceneView don't refresh when using LightExplorer with a running and Paused game (1354129)
- Fixed wrong ordering in FrameSettings (Normalize Reflection Probes)
- Fixed ThreadMapDetail to saturate AO & smoothness strength inputs to prevent out-of-bounds values set by users (1357740)
- Allow negative wind speed parameter.
- Fixed custom pass custom buffer not bound after being created inside a custom pass.
- Fixed silhouette issue with emissive decals
- Fixed the LensFlare flicker with TAA on SceneView (case 1356734).

### Changed
- Changed Window/Render Pipeline/HD Render Pipeline Wizard to Window/Rendering/HDRP Wizard
- Removed the material pass probe volumes evaluation mode.
- Changed GameObject/Rendering/Density Volume to GameObject/Rendering/Local Volumetric Fog
- Changed GameObject/Volume/Sky and Fog Volume to GameObject/Volume/Sky and Fog Global Volume
- Move the Decal Gizmo Color initialization to preferences
- Unifying the history validation pass so that it is only done once for the whole frame and not per effect.
- Moved Edit/Render Pipeline/HD Render Pipeline/Render Selected Camera to log Exr to Edit/Rendering/Render Selected HDRP Camera to log Exr
- Moved Edit/Render Pipeline/HD Render Pipeline/Export Sky to Image to Edit/Rendering/Export HDRP Sky to Image
- Moved Edit/Render Pipeline/HD Render Pipeline/Check Scene Content for Ray Tracing to Edit/Rendering/Check Scene Content for HDRP Ray Tracing
- Moved Edit/Render Pipeline/HD Render Pipeline/Upgrade from Builtin pipeline/Upgrade Project Materials to High Definition Materials to Edit/Rendering/Materials/Convert All Built-in Materials to HDRP"
- Moved Edit/Render Pipeline/HD Render Pipeline/Upgrade from Builtin pipeline/Upgrade Selected Materials to High Definition Materials to Edit/Rendering/Materials/Convert Selected Built-in Materials to HDRP
- Moved Edit/Render Pipeline/HD Render Pipeline/Upgrade from Builtin pipeline/Upgrade Scene Terrains to High Definition Terrains to Edit/Rendering/Materials/Convert Scene Terrains to HDRP Terrains
- Changed the Channel Mixer Volume Component UI.Showing all the channels.
- Updated the tooltip for the Decal Angle Fade property (requires to enable Decal Layers in both HDRP asset and Frame settings) (case 1308048).
- The RTAO's history is now discarded if the occlusion caster was moving (case 1303418).
- Change Asset/Create/Shader/HD Render Pipeline/Decal Shader Graph to Asset/Create/Shader Graph/HDRP/Decal Shader Graph
- Change Asset/Create/Shader/HD Render Pipeline/Eye Shader Graph to Asset/Create/Shader Graph/HDRP/Eye Shader Graph
- Change Asset/Create/Shader/HD Render Pipeline/Fabric Shader Graph to Asset/Create/Shader Graph/HDRP/Decal Fabric Shader Graph
- Change Asset/Create/Shader/HD Render Pipeline/Eye Shader Graph to Asset/Create/Shader Graph/HDRP/Hair Shader Graph
- Change Asset/Create/Shader/HD Render Pipeline/Lit Shader Graph to Asset/Create/Shader Graph/HDRP/Lit
- Change Asset/Create/Shader/HD Render Pipeline/StackLit Shader Graph to Asset/Create/Shader Graph/HDRP/StackLit Shader GraphShader Graph
- Change Asset/Create/Shader/HD Render Pipeline/Unlit Shader Graph to Asset/Create/Shader Graph/HDRP/Unlit Shader Graph
- Change Asset/Create/Shader/HD Render Pipeline/Custom FullScreen Pass to Asset/Create/Shader/HDRP Custom FullScreen Pass
- Change Asset/Create/Shader/HD Render Pipeline/Custom Renderers Pass to Asset/Create/Shader/HDRP Custom Renderers Pass
- Change Asset/Create/Shader/HD Render Pipeline/Post Process Pass to Asset/Create/Shader/HDRP Post Process
- Change Assets/Create/Rendering/High Definition Render Pipeline Asset to Assets/Create/Rendering/HDRP Asset
- Change Assets/Create/Rendering/Diffusion Profile to Assets/Create/Rendering/HDRP Diffusion Profile
- Change Assets/Create/Rendering/C# Custom Pass to Assets/Create/Rendering/HDRP C# Custom Pass
- Change Assets/Create/Rendering/C# Post Process Volume to Assets/Create/Rendering/HDRP C# Post Process Volume
- Change labels about scroll direction and cloud type.
- Change the handling of additional properties to base class
- Improved shadow cascade GUI drawing with pixel perfect, hover and focus functionalities.
- Improving the screen space global illumination.
- ClearFlag.Depth does not implicitely clear stencil anymore. ClearFlag.Stencil added.
- Improved the Camera Inspector, new sections and better grouping of fields
- Moving MaterialHeaderScopes to Core
- Changed resolution (to match the render buffer) of the sky used for camera misses in Path Tracing. (case 1304114).
- Tidy up of platform abstraction code for shader optimization.
- Display a warning help box when decal atlas is out of size.
- Moved the HDRP render graph debug panel content to the Rendering debug panel.
- Changed Path Tracing's maximum intensity from clamped (0 to 100) to positive value (case 1310514).
- Avoid unnecessary RenderGraphBuilder.ReadTexture in the "Set Final Target" pass
- Change Allow dynamic resolution from Rendering to Output on the Camera Inspector
- Change Link FOV to Physical Camera to Physical Camera, and show and hide everything on the Projection Section
- Change FOV Axis to Field of View Axis
- Density Volumes can now take a 3D RenderTexture as mask, the mask can use RGBA format for RGB fog.
- Decreased the minimal Fog Distance value in the Density Volume to 0.05.
- Virtual Texturing Resolver now performs RTHandle resize logic in HDRP instead of in core Unity
- Cached the base types of Volume Manager to improve memory and cpu usage.
- Reduced the maximal number of bounces for both RTGI and RTR (case 1318876).
- Changed Density Volume for Local Volumetric Fog
- HDRP Global Settings are now saved into their own asset (HDRenderPipelineGlobalSettings) and HDRenderPipeline's default asset refers to this new asset.
- Improved physically based Depth of Field with better near defocus blur quality.
- Changed the behavior of the clear coat and SSR/RTR for the stack lit to mimic the Lit's behavior (case 1320154).
- The default LookDev volume profile is now copied and referened in the Asset folder instead of the package folder.
- Changed normal used in path tracing to create a local light list from the geometric to the smooth shading one.
- Embed the HDRP config package instead of copying locally, the `Packages` folder is versionned by Collaborate. (case 1276518)
- Materials with Transparent Surface type, the property Sorting Priority is clamped on the UI from -50 to 50 instead of -100 to 100.
- Improved lighting models for AxF shader area lights.
- Updated Wizard to better handle RenderPipelineAsset in Quality Settings
- UI for Frame Settings has been updated: default values in the HDRP Settings and Custom Frame Settings are always editable
- Updated Light's shadow layer name in Editor.
- Increased path tracing max samples from 4K to 16K (case 1327729).
- Film grain does not affect the alpha channel.
- Disable TAA sharpening on alpha channel.
- Enforced more consistent shading normal computation for path tracing, so that impossible shading/geometric normal combinations are avoided (case 1323455).
- Default black texture XR is now opaque (alpha = 1).
- Changed ray tracing acceleration structure build, so that only meshes with HDRP materials are included (case 1322365).
- Changed default sidedness to double, when a mesh with a mix of single and double-sided materials is added to the ray tracing acceleration structure (case 1323451).
- Use the new API for updating Reflection Probe state (fixes garbage allocation, case 1290521)
- Augmented debug visualization for probe volumes.
- Global Camera shader constants are now pushed when doing a custom render callback.
- Splited HDProjectSettings with new HDUserSettings in UserProject. Now Wizard working variable should not bother versioning tool anymore (case 1330640)
- Removed redundant Show Inactive Objects and Isolate Selection checkboxes from the Emissive Materials tab of the Light Explorer (case 1331750).
- Renaming Decal Projector to HDRP Decal Projector.
- The HDRP Render Graph now uses the new RendererList API for rendering and (optional) pass culling.
- Increased the minimal density of the volumetric clouds.
- Changed the storage format of volumetric clouds presets for easier editing.
- Reduced the maximum distance per ray step of volumetric clouds.
- Improved the fly through ghosting artifacts in the volumetric clouds.
- Make LitTessellation and LayeredLitTessellation fallback on Lit and LayeredLit respectively in DXR.
- Display an info box and disable MSAA  asset entry when ray tracing is enabled.
- Changed light reset to preserve type.
- Ignore hybrid duplicated reflection probes during light baking.
- Replaced the context menu by a search window when adding custom pass.
- Moved supportRuntimeDebugDisplay option from HDRPAsset to HDRPGlobalSettings.
- When a ray hits the sky in the ray marching part of mixed ray tracing, it is considered a miss.
- TAA jitter is disabled while using Frame Debugger now.
- Depth of field at half or quarter resolution is now computed consistently with the full resolution option (case 1335687).
- Hair uses GGX LTC for area light specular.
- Moved invariants outside of loop for a minor CPU speedup in the light loop code.
- Various improvements to the volumetric clouds.
- Moved area light's shadow frustum: light's surface no longer passes through the apex, and instead aligns with the 0-offset near plane.
- Restore old version of the RendererList structs/api for compatibility.
- Various improvements to SSGI (case 1340851, case 1339297, case 1327919).
- Changed the NVIDIA install button to the standard FixMeButton.
- Improved a bit the area cookie behavior for higher smoothness values to reduce artifacts.
- Improved volumetric clouds (added new noise for erosion, reduced ghosting while flying through, altitude distortion, ghosting when changing from local to distant clouds, fix issue in wind distortion along the Z axis).
- Fixed upscaling issue that is exagerated by DLSS (case 1347250).
- Improvements to the RTGI denoising.
- Remove Bilinear and Lanczos upscale filter.
- Make some volumetric clouds properties additional to reduce the number default parameters (case 1357926).
- Renamed the Cloud Offset to Cloud Map Offset in the volumetric clouds volume component (case 1358528).
- Made debug panel mip bias functions internal, not public.
- Mitigate ghosting / overbluring artifacts when TAA and physically-based DoF are enabled by adjusting the internal range of blend factor values (case 1340541).

## [11.0.0] - 2020-10-21

### Added
- Added a new API to bake HDRP probes from C# (case 1276360)
- Added support for pre-exposure for planar reflections.
- Added support for nested volume components to volume system.
- Added a cameraCullingResult field in Custom Pass Context to give access to both custom pass and camera culling result.
- Added a toggle to allow to include or exclude smooth surfaces from ray traced reflection denoising.
- Added support for raytracing for AxF material
- Added rasterized area light shadows for AxF material
- Added a cloud system and the CloudLayer volume override.
- Added per-stage shader keywords.

### Fixed
- Fixed probe volumes debug views.
- Fixed ShaderGraph Decal material not showing exposed properties.
- Fixed couple samplers that had the wrong name in raytracing code
- VFX: Fixed LPPV with lit particles in deferred (case 1293608)
- Fixed the default background color for previews to use the original color.
- Fixed compilation issues on platforms that don't support XR.
- Fixed issue with compute shader stripping for probe volumes variants.
- Fixed issue with an empty index buffer not being released.
- Fixed issue when debug full screen 'Transparent Screen Space Reflection' do not take in consideration debug exposure

### Changed
- Removed the material pass probe volumes evaluation mode.
- Volume parameter of type Cubemap can now accept Cubemap render textures and custom render textures.
- Removed the superior clamping value for the recursive rendering max ray length.
- Removed the superior clamping value for the ray tracing light cluster size.
- Removed the readonly keyword on the cullingResults of the CustomPassContext to allow users to overwrite.
- The DrawRenderers function of CustomPassUtils class now takes a sortingCriteria in parameter.
- When in half res, RTR denoising is executed at half resolution and the upscale happens at the end.
- Removed the upscale radius from the RTR.

## [10.3.0] - 2020-12-01

### Added
- Added a slider to control the fallback value of the directional shadow when the cascade have no coverage.
- Added light unit slider for automatic and automatic histrogram exposure limits.
- Added View Bias for mesh decals.
- Added support for the PlayStation 5 platform.

### Fixed
- Fixed computation of geometric normal in path tracing (case 1293029).
- Fixed issues with path-traced volumetric scattering (cases 1295222, 1295234).
- Fixed issue with faulty shadow transition when view is close to an object under some aspect ratio conditions
- Fixed issue where some ShaderGraph generated shaders were not SRP compatible because of UnityPerMaterial cbuffer layout mismatches [1292501] (https://issuetracker.unity3d.com/issues/a2-some-translucent-plus-alphaclipping-shadergraphs-are-not-srp-batcher-compatible)
- Fixed issues with path-traced volumetric scattering (cases 1295222, 1295234)
- Fixed Rendergraph issue with virtual texturing and debug mode while in forward.
- Fixed wrong coat normal space in shader graph
- Fixed NullPointerException when baking probes from the lighting window (case 1289680)
- Fixed volumetric fog with XR single-pass rendering.
- Fixed issues with first frame rendering when RenderGraph is used (auto exposure, AO)
- Fixed AOV api in render graph (case 1296605)
- Fixed a small discrepancy in the marker placement in light intensity sliders (case 1299750)
- Fixed issue with VT resolve pass rendergraph errors when opaque and transparent are disabled in frame settings.
- Fixed a bug in the sphere-aabb light cluster (case 1294767).
- Fixed issue when submitting SRPContext during EndCameraRendering.
- Fixed baked light being included into the ray tracing light cluster (case 1296203).
- Fixed enums UI for the shadergraph nodes.
- Fixed ShaderGraph stack blocks appearing when opening the settings in Hair and Eye ShaderGraphs.
- Fixed white screen when undoing in the editor.
- Fixed display of LOD Bias and maximum level in frame settings when using Quality Levels
- Fixed an issue when trying to open a look dev env library when Look Dev is not supported.
- Fixed shader graph not supporting indirectdxr multibounce (case 1294694).
- Fixed the planar depth texture not being properly created and rendered to (case 1299617).
- Fixed C# 8 compilation issue with turning on nullable checks (case 1300167)
- Fixed affects AO for deacl materials.
- Fixed case where material keywords would not get setup before usage.
- Fixed an issue with material using distortion from ShaderGraph init after Material creation (case 1294026)
- Fixed Clearcoat on Stacklit or Lit breaks when URP is imported into the project (case 1297806)
- VFX : Debug material view were rendering pink for albedo. (case 1290752)
- Fixed XR depth copy when using MSAA.
- Fixed GC allocations from XR occlusion mesh when using multipass.
- Fixed an issue with the frame count management for the volumetric fog (case 1299251).
- Fixed an issue with half res ssgi upscale.
- Fixed timing issues with accumulation motion blur
- Fixed register spilling on  FXC in light list shaders.
- Fixed issue with shadow mask and area lights.
- Fixed an issue with the capture callback (now includes post processing results).
- Fixed decal draw order for ShaderGraph decal materials.
- Fixed StackLit ShaderGraph surface option property block to only display energy conserving specular color option for the specular parametrization (case 1257050)
- Fixed missing BeginCameraRendering call for custom render mode of a Camera.
- Fixed LayerMask editor for volume parameters.
- Fixed the condition on temporal accumulation in the reflection denoiser (case 1303504).
- Fixed box light attenuation.
- Fixed after post process custom pass scale issue when dynamic resolution is enabled (case 1299194).
- Fixed an issue with light intensity prefab override application not visible in the inspector (case 1299563).
- Fixed Undo/Redo instability of light temperature.
- Fixed label style in pbr sky editor.
- Fixed side effect on styles during compositor rendering.
- Fixed size and spacing of compositor info boxes (case 1305652).
- Fixed spacing of UI widgets in the Graphics Compositor (case 1305638).
- Fixed undo-redo on layered lit editor.
- Fixed tesselation culling, big triangles using lit tesselation shader would dissapear when camera is too close to them (case 1299116)
- Fixed issue with compositor related custom passes still active after disabling the compositor (case 1305330)
- Fixed regression in Wizard that not fix runtime ressource anymore (case 1287627)
- Fixed error in Depth Of Field near radius blur calculation (case 1306228).
- Fixed a reload bug when using objects from the scene in the lookdev (case 1300916).
- Fixed some render texture leaks.
- Fixed light gizmo showing shadow near plane when shadows are disabled.
- Fixed path tracing alpha channel support (case 1304187).
- Fixed shadow matte not working with ambient occlusion when MSAA is enabled
- Fixed issues with compositor's undo (cases 1305633, 1307170).
- VFX : Debug material view incorrect depth test. (case 1293291)
- Fixed wrong shader / properties assignement to materials created from 3DsMax 2021 Physical Material. (case 1293576)
- Fixed Emissive color property from Autodesk Interactive materials not editable in Inspector. (case 1307234)
- Fixed exception when changing the current render pipeline to from HDRP to universal (case 1306291).
- Fixed an issue in shadergraph when switch from a RenderingPass (case 1307653)
- Fixed LookDev environment library assignement after leaving playmode.
- Fixed a locale issue with the diffusion profile property values in ShaderGraph on PC where comma is the decimal separator.
- Fixed error in the RTHandle scale of Depth Of Field when TAA is enabled.
- Fixed Quality Level set to the last one of the list after a Build (case 1307450)
- Fixed XR depth copy (case 1286908).
- Fixed Warnings about "SceneIdMap" missing script in eye material sample scene

### Changed
- Now reflection probes cannot have SSAO, SSGI, SSR, ray tracing effects or volumetric reprojection.
- Rename HDRP sub menu in Assets/Create/Shader to HD Render Pipeline for consistency.
- Improved robustness of volumetric sampling in path tracing (case 1295187).
- Changed the message when the graphics device doesn't support ray tracing (case 1287355).
- When a Custom Pass Volume is disabled, the custom pass Cleanup() function is called, it allows to release resources when the volume isn't used anymore.
- Enable Reflector for Spotlight by default
- Changed the convergence time of ssgi to 16 frames and the preset value
- Changed the clamping approach for RTR and RTGI (in both perf and quality) to improve visual quality.
- Changed the warning message for ray traced area shadows (case 1303410).
- Disabled specular occlusion for what we consider medium and larger scale ao > 1.25 with a 25cm falloff interval.
- Change the source value for the ray tracing frame index iterator from m_FrameCount to the camera frame count (case 1301356).
- Removed backplate from rendering of lighting cubemap as it did not really work conceptually and caused artefacts.
- Transparent materials created by the Model Importer are set to not cast shadows. ( case 1295747)
- Change some light unit slider value ranges to better reflect the lighting scenario.
- Change the tooltip for color shadows and semi-transparent shadows (case 1307704).

## [10.2.1] - 2020-11-30

### Added
- Added a warning when trying to bake with static lighting being in an invalid state.

### Fixed
- Fixed stylesheet reloading for LookDev window and Wizard window.
- Fixed XR single-pass rendering with legacy shaders using unity_StereoWorldSpaceCameraPos.
- Fixed issue displaying wrong debug mode in runtime debug menu UI.
- Fixed useless editor repaint when using lod bias.
- Fixed multi-editing with new light intensity slider.
- Fixed issue with density volumes flickering when editing shape box.
- Fixed issue with image layers in the graphics compositor (case 1289936).
- Fixed issue with angle fading when rotating decal projector.
- Fixed issue with gameview repaint in the graphics compositor (case 1290622).
- Fixed some labels being clipped in the Render Graph Viewer
- Fixed issue when decal projector material is none.
- Fixed the sampling of the normal buffer in the the forward transparent pass.
- Fixed bloom prefiltering tooltip.
- Fixed NullReferenceException when loading multipel scene async
- Fixed missing alpha blend state properties in Axf shader and update default stencil properties
- Fixed normal buffer not bound to custom pass anymore.
- Fixed issues with camera management in the graphics compositor (cases 1292548, 1292549).
- Fixed an issue where a warning about the static sky not being ready was wrongly displayed.
- Fixed the clear coat not being handled properly for SSR and RTR (case 1291654).
- Fixed ghosting in RTGI and RTAO when denoising is enabled and the RTHandle size is not equal to the Viewport size (case 1291654).
- Fixed alpha output when atmospheric scattering is enabled.
- Fixed issue with TAA history sharpening when view is downsampled.
- Fixed lookdev movement.
- Fixed volume component tooltips using the same parameter name.
- Fixed issue with saving some quality settings in volume overrides  (case 1293747)
- Fixed NullReferenceException in HDRenderPipeline.UpgradeResourcesIfNeeded (case 1292524)
- Fixed SSGI texture allocation when not using the RenderGraph.
- Fixed NullReference Exception when setting Max Shadows On Screen to 0 in the HDRP asset.
- Fixed path tracing accumulation not being reset when changing to a different frame of an animation.
- Fixed issue with saving some quality settings in volume overrides  (case 1293747)

### Changed
- Volume Manager now always tests scene culling masks. This was required to fix hybrid workflow.
- Now the screen space shadow is only used if the analytic value is valid.
- Distance based roughness is disabled by default and have a control
- Changed the name from the Depth Buffer Thickness to Depth Tolerance for SSGI (case 1301352).

## [10.2.0] - 2020-10-19

### Added
- Added a rough distortion frame setting and and info box on distortion materials.
- Adding support of 4 channel tex coords for ray tracing (case 1265309).
- Added a help button on the volume component toolbar for documentation.
- Added range remapping to metallic property for Lit and Decal shaders.
- Exposed the API to access HDRP shader pass names.
- Added the status check of default camera frame settings in the DXR wizard.
- Added frame setting for Virtual Texturing.
- Added a fade distance for light influencing volumetric lighting.
- Adding an "Include For Ray Tracing" toggle on lights to allow the user to exclude them when ray tracing is enabled in the frame settings of a camera.
- Added fog volumetric scattering support for path tracing.
- Added new algorithm for SSR with temporal accumulation
- Added quality preset of the new volumetric fog parameters.
- Added missing documentation for unsupported SG RT nodes and light's include for raytracing attrbute.
- Added documentation for LODs not being supported by ray tracing.
- Added more options to control how the component of motion vectors coming from the camera transform will affect the motion blur with new clamping modes.
- Added anamorphism support for phsyical DoF, switched to blue noise sampling and fixed tiling artifacts.

### Fixed
- Fixed an issue where the Exposure Shader Graph node had clipped text. (case 1265057)
- Fixed an issue when rendering into texture where alpha would not default to 1.0 when using 11_11_10 color buffer in non-dev builds.
- Fixed issues with reordering and hiding graphics compositor layers (cases 1283903, 1285282, 1283886).
- Fixed the possibility to have a shader with a pre-refraction render queue and refraction enabled at the same time.
- Fixed a migration issue with the rendering queue in ShaderGraph when upgrading to 10.x;
- Fixed the object space matrices in shader graph for ray tracing.
- Changed the cornea refraction function to take a view dir in object space.
- Fixed upside down XR occlusion mesh.
- Fixed precision issue with the atmospheric fog.
- Fixed issue with TAA and no motion vectors.
- Fixed the stripping not working the terrain alphatest feature required for terrain holes (case 1205902).
- Fixed bounding box generation that resulted in incorrect light culling (case 3875925).
- VFX : Fix Emissive writing in Opaque Lit Output with PSSL platforms (case 273378).
- Fixed issue where pivot of DecalProjector was not aligned anymore on Transform position when manipulating the size of the projector from the Inspector.
- Fixed a null reference exception when creating a diffusion profile asset.
- Fixed the diffusion profile not being registered as a dependency of the ShaderGraph.
- Fixing exceptions in the console when putting the SSGI in low quality mode (render graph).
- Fixed NullRef Exception when decals are in the scene, no asset is set and HDRP wizard is run.
- Fixed issue with TAA causing bleeding of a view into another when multiple views are visible.
- Fix an issue that caused issues of usability of editor if a very high resolution is set by mistake and then reverted back to a smaller resolution.
- Fixed issue where Default Volume Profile Asset change in project settings was not added to the undo stack (case 1285268).
- Fixed undo after enabling compositor.
- Fixed the ray tracing shadow UI being displayed while it shouldn't (case 1286391).
- Fixed issues with physically-based DoF, improved speed and robustness
- Fixed a warning happening when putting the range of lights to 0.
- Fixed issue when null parameters in a volume component would spam null reference errors. Produce a warning instead.
- Fixed volument component creation via script.
- Fixed GC allocs in render graph.
- Fixed scene picking passes.
- Fixed broken ray tracing light cluster full screen debug.
- Fixed dead code causing error.
- Fixed issue when dragging slider in inspector for ProjectionDepth.
- Fixed issue when resizing Inspector window that make the DecalProjector editor flickers.
- Fixed issue in DecalProjector editor when the Inspector window have a too small width: the size appears on 2 lines but the editor not let place for the second one.
- Fixed issue (null reference in console) when selecting a DensityVolume with rectangle selection.
- Fixed issue when linking the field of view with the focal length in physical camera
- Fixed supported platform build and error message.
- Fixed exceptions occuring when selecting mulitple decal projectors without materials assigned (case 1283659).
- Fixed LookDev error message when pipeline is not loaded.
- Properly reject history when enabling seond denoiser for RTGI.
- Fixed an issue that could cause objects to not be rendered when using Vulkan API.
- Fixed issue with lookdev shadows looking wrong upon exiting playmode.
- Fixed temporary Editor freeze when selecting AOV output in graphics compositor (case 1288744).
- Fixed normal flip with double sided materials.
- Fixed shadow resolution settings level in the light explorer.
- Fixed the ShaderGraph being dirty after the first save.
- Fixed XR shadows culling
- Fixed Nans happening when upscaling the RTGI.
- Fixed the adjust weight operation not being done for the non-rendergraph pipeline.
- Fixed overlap with SSR Transparent default frame settings message on DXR Wizard.
- Fixed alpha channel in the stop NaNs and motion blur shaders.
- Fixed undo of duplicate environments in the look dev environment library.
- Fixed a ghosting issue with RTShadows (Sun, Point and Spot), RTAO and RTGI when the camera is moving fast.
- Fixed a SSGI denoiser bug for large scenes.
- Fixed a Nan issue with SSGI.
- Fixed an issue with IsFrontFace node in Shader Graph not working properly
- Fixed CustomPassUtils.RenderFrom* functions and CustomPassUtils.DisableSinglePassRendering struct in VR.
- Fixed custom pass markers not recorded when render graph was enabled.
- Fixed exceptions when unchecking "Big Tile Prepass" on the frame settings with render-graph.
- Fixed an issue causing errors in GenerateMaxZ when opaque objects or decals are disabled.
- Fixed an issue with Bake button of Reflection Probe when in custom mode
- Fixed exceptions related to the debug display settings when changing the default frame settings.
- Fixed picking for materials with depth offset.
- Fixed issue with exposure history being uninitialized on second frame.
- Fixed issue when changing FoV with the physical camera fold-out closed.
- Fixed some labels being clipped in the Render Graph Viewer

### Changed
- Combined occlusion meshes into one to reduce draw calls and state changes with XR single-pass.
- Claryfied doc for the LayeredLit material.
- Various improvements for the Volumetric Fog.
- Use draggable fields for float scalable settings
- Migrated the fabric & hair shadergraph samples directly into the renderpipeline resources.
- Removed green coloration of the UV on the DecalProjector gizmo.
- Removed _BLENDMODE_PRESERVE_SPECULAR_LIGHTING keyword from shaders.
- Now the DXR wizard displays the name of the target asset that needs to be changed.
- Standardized naming for the option regarding Transparent objects being able to receive Screen Space Reflections.
- Making the reflection and refractions of cubemaps distance based.
- Changed Receive SSR to also controls Receive SSGI on opaque objects.
- Improved the punctual light shadow rescale algorithm.
- Changed the names of some of the parameters for the Eye Utils SG Nodes.
- Restored frame setting for async compute of contact shadows.
- Removed the possibility to have MSAA (through the frame settings) when ray tracing is active.
- Range handles for decal projector angle fading.
- Smoother angle fading for decal projector.

## [10.1.0] - 2020-10-12

### Added
- Added an option to have only the metering mask displayed in the debug mode.
- Added a new mode to cluster visualization debug where users can see a slice instead of the cluster on opaque objects.
- Added ray traced reflection support for the render graph version of the pipeline.
- Added render graph support of RTAO and required denoisers.
- Added render graph support of RTGI.
- Added support of RTSSS and Recursive Rendering in the render graph mode.
- Added support of RT and screen space shadow for render graph.
- Added tooltips with the full name of the (graphics) compositor properties to properly show large names that otherwise are clipped by the UI (case 1263590)
- Added error message if a callback AOV allocation fail
- Added marker for all AOV request operation on GPU
- Added remapping options for Depth Pyramid debug view mode
- Added an option to support AOV shader at runtime in HDRP settings (case 1265070)
- Added support of SSGI in the render graph mode.
- Added option for 11-11-10 format for cube reflection probes.
- Added an optional check in the HDRP DXR Wizard to verify 64 bits target architecture
- Added option to display timing stats in the debug menu as an average over 1 second.
- Added a light unit slider to provide users more context when authoring physically based values.
- Added a way to check the normals through the material views.
- Added Simple mode to Earth Preset for PBR Sky
- Added the export of normals during the prepass for shadow matte for proper SSAO calculation.
- Added the usage of SSAO for shadow matte unlit shader graph.
- Added the support of input system V2
- Added a new volume component parameter to control the max ray length of directional lights(case 1279849).
- Added support for 'Pyramid' and 'Box' spot light shapes in path tracing.
- Added high quality prefiltering option for Bloom.
- Added support for camera relative ray tracing (and keeping non-camera relative ray tracing working)
- Added a rough refraction option on planar reflections.
- Added scalability settings for the planar reflection resolution.
- Added tests for AOV stacking and UI rendering in the graphics compositor.
- Added a new ray tracing only function that samples the specular part of the materials.
- Adding missing marker for ray tracing profiling (RaytracingDeferredLighting)
- Added the support of eye shader for ray tracing.
- Exposed Refraction Model to the material UI when using a Lit ShaderGraph.
- Added bounding sphere support to screen-space axis-aligned bounding box generation pass.

### Fixed
- Fixed several issues with physically-based DoF (TAA ghosting of the CoC buffer, smooth layer transitions, etc)
- Fixed GPU hang on D3D12 on xbox.
- Fixed game view artifacts on resizing when hardware dynamic resolution was enabled
- Fixed black line artifacts occurring when Lanczos upsampling was set for dynamic resolution
- Fixed Amplitude -> Min/Max parametrization conversion
- Fixed CoatMask block appearing when creating lit master node (case 1264632)
- Fixed issue with SceneEV100 debug mode indicator when rescaling the window.
- Fixed issue with PCSS filter being wrong on first frame.
- Fixed issue with emissive mesh for area light not appearing in playmode if Reload Scene option is disabled in Enter Playmode Settings.
- Fixed issue when Reflection Probes are set to OnEnable and are never rendered if the probe is enabled when the camera is farther than the probe fade distance.
- Fixed issue with sun icon being clipped in the look dev window.
- Fixed error about layers when disabling emissive mesh for area lights.
- Fixed issue when the user deletes the composition graph or .asset in runtime (case 1263319)
- Fixed assertion failure when changing resolution to compositor layers after using AOVs (case 1265023)
- Fixed flickering layers in graphics compositor (case 1264552)
- Fixed issue causing the editor field not updating the disc area light radius.
- Fixed issues that lead to cookie atlas to be updated every frame even if cached data was valid.
- Fixed an issue where world space UI was not emitted for reflection cameras in HDRP
- Fixed an issue with cookie texture atlas that would cause realtime textures to always update in the atlas even when the content did not change.
- Fixed an issue where only one of the two lookdev views would update when changing the default lookdev volume profile.
- Fixed a bug related to light cluster invalidation.
- Fixed shader warning in DofGather (case 1272931)
- Fixed AOV export of depth buffer which now correctly export linear depth (case 1265001)
- Fixed issue that caused the decal atlas to not be updated upon changing of the decal textures content.
- Fixed "Screen position out of view frustum" error when camera is at exactly the planar reflection probe location.
- Fixed Amplitude -> Min/Max parametrization conversion
- Fixed issue that allocated a small cookie for normal spot lights.
- Fixed issue when undoing a change in diffuse profile list after deleting the volume profile.
- Fixed custom pass re-ordering and removing.
- Fixed TAA issue and hardware dynamic resolution.
- Fixed a static lighting flickering issue caused by having an active planar probe in the scene while rendering inspector preview.
- Fixed an issue where even when set to OnDemand, the sky lighting would still be updated when changing sky parameters.
- Fixed an error message trigerred when a mesh has more than 32 sub-meshes (case 1274508).
- Fixed RTGI getting noisy for grazying angle geometry (case 1266462).
- Fixed an issue with TAA history management on pssl.
- Fixed the global illumination volume override having an unwanted advanced mode (case 1270459).
- Fixed screen space shadow option displayed on directional shadows while they shouldn't (case 1270537).
- Fixed the handling of undo and redo actions in the graphics compositor (cases 1268149, 1266212, 1265028)
- Fixed issue with composition graphs that include virtual textures, cubemaps and other non-2D textures (cases 1263347, 1265638).
- Fixed issues when selecting a new composition graph or setting it to None (cases 1263350, 1266202)
- Fixed ArgumentNullException when saving shader graphs after removing the compositor from the scene (case 1268658)
- Fixed issue with updating the compositor output when not in play mode (case 1266216)
- Fixed warning with area mesh (case 1268379)
- Fixed issue with diffusion profile not being updated upon reset of the editor.
- Fixed an issue that lead to corrupted refraction in some scenarios on xbox.
- Fixed for light loop scalarization not happening.
- Fixed issue with stencil not being set in rendergraph mode.
- Fixed for post process being overridable in reflection probes even though it is not supported.
- Fixed RTGI in performance mode when light layers are enabled on the asset.
- Fixed SSS materials appearing black in matcap mode.
- Fixed a collision in the interaction of RTR and RTGI.
- Fix for lookdev toggling renderers that are set to non editable or are hidden in the inspector.
- Fixed issue with mipmap debug mode not properly resetting full screen mode (and viceversa).
- Added unsupported message when using tile debug mode with MSAA.
- Fixed SSGI compilation issues on PS4.
- Fixed "Screen position out of view frustum" error when camera is on exactly the planar reflection probe plane.
- Workaround issue that caused objects using eye shader to not be rendered on xbox.
- Fixed GC allocation when using XR single-pass test mode.
- Fixed text in cascades shadow split being truncated.
- Fixed rendering of custom passes in the Custom Pass Volume inspector
- Force probe to render again if first time was during async shader compilation to avoid having cyan objects.
- Fixed for lookdev library field not being refreshed upon opening a library from the environment library inspector.
- Fixed serialization issue with matcap scale intensity.
- Close Add Override popup of Volume Inspector when the popup looses focus (case 1258571)
- Light quality setting for contact shadow set to on for High quality by default.
- Fixed an exception thrown when closing the look dev because there is no active SRP anymore.
- Fixed alignment of framesettings in HDRP Default Settings
- Fixed an exception thrown when closing the look dev because there is no active SRP anymore.
- Fixed an issue where entering playmode would close the LookDev window.
- Fixed issue with rendergraph on console failing on SSS pass.
- Fixed Cutoff not working properly with ray tracing shaders default and SG (case 1261292).
- Fixed shader compilation issue with Hair shader and debug display mode
- Fixed cubemap static preview not updated when the asset is imported.
- Fixed wizard DXR setup on non-DXR compatible devices.
- Fixed Custom Post Processes affecting preview cameras.
- Fixed issue with lens distortion breaking rendering.
- Fixed save popup appearing twice due to HDRP wizard.
- Fixed error when changing planar probe resolution.
- Fixed the dependecy of FrameSettings (MSAA, ClearGBuffer, DepthPrepassWithDeferred) (case 1277620).
- Fixed the usage of GUIEnable for volume components (case 1280018).
- Fixed the diffusion profile becoming invalid when hitting the reset (case 1269462).
- Fixed issue with MSAA resolve killing the alpha channel.
- Fixed a warning in materialevalulation
- Fixed an error when building the player.
- Fixed issue with box light not visible if range is below one and range attenuation is off.
- Fixed an issue that caused a null reference when deleting camera component in a prefab. (case 1244430)
- Fixed issue with bloom showing a thin black line after rescaling window.
- Fixed rendergraph motion vector resolve.
- Fixed the Ray-Tracing related Debug Display not working in render graph mode.
- Fix nan in pbr sky
- Fixed Light skin not properly applied on the LookDev when switching from Dark Skin (case 1278802)
- Fixed accumulation on DX11
- Fixed issue with screen space UI not drawing on the graphics compositor (case 1279272).
- Fixed error Maximum allowed thread group count is 65535 when resolution is very high.
- LOD meshes are now properly stripped based on the maximum lod value parameters contained in the HDRP asset.
- Fixed an inconsistency in the LOD group UI where LOD bias was not the right one.
- Fixed outlines in transitions between post-processed and plain regions in the graphics compositor (case 1278775).
- Fix decal being applied twice with LOD Crossfade.
- Fixed camera stacking for AOVs in the graphics compositor (case 1273223).
- Fixed backface selection on some shader not ignore correctly.
- Disable quad overdraw on ps4.
- Fixed error when resizing the graphics compositor's output and when re-adding a compositor in the scene
- Fixed issues with bloom, alpha and HDR layers in the compositor (case 1272621).
- Fixed alpha not having TAA applied to it.
- Fix issue with alpha output in forward.
- Fix compilation issue on Vulkan for shaders using high quality shadows in XR mode.
- Fixed wrong error message when fixing DXR resources from Wizard.
- Fixed compilation error of quad overdraw with double sided materials
- Fixed screen corruption on xbox when using TAA and Motion Blur with rendergraph.
- Fixed UX issue in the graphics compositor related to clear depth and the defaults for new layers, add better tooltips and fix minor bugs (case 1283904)
- Fixed scene visibility not working for custom pass volumes.
- Fixed issue with several override entries in the runtime debug menu.
- Fixed issue with rendergraph failing to execute every 30 minutes.
- Fixed Lit ShaderGraph surface option property block to only display transmission and energy conserving specular color options for their proper material mode (case 1257050)
- Fixed nan in reflection probe when volumetric fog filtering is enabled, causing the whole probe to be invalid.
- Fixed Debug Color pixel became grey
- Fixed TAA flickering on the very edge of screen.
- Fixed profiling scope for quality RTGI.
- Fixed the denoising and multi-sample not being used for smooth multibounce RTReflections.
- Fixed issue where multiple cameras would cause GC each frame.
- Fixed after post process rendering pass options not showing for unlit ShaderGraphs.
- Fixed null reference in the Undo callback of the graphics compositor
- Fixed cullmode for SceneSelectionPass.
- Fixed issue that caused non-static object to not render at times in OnEnable reflection probes.
- Baked reflection probes now correctly use static sky for ambient lighting.

### Changed
- Preparation pass for RTSSShadows to be supported by render graph.
- Add tooltips with the full name of the (graphics) compositor properties to properly show large names that otherwise are clipped by the UI (case 1263590)
- Composition profile .asset files cannot be manually edited/reset by users (to avoid breaking things - case 1265631)
- Preparation pass for RTSSShadows to be supported by render graph.
- Changed the way the ray tracing property is displayed on the material (QOL 1265297).
- Exposed lens attenuation mode in default settings and remove it as a debug mode.
- Composition layers without any sub layers are now cleared to black to avoid confusion (case 1265061).
- Slight reduction of VGPR used by area light code.
- Changed thread group size for contact shadows (save 1.1ms on PS4)
- Make sure distortion stencil test happens before pixel shader is run.
- Small optimization that allows to skip motion vector prepping when the whole wave as velocity of 0.
- Improved performance to avoid generating coarse stencil buffer when not needed.
- Remove HTile generation for decals (faster without).
- Improving SSGI Filtering and fixing a blend issue with RTGI.
- Changed the Trackball UI so that it allows explicit numeric values.
- Reduce the G-buffer footprint of anisotropic materials
- Moved SSGI out of preview.
- Skip an unneeded depth buffer copy on consoles.
- Replaced the Density Volume Texture Tool with the new 3D Texture Importer.
- Rename Raytracing Node to Raytracing Quality Keyword and rename high and low inputs as default and raytraced. All raytracing effects now use the raytraced mode but path tracing.
- Moved diffusion profile list to the HDRP default settings panel.
- Skip biquadratic resampling of vbuffer when volumetric fog filtering is enabled.
- Optimized Grain and sRGB Dithering.
- On platforms that allow it skip the first mip of the depth pyramid and compute it alongside the depth buffer used for low res transparents.
- When trying to install the local configuration package, if another one is already present the user is now asked whether they want to keep it or not.
- Improved MSAA color resolve to fix issues when very bright and very dark samples are resolved together.
- Improve performance of GPU light AABB generation
- Removed the max clamp value for the RTR, RTAO and RTGI's ray length (case 1279849).
- Meshes assigned with a decal material are not visible anymore in ray-tracing or path-tracing.
- Removed BLEND shader keywords.
- Remove a rendergraph debug option to clear resources on release from UI.
- added SV_PrimitiveID in the VaryingMesh structure for fulldebugscreenpass as well as primitiveID in FragInputs
- Changed which local frame is used for multi-bounce RTReflections.
- Move System Generated Values semantics out of VaryingsMesh structure.
- Other forms of FSAA are silently deactivated, when path tracing is on.
- Removed XRSystemTests. The GC verification is now done during playmode tests (case 1285012).
- SSR now uses the pre-refraction color pyramid.
- Various improvements for the Volumetric Fog.
- Optimizations for volumetric fog.

## [10.0.0] - 2019-06-10

### Added
- Ray tracing support for VR single-pass
- Added sharpen filter shader parameter and UI for TemporalAA to control image quality instead of hardcoded value
- Added frame settings option for custom post process and custom passes as well as custom color buffer format option.
- Add check in wizard on SRP Batcher enabled.
- Added default implementations of OnPreprocessMaterialDescription for FBX, Obj, Sketchup and 3DS file formats.
- Added custom pass fade radius
- Added after post process injection point for custom passes
- Added basic alpha compositing support - Alpha is available afterpostprocess when using FP16 buffer format.
- Added falloff distance on Reflection Probe and Planar Reflection Probe
- Added Backplate projection from the HDRISky
- Added Shadow Matte in UnlitMasterNode, which only received shadow without lighting
- Added hability to name LightLayers in HDRenderPipelineAsset
- Added a range compression factor for Reflection Probe and Planar Reflection Probe to avoid saturation of colors.
- Added path tracing support for directional, point and spot lights, as well as emission from Lit and Unlit.
- Added non temporal version of SSAO.
- Added more detailed ray tracing stats in the debug window
- Added Disc area light (bake only)
- Added a warning in the material UI to prevent transparent + subsurface-scattering combination.
- Added XR single-pass setting into HDRP asset
- Added a penumbra tint option for lights
- Added support for depth copy with XR SDK
- Added debug setting to Rendering Debugger Window to list the active XR views
- Added an option to filter the result of the volumetric lighting (off by default).
- Added a transmission multiplier for directional lights
- Added XR single-pass test mode to Rendering Debugger Window
- Added debug setting to Render Pipeline Window to list the active XR views
- Added a new refraction mode for the Lit shader (thin). Which is a box refraction with small thickness values
- Added the code to support Barn Doors for Area Lights based on a shaderconfig option.
- Added HDRPCameraBinder property binder for Visual Effect Graph
- Added "Celestial Body" controls to the Directional Light
- Added new parameters to the Physically Based Sky
- Added Reflections to the DXR Wizard
- Added the possibility to have ray traced colored and semi-transparent shadows on directional lights.
- Added a check in the custom post process template to throw an error if the default shader is not found.
- Exposed the debug overlay ratio in the debug menu.
- Added a separate frame settings for tonemapping alongside color grading.
- Added the receive fog option in the material UI for ShaderGraphs.
- Added a public virtual bool in the custom post processes API to specify if a post processes should be executed in the scene view.
- Added a menu option that checks scene issues with ray tracing. Also removed the previously existing warning at runtime.
- Added Contrast Adaptive Sharpen (CAS) Upscaling effect.
- Added APIs to update probe settings at runtime.
- Added documentation for the rayTracingSupported method in HDRP
- Added user-selectable format for the post processing passes.
- Added support for alpha channel in some post-processing passes (DoF, TAA, Uber).
- Added warnings in FrameSettings inspector when using DXR and atempting to use Asynchronous Execution.
- Exposed Stencil bits that can be used by the user.
- Added history rejection based on velocity of intersected objects for directional, point and spot lights.
- Added a affectsVolumetric field to the HDAdditionalLightData API to know if light affects volumetric fog.
- Add OS and Hardware check in the Wizard fixes for DXR.
- Added option to exclude camera motion from motion blur.
- Added semi-transparent shadows for point and spot lights.
- Added support for semi-transparent shadow for unlit shader and unlit shader graph.
- Added the alpha clip enabled toggle to the material UI for all HDRP shader graphs.
- Added Material Samples to explain how to use the lit shader features
- Added an initial implementation of ray traced sub surface scattering
- Added AssetPostprocessors and Shadergraphs to handle Arnold Standard Surface and 3DsMax Physical material import from FBX.
- Added support for Smoothness Fade start work when enabling ray traced reflections.
- Added Contact shadow, Micro shadows and Screen space refraction API documentation.
- Added script documentation for SSR, SSAO (ray tracing), GI, Light Cluster, RayTracingSettings, Ray Counters, etc.
- Added path tracing support for refraction and internal reflections.
- Added support for Thin Refraction Model and Lit's Clear Coat in Path Tracing.
- Added the Tint parameter to Sky Colored Fog.
- Added of Screen Space Reflections for Transparent materials
- Added a fallback for ray traced area light shadows in case the material is forward or the lit mode is forward.
- Added a new debug mode for light layers.
- Added an "enable" toggle to the SSR volume component.
- Added support for anisotropic specular lobes in path tracing.
- Added support for alpha clipping in path tracing.
- Added support for light cookies in path tracing.
- Added support for transparent shadows in path tracing.
- Added support for iridescence in path tracing.
- Added support for background color in path tracing.
- Added a path tracing test to the test suite.
- Added a warning and workaround instructions that appear when you enable XR single-pass after the first frame with the XR SDK.
- Added the exposure sliders to the planar reflection probe preview
- Added support for subsurface scattering in path tracing.
- Added a new mode that improves the filtering of ray traced shadows (directional, point and spot) based on the distance to the occluder.
- Added support of cookie baking and add support on Disc light.
- Added support for fog attenuation in path tracing.
- Added a new debug panel for volumes
- Added XR setting to control camera jitter for temporal effects
- Added an error message in the DrawRenderers custom pass when rendering opaque objects with an HDRP asset in DeferredOnly mode.
- Added API to enable proper recording of path traced scenes (with the Unity recorder or other tools).
- Added support for fog in Recursive rendering, ray traced reflections and ray traced indirect diffuse.
- Added an alpha blend option for recursive rendering
- Added support for stack lit for ray tracing effects.
- Added support for hair for ray tracing effects.
- Added support for alpha to coverage for HDRP shaders and shader graph
- Added support for Quality Levels to Subsurface Scattering.
- Added option to disable XR rendering on the camera settings.
- Added support for specular AA from geometric curvature in AxF
- Added support for baked AO (no input for now) in AxF
- Added an info box to warn about depth test artifacts when rendering object twice in custom passes with MSAA.
- Added a frame setting for alpha to mask.
- Added support for custom passes in the AOV API
- Added Light decomposition lighting debugging modes and support in AOV
- Added exposure compensation to Fixed exposure mode
- Added support for rasterized area light shadows in StackLit
- Added support for texture-weighted automatic exposure
- Added support for POM for emissive map
- Added alpha channel support in motion blur pass.
- Added the HDRP Compositor Tool (in Preview).
- Added a ray tracing mode option in the HDRP asset that allows to override and shader stripping.
- Added support for arbitrary resolution scaling of Volumetric Lighting to the Fog volume component.
- Added range attenuation for box-shaped spotlights.
- Added scenes for hair and fabric and decals with material samples
- Added fabric materials and textures
- Added information for fabric materials in fabric scene
- Added a DisplayInfo attribute to specify a name override and a display order for Volume Component fields (used only in default inspector for now).
- Added Min distance to contact shadows.
- Added support for Depth of Field in path tracing (by sampling the lens aperture).
- Added an API in HDRP to override the camera within the rendering of a frame (mainly for custom pass).
- Added a function (HDRenderPipeline.ResetRTHandleReferenceSize) to reset the reference size of RTHandle systems.
- Added support for AxF measurements importing into texture resources tilings.
- Added Layer parameter on Area Light to modify Layer of generated Emissive Mesh
- Added a flow map parameter to HDRI Sky
- Implemented ray traced reflections for transparent objects.
- Add a new parameter to control reflections in recursive rendering.
- Added an initial version of SSGI.
- Added Virtual Texturing cache settings to control the size of the Streaming Virtual Texturing caches.
- Added back-compatibility with builtin stereo matrices.
- Added CustomPassUtils API to simplify Blur, Copy and DrawRenderers custom passes.
- Added Histogram guided automatic exposure.
- Added few exposure debug modes.
- Added support for multiple path-traced views at once (e.g., scene and game views).
- Added support for 3DsMax's 2021 Simplified Physical Material from FBX files in the Model Importer.
- Added custom target mid grey for auto exposure.
- Added CustomPassUtils API to simplify Blur, Copy and DrawRenderers custom passes.
- Added an API in HDRP to override the camera within the rendering of a frame (mainly for custom pass).
- Added more custom pass API functions, mainly to render objects from another camera.
- Added support for transparent Unlit in path tracing.
- Added a minimal lit used for RTGI in peformance mode.
- Added procedural metering mask that can follow an object
- Added presets quality settings for RTAO and RTGI.
- Added an override for the shadow culling that allows better directional shadow maps in ray tracing effects (RTR, RTGI, RTSSS and RR).
- Added a Cloud Layer volume override.
- Added Fast Memory support for platform that support it.
- Added CPU and GPU timings for ray tracing effects.
- Added support to combine RTSSS and RTGI (1248733).
- Added IES Profile support for Point, Spot and Rectangular-Area lights
- Added support for multiple mapping modes in AxF.
- Add support of lightlayers on indirect lighting controller
- Added compute shader stripping.
- Added Cull Mode option for opaque materials and ShaderGraphs.
- Added scene view exposure override.
- Added support for exposure curve remapping for min/max limits.
- Added presets for ray traced reflections.
- Added final image histogram debug view (both luminance and RGB).
- Added an example texture and rotation to the Cloud Layer volume override.
- Added an option to extend the camera culling for skinned mesh animation in ray tracing effects (1258547).
- Added decal layer system similar to light layer. Mesh will receive a decal when both decal layer mask matches.
- Added shader graph nodes for rendering a complex eye shader.
- Added more controls to contact shadows and increased quality in some parts.
- Added a physically based option in DoF volume.
- Added API to check if a Camera, Light or ReflectionProbe is compatible with HDRP.
- Added path tracing test scene for normal mapping.
- Added missing API documentation.
- Remove CloudLayer
- Added quad overdraw and vertex density debug modes.

### Fixed
- fix when saved HDWizard window tab index out of range (1260273)
- Fix when rescale probe all direction below zero (1219246)
- Update documentation of HDRISky-Backplate, precise how to have Ambient Occlusion on the Backplate
- Sorting, undo, labels, layout in the Lighting Explorer.
- Fixed sky settings and materials in Shader Graph Samples package
- Fix/workaround a probable graphics driver bug in the GTAO shader.
- Fixed Hair and PBR shader graphs double sided modes
- Fixed an issue where updating an HDRP asset in the Quality setting panel would not recreate the pipeline.
- Fixed issue with point lights being considered even when occupying less than a pixel on screen (case 1183196)
- Fix a potential NaN source with iridescence (case 1183216)
- Fixed issue of spotlight breaking when minimizing the cone angle via the gizmo (case 1178279)
- Fixed issue that caused decals not to modify the roughness in the normal buffer, causing SSR to not behave correctly (case 1178336)
- Fixed lit transparent refraction with XR single-pass rendering
- Removed extra jitter for TemporalAA in VR
- Fixed ShaderGraph time in main preview
- Fixed issue on some UI elements in HDRP asset not expanding when clicking the arrow (case 1178369)
- Fixed alpha blending in custom post process
- Fixed the modification of the _AlphaCutoff property in the material UI when exposed with a ShaderGraph parameter.
- Fixed HDRP test `1218_Lit_DiffusionProfiles` on Vulkan.
- Fixed an issue where building a player in non-dev mode would generate render target error logs every frame
- Fixed crash when upgrading version of HDRP
- Fixed rendering issues with material previews
- Fixed NPE when using light module in Shuriken particle systems (1173348).
- Refresh cached shadow on editor changes
- Fixed light supported units caching (1182266)
- Fixed an issue where SSAO (that needs temporal reprojection) was still being rendered when Motion Vectors were not available (case 1184998)
- Fixed a nullref when modifying the height parameters inside the layered lit shader UI.
- Fixed Decal gizmo that become white after exiting play mode
- Fixed Decal pivot position to behave like a spotlight
- Fixed an issue where using the LightingOverrideMask would break sky reflection for regular cameras
- Fix DebugMenu FrameSettingsHistory persistency on close
- Fix DensityVolume, ReflectionProbe aned PlanarReflectionProbe advancedControl display
- Fix DXR scene serialization in wizard
- Fixed an issue where Previews would reallocate History Buffers every frame
- Fixed the SetLightLayer function in HDAdditionalLightData setting the wrong light layer
- Fix error first time a preview is created for planar
- Fixed an issue where SSR would use an incorrect roughness value on ForwardOnly (StackLit, AxF, Fabric, etc.) materials when the pipeline is configured to also allow deferred Lit.
- Fixed issues with light explorer (cases 1183468, 1183269)
- Fix dot colors in LayeredLit material inspector
- Fix undo not resetting all value when undoing the material affectation in LayerLit material
- Fix for issue that caused gizmos to render in render textures (case 1174395)
- Fixed the light emissive mesh not updated when the light was disabled/enabled
- Fixed light and shadow layer sync when setting the HDAdditionalLightData.lightlayersMask property
- Fixed a nullref when a custom post process component that was in the HDRP PP list is removed from the project
- Fixed issue that prevented decals from modifying specular occlusion (case 1178272).
- Fixed exposure of volumetric reprojection
- Fixed multi selection support for Scalable Settings in lights
- Fixed font shaders in test projects for VR by using a Shader Graph version
- Fixed refresh of baked cubemap by incrementing updateCount at the end of the bake (case 1158677).
- Fixed issue with rectangular area light when seen from the back
- Fixed decals not affecting lightmap/lightprobe
- Fixed zBufferParams with XR single-pass rendering
- Fixed moving objects not rendered in custom passes
- Fixed abstract classes listed in the + menu of the custom pass list
- Fixed custom pass that was rendered in previews
- Fixed precision error in zero value normals when applying decals (case 1181639)
- Fixed issue that triggered No Scene Lighting view in game view as well (case 1156102)
- Assign default volume profile when creating a new HDRP Asset
- Fixed fov to 0 in planar probe breaking the projection matrix (case 1182014)
- Fixed bugs with shadow caching
- Reassign the same camera for a realtime probe face render request to have appropriate history buffer during realtime probe rendering.
- Fixed issue causing wrong shading when normal map mode is Object space, no normal map is set, but a detail map is present (case 1143352)
- Fixed issue with decal and htile optimization
- Fixed TerrainLit shader compilation error regarding `_Control0_TexelSize` redefinition (case 1178480).
- Fixed warning about duplicate HDRuntimeReflectionSystem when configuring play mode without domain reload.
- Fixed an editor crash when multiple decal projectors were selected and some had null material
- Added all relevant fix actions to FixAll button in Wizard
- Moved FixAll button on top of the Wizard
- Fixed an issue where fog color was not pre-exposed correctly
- Fix priority order when custom passes are overlapping
- Fix cleanup not called when the custom pass GameObject is destroyed
- Replaced most instances of GraphicsSettings.renderPipelineAsset by GraphicsSettings.currentRenderPipeline. This should fix some parameters not working on Quality Settings overrides.
- Fixed an issue with Realtime GI not working on upgraded projects.
- Fixed issue with screen space shadows fallback texture was not set as a texture array.
- Fixed Pyramid Lights bounding box
- Fixed terrain heightmap default/null values and epsilons
- Fixed custom post-processing effects breaking when an abstract class inherited from `CustomPostProcessVolumeComponent`
- Fixed XR single-pass rendering in Editor by using ShaderConfig.s_XrMaxViews to allocate matrix array
- Multiple different skies rendered at the same time by different cameras are now handled correctly without flickering
- Fixed flickering issue happening when different volumes have shadow settings and multiple cameras are present.
- Fixed issue causing planar probes to disappear if there is no light in the scene.
- Fixed a number of issues with the prefab isolation mode (Volumes leaking from the main scene and reflection not working properly)
- Fixed an issue with fog volume component upgrade not working properly
- Fixed Spot light Pyramid Shape has shadow artifacts on aspect ratio values lower than 1
- Fixed issue with AO upsampling in XR
- Fixed camera without HDAdditionalCameraData component not rendering
- Removed the macro ENABLE_RAYTRACING for most of the ray tracing code
- Fixed prefab containing camera reloading in loop while selected in the Project view
- Fixed issue causing NaN wheh the Z scale of an object is set to 0.
- Fixed DXR shader passes attempting to render before pipeline loaded
- Fixed black ambient sky issue when importing a project after deleting Library.
- Fixed issue when upgrading a Standard transparent material (case 1186874)
- Fixed area light cookies not working properly with stack lit
- Fixed material render queue not updated when the shader is changed in the material inspector.
- Fixed a number of issues with full screen debug modes not reseting correctly when setting another mutually exclusive mode
- Fixed compile errors for platforms with no VR support
- Fixed an issue with volumetrics and RTHandle scaling (case 1155236)
- Fixed an issue where sky lighting might be updated uselessly
- Fixed issue preventing to allow setting decal material to none (case 1196129)
- Fixed XR multi-pass decals rendering
- Fixed several fields on Light Inspector that not supported Prefab overrides
- Fixed EOL for some files
- Fixed scene view rendering with volumetrics and XR enabled
- Fixed decals to work with multiple cameras
- Fixed optional clear of GBuffer (Was always on)
- Fixed render target clears with XR single-pass rendering
- Fixed HDRP samples file hierarchy
- Fixed Light units not matching light type
- Fixed QualitySettings panel not displaying HDRP Asset
- Fixed black reflection probes the first time loading a project
- Fixed y-flip in scene view with XR SDK
- Fixed Decal projectors do not immediately respond when parent object layer mask is changed in editor.
- Fixed y-flip in scene view with XR SDK
- Fixed a number of issues with Material Quality setting
- Fixed the transparent Cull Mode option in HD unlit master node settings only visible if double sided is ticked.
- Fixed an issue causing shadowed areas by contact shadows at the edge of far clip plane if contact shadow length is very close to far clip plane.
- Fixed editing a scalable settings will edit all loaded asset in memory instead of targetted asset.
- Fixed Planar reflection default viewer FOV
- Fixed flickering issues when moving the mouse in the editor with ray tracing on.
- Fixed the ShaderGraph main preview being black after switching to SSS in the master node settings
- Fixed custom fullscreen passes in VR
- Fixed camera culling masks not taken in account in custom pass volumes
- Fixed object not drawn in custom pass when using a DrawRenderers with an HDRP shader in a build.
- Fixed injection points for Custom Passes (AfterDepthAndNormal and BeforePreRefraction were missing)
- Fixed a enum to choose shader tags used for drawing objects (DepthPrepass or Forward) when there is no override material.
- Fixed lit objects in the BeforePreRefraction, BeforeTransparent and BeforePostProcess.
- Fixed the None option when binding custom pass render targets to allow binding only depth or color.
- Fixed custom pass buffers allocation so they are not allocated if they're not used.
- Fixed the Custom Pass entry in the volume create asset menu items.
- Fixed Prefab Overrides workflow on Camera.
- Fixed alignment issue in Preset for Camera.
- Fixed alignment issue in Physical part for Camera.
- Fixed FrameSettings multi-edition.
- Fixed a bug happening when denoising multiple ray traced light shadows
- Fixed minor naming issues in ShaderGraph settings
- VFX: Removed z-fight glitches that could appear when using deferred depth prepass and lit quad primitives
- VFX: Preserve specular option for lit outputs (matches HDRP lit shader)
- Fixed an issue with Metal Shader Compiler and GTAO shader for metal
- Fixed resources load issue while upgrading HDRP package.
- Fix LOD fade mask by accounting for field of view
- Fixed spot light missing from ray tracing indirect effects.
- Fixed a UI bug in the diffusion profile list after fixing them from the wizard.
- Fixed the hash collision when creating new diffusion profile assets.
- Fixed a light leaking issue with box light casting shadows (case 1184475)
- Fixed Cookie texture type in the cookie slot of lights (Now displays a warning because it is not supported).
- Fixed a nullref that happens when using the Shuriken particle light module
- Fixed alignment in Wizard
- Fixed text overflow in Wizard's helpbox
- Fixed Wizard button fix all that was not automatically grab all required fixes
- Fixed VR tab for MacOS in Wizard
- Fixed local config package workflow in Wizard
- Fixed issue with contact shadows shifting when MSAA is enabled.
- Fixed EV100 in the PBR sky
- Fixed an issue In URP where sometime the camera is not passed to the volume system and causes a null ref exception (case 1199388)
- Fixed nullref when releasing HDRP with custom pass disabled
- Fixed performance issue derived from copying stencil buffer.
- Fixed an editor freeze when importing a diffusion profile asset from a unity package.
- Fixed an exception when trying to reload a builtin resource.
- Fixed the light type intensity unit reset when switching the light type.
- Fixed compilation error related to define guards and CreateLayoutFromXrSdk()
- Fixed documentation link on CustomPassVolume.
- Fixed player build when HDRP is in the project but not assigned in the graphic settings.
- Fixed an issue where ambient probe would be black for the first face of a baked reflection probe
- VFX: Fixed Missing Reference to Visual Effect Graph Runtime Assembly
- Fixed an issue where rendering done by users in EndCameraRendering would be executed before the main render loop.
- Fixed Prefab Override in main scope of Volume.
- Fixed alignment issue in Presset of main scope of Volume.
- Fixed persistence of ShowChromeGizmo and moved it to toolbar for coherency in ReflectionProbe and PlanarReflectionProbe.
- Fixed Alignement issue in ReflectionProbe and PlanarReflectionProbe.
- Fixed Prefab override workflow issue in ReflectionProbe and PlanarReflectionProbe.
- Fixed empty MoreOptions and moved AdvancedManipulation in a dedicated location for coherency in ReflectionProbe and PlanarReflectionProbe.
- Fixed Prefab override workflow issue in DensityVolume.
- Fixed empty MoreOptions and moved AdvancedManipulation in a dedicated location for coherency in DensityVolume.
- Fix light limit counts specified on the HDRP asset
- Fixed Quality Settings for SSR, Contact Shadows and Ambient Occlusion volume components
- Fixed decalui deriving from hdshaderui instead of just shaderui
- Use DelayedIntField instead of IntField for scalable settings
- Fixed init of debug for FrameSettingsHistory on SceneView camera
- Added a fix script to handle the warning 'referenced script in (GameObject 'SceneIDMap') is missing'
- Fix Wizard load when none selected for RenderPipelineAsset
- Fixed TerrainLitGUI when per-pixel normal property is not present.
- Fixed rendering errors when enabling debug modes with custom passes
- Fix an issue that made PCSS dependent on Atlas resolution (not shadow map res)
- Fixing a bug whith histories when n>4 for ray traced shadows
- Fixing wrong behavior in ray traced shadows for mesh renderers if their cast shadow is shadow only or double sided
- Only tracing rays for shadow if the point is inside the code for spotlight shadows
- Only tracing rays if the point is inside the range for point lights
- Fixing ghosting issues when the screen space shadow  indexes change for a light with ray traced shadows
- Fixed an issue with stencil management and Xbox One build that caused corrupted output in deferred mode.
- Fixed a mismatch in behavior between the culling of shadow maps and ray traced point and spot light shadows
- Fixed recursive ray tracing not working anymore after intermediate buffer refactor.
- Fixed ray traced shadow denoising not working (history rejected all the time).
- Fixed shader warning on xbox one
- Fixed cookies not working for spot lights in ray traced reflections, ray traced GI and recursive rendering
- Fixed an inverted handling of CoatSmoothness for SSR in StackLit.
- Fixed missing distortion inputs in Lit and Unlit material UI.
- Fixed issue that propagated NaNs across multiple frames through the exposure texture.
- Fixed issue with Exclude from TAA stencil ignored.
- Fixed ray traced reflection exposure issue.
- Fixed issue with TAA history not initialising corretly scale factor for first frame
- Fixed issue with stencil test of material classification not using the correct Mask (causing false positive and bad performance with forward material in deferred)
- Fixed issue with History not reset when chaning antialiasing mode on camera
- Fixed issue with volumetric data not being initialized if default settings have volumetric and reprojection off.
- Fixed ray tracing reflection denoiser not applied in tier 1
- Fixed the vibility of ray tracing related methods.
- Fixed the diffusion profile list not saved when clicking the fix button in the material UI.
- Fixed crash when pushing bounce count higher than 1 for ray traced GI or reflections
- Fixed PCSS softness scale so that it better match ray traced reference for punctual lights.
- Fixed exposure management for the path tracer
- Fixed AxF material UI containing two advanced options settings.
- Fixed an issue where cached sky contexts were being destroyed wrongly, breaking lighting in the LookDev
- Fixed issue that clamped PCSS softness too early and not after distance scale.
- Fixed fog affect transparent on HD unlit master node
- Fixed custom post processes re-ordering not saved.
- Fixed NPE when using scalable settings
- Fixed an issue where PBR sky precomputation was reset incorrectly in some cases causing bad performance.
- Fixed a bug due to depth history begin overriden too soon
- Fixed CustomPassSampleCameraColor scale issue when called from Before Transparent injection point.
- Fixed corruption of AO in baked probes.
- Fixed issue with upgrade of projects that still had Very High as shadow filtering quality.
- Fixed issue that caused Distortion UI to appear in Lit.
- Fixed several issues with decal duplicating when editing them.
- Fixed initialization of volumetric buffer params (1204159)
- Fixed an issue where frame count was incorrectly reset for the game view, causing temporal processes to fail.
- Fixed Culling group was not disposed error.
- Fixed issues on some GPU that do not support gathers on integer textures.
- Fixed an issue with ambient probe not being initialized for the first frame after a domain reload for volumetric fog.
- Fixed the scene visibility of decal projectors and density volumes
- Fixed a leak in sky manager.
- Fixed an issue where entering playmode while the light editor is opened would produce null reference exceptions.
- Fixed the debug overlay overlapping the debug menu at runtime.
- Fixed an issue with the framecount when changing scene.
- Fixed errors that occurred when using invalid near and far clip plane values for planar reflections.
- Fixed issue with motion blur sample weighting function.
- Fixed motion vectors in MSAA.
- Fixed sun flare blending (case 1205862).
- Fixed a lot of issues related to ray traced screen space shadows.
- Fixed memory leak caused by apply distortion material not being disposed.
- Fixed Reflection probe incorrectly culled when moving its parent (case 1207660)
- Fixed a nullref when upgrading the Fog volume components while the volume is opened in the inspector.
- Fix issues where decals on PS4 would not correctly write out the tile mask causing bits of the decal to go missing.
- Use appropriate label width and text content so the label is completely visible
- Fixed an issue where final post process pass would not output the default alpha value of 1.0 when using 11_11_10 color buffer format.
- Fixed SSR issue after the MSAA Motion Vector fix.
- Fixed an issue with PCSS on directional light if punctual shadow atlas was not allocated.
- Fixed an issue where shadow resolution would be wrong on the first face of a baked reflection probe.
- Fixed issue with PCSS softness being incorrect for cascades different than the first one.
- Fixed custom post process not rendering when using multiple HDRP asset in quality settings
- Fixed probe gizmo missing id (case 1208975)
- Fixed a warning in raytracingshadowfilter.compute
- Fixed issue with AO breaking with small near plane values.
- Fixed custom post process Cleanup function not called in some cases.
- Fixed shader warning in AO code.
- Fixed a warning in simpledenoiser.compute
- Fixed tube and rectangle light culling to use their shape instead of their range as a bounding box.
- Fixed caused by using gather on a UINT texture in motion blur.
- Fix issue with ambient occlusion breaking when dynamic resolution is active.
- Fixed some possible NaN causes in Depth of Field.
- Fixed Custom Pass nullref due to the new Profiling Sample API changes
- Fixed the black/grey screen issue on after post process Custom Passes in non dev builds.
- Fixed particle lights.
- Improved behavior of lights and probe going over the HDRP asset limits.
- Fixed issue triggered when last punctual light is disabled and more than one camera is used.
- Fixed Custom Pass nullref due to the new Profiling Sample API changes
- Fixed the black/grey screen issue on after post process Custom Passes in non dev builds.
- Fixed XR rendering locked to vsync of main display with Standalone Player.
- Fixed custom pass cleanup not called at the right time when using multiple volumes.
- Fixed an issue on metal with edge of decal having artifact by delaying discard of fragments during decal projection
- Fixed various shader warning
- Fixing unnecessary memory allocations in the ray tracing cluster build
- Fixed duplicate column labels in LightEditor's light tab
- Fixed white and dark flashes on scenes with very high or very low exposure when Automatic Exposure is being used.
- Fixed an issue where passing a null ProfilingSampler would cause a null ref exception.
- Fixed memory leak in Sky when in matcap mode.
- Fixed compilation issues on platform that don't support VR.
- Fixed migration code called when we create a new HDRP asset.
- Fixed RemoveComponent on Camera contextual menu to not remove Camera while a component depend on it.
- Fixed an issue where ambient occlusion and screen space reflections editors would generate null ref exceptions when HDRP was not set as the current pipeline.
- Fixed a null reference exception in the probe UI when no HDRP asset is present.
- Fixed the outline example in the doc (sampling range was dependent on screen resolution)
- Fixed a null reference exception in the HDRI Sky editor when no HDRP asset is present.
- Fixed an issue where Decal Projectors created from script where rotated around the X axis by 90°.
- Fixed frustum used to compute Density Volumes visibility when projection matrix is oblique.
- Fixed a null reference exception in Path Tracing, Recursive Rendering and raytraced Global Illumination editors when no HDRP asset is present.
- Fix for NaNs on certain geometry with Lit shader -- [case 1210058](https://fogbugz.unity3d.com/f/cases/1210058/)
- Fixed an issue where ambient occlusion and screen space reflections editors would generate null ref exceptions when HDRP was not set as the current pipeline.
- Fixed a null reference exception in the probe UI when no HDRP asset is present.
- Fixed the outline example in the doc (sampling range was dependent on screen resolution)
- Fixed a null reference exception in the HDRI Sky editor when no HDRP asset is present.
- Fixed an issue where materials newly created from the contextual menu would have an invalid state, causing various problems until it was edited.
- Fixed transparent material created with ZWrite enabled (now it is disabled by default for new transparent materials)
- Fixed mouseover on Move and Rotate tool while DecalProjector is selected.
- Fixed wrong stencil state on some of the pixel shader versions of deferred shader.
- Fixed an issue where creating decals at runtime could cause a null reference exception.
- Fixed issue that displayed material migration dialog on the creation of new project.
- Fixed various issues with time and animated materials (cases 1210068, 1210064).
- Updated light explorer with latest changes to the Fog and fixed issues when no visual environment was present.
- Fixed not handleling properly the recieve SSR feature with ray traced reflections
- Shadow Atlas is no longer allocated for area lights when they are disabled in the shader config file.
- Avoid MRT Clear on PS4 as it is not implemented yet.
- Fixed runtime debug menu BitField control.
- Fixed the radius value used for ray traced directional light.
- Fixed compilation issues with the layered lit in ray tracing shaders.
- Fixed XR autotests viewport size rounding
- Fixed mip map slider knob displayed when cubemap have no mipmap
- Remove unnecessary skip of material upgrade dialog box.
- Fixed the profiling sample mismatch errors when enabling the profiler in play mode
- Fixed issue that caused NaNs in reflection probes on consoles.
- Fixed adjusting positive axis of Blend Distance slides the negative axis in the density volume component.
- Fixed the blend of reflections based on the weight.
- Fixed fallback for ray traced reflections when denoising is enabled.
- Fixed error spam issue with terrain detail terrainDetailUnsupported (cases 1211848)
- Fixed hardware dynamic resolution causing cropping/scaling issues in scene view (case 1158661)
- Fixed Wizard check order for `Hardware and OS` and `Direct3D12`
- Fix AO issue turning black when Far/Near plane distance is big.
- Fixed issue when opening lookdev and the lookdev volume have not been assigned yet.
- Improved memory usage of the sky system.
- Updated label in HDRP quality preference settings (case 1215100)
- Fixed Decal Projector gizmo not undoing properly (case 1216629)
- Fix a leak in the denoising of ray traced reflections.
- Fixed Alignment issue in Light Preset
- Fixed Environment Header in LightingWindow
- Fixed an issue where hair shader could write garbage in the diffuse lighting buffer, causing NaNs.
- Fixed an exposure issue with ray traced sub-surface scattering.
- Fixed runtime debug menu light hierarchy None not doing anything.
- Fixed the broken ShaderGraph preview when creating a new Lit graph.
- Fix indentation issue in preset of LayeredLit material.
- Fixed minor issues with cubemap preview in the inspector.
- Fixed wrong build error message when building for android on mac.
- Fixed an issue related to denoising ray trace area shadows.
- Fixed wrong build error message when building for android on mac.
- Fixed Wizard persistency of Direct3D12 change on domain reload.
- Fixed Wizard persistency of FixAll on domain reload.
- Fixed Wizard behaviour on domain reload.
- Fixed a potential source of NaN in planar reflection probe atlas.
- Fixed an issue with MipRatio debug mode showing _DebugMatCapTexture not being set.
- Fixed missing initialization of input params in Blit for VR.
- Fix Inf source in LTC for area lights.
- Fix issue with AO being misaligned when multiple view are visible.
- Fix issue that caused the clamp of camera rotation motion for motion blur to be ineffective.
- Fixed issue with AssetPostprocessors dependencies causing models to be imported twice when upgrading the package version.
- Fixed culling of lights with XR SDK
- Fixed memory stomp in shadow caching code, leading to overflow of Shadow request array and runtime errors.
- Fixed an issue related to transparent objects reading the ray traced indirect diffuse buffer
- Fixed an issue with filtering ray traced area lights when the intensity is high or there is an exposure.
- Fixed ill-formed include path in Depth Of Field shader.
- Fixed shader graph and ray tracing after the shader target PR.
- Fixed a bug in semi-transparent shadows (object further than the light casting shadows)
- Fix state enabled of default volume profile when in package.
- Fixed removal of MeshRenderer and MeshFilter on adding Light component.
- Fixed Ray Traced SubSurface Scattering not working with ray traced area lights
- Fixed Ray Traced SubSurface Scattering not working in forward mode.
- Fixed a bug in debug light volumes.
- Fixed a bug related to ray traced area light shadow history.
- Fixed an issue where fog sky color mode could sample NaNs in the sky cubemap.
- Fixed a leak in the PBR sky renderer.
- Added a tooltip to the Ambient Mode parameter in the Visual Envionment volume component.
- Static lighting sky now takes the default volume into account (this fixes discrepancies between baked and realtime lighting).
- Fixed a leak in the sky system.
- Removed MSAA Buffers allocation when lit shader mode is set to "deferred only".
- Fixed invalid cast for realtime reflection probes (case 1220504)
- Fixed invalid game view rendering when disabling all cameras in the scene (case 1105163)
- Hide reflection probes in the renderer components.
- Fixed infinite reload loop while displaying Light's Shadow's Link Light Layer in Inspector of Prefab Asset.
- Fixed the culling was not disposed error in build log.
- Fixed the cookie atlas size and planar atlas size being too big after an upgrade of the HDRP asset.
- Fixed transparent SSR for shader graph.
- Fixed an issue with emissive light meshes not being in the RAS.
- Fixed DXR player build
- Fixed the HDRP asset migration code not being called after an upgrade of the package
- Fixed draw renderers custom pass out of bound exception
- Fixed the PBR shader rendering in deferred
- Fixed some typos in debug menu (case 1224594)
- Fixed ray traced point and spot lights shadows not rejecting istory when semi-transparent or colored.
- Fixed a warning due to StaticLightingSky when reloading domain in some cases.
- Fixed the MaxLightCount being displayed when the light volume debug menu is on ColorAndEdge.
- Fixed issue with unclear naming of debug menu for decals.
- Fixed z-fighting in scene view when scene lighting is off (case 1203927)
- Fixed issue that prevented cubemap thumbnails from rendering (only on D3D11 and Metal).
- Fixed ray tracing with VR single-pass
- Fix an exception in ray tracing that happens if two LOD levels are using the same mesh renderer.
- Fixed error in the console when switching shader to decal in the material UI.
- Fixed an issue with refraction model and ray traced recursive rendering (case 1198578).
- Fixed an issue where a dynamic sky changing any frame may not update the ambient probe.
- Fixed cubemap thumbnail generation at project load time.
- Fixed cubemap thumbnail generation at project load time.
- Fixed XR culling with multiple cameras
- Fixed XR single-pass with Mock HMD plugin
- Fixed sRGB mismatch with XR SDK
- Fixed an issue where default volume would not update when switching profile.
- Fixed issue with uncached reflection probe cameras reseting the debug mode (case 1224601)
- Fixed an issue where AO override would not override specular occlusion.
- Fixed an issue where Volume inspector might not refresh correctly in some cases.
- Fixed render texture with XR
- Fixed issue with resources being accessed before initialization process has been performed completely.
- Half fixed shuriken particle light that cast shadows (only the first one will be correct)
- Fixed issue with atmospheric fog turning black if a planar reflection probe is placed below ground level. (case 1226588)
- Fixed custom pass GC alloc issue in CustomPassVolume.GetActiveVolumes().
- Fixed a bug where instanced shadergraph shaders wouldn't compile on PS4.
- Fixed an issue related to the envlightdatasrt not being bound in recursive rendering.
- Fixed shadow cascade tooltip when using the metric mode (case 1229232)
- Fixed how the area light influence volume is computed to match rasterization.
- Focus on Decal uses the extends of the projectors
- Fixed usage of light size data that are not available at runtime.
- Fixed the depth buffer copy made before custom pass after opaque and normal injection point.
- Fix for issue that prevented scene from being completely saved when baked reflection probes are present and lighting is set to auto generate.
- Fixed drag area width at left of Light's intensity field in Inspector.
- Fixed light type resolution when performing a reset on HDAdditionalLightData (case 1220931)
- Fixed reliance on atan2 undefined behavior in motion vector debug shader.
- Fixed an usage of a a compute buffer not bound (1229964)
- Fixed an issue where changing the default volume profile from another inspector would not update the default volume editor.
- Fix issues in the post process system with RenderTexture being invalid in some cases, causing rendering problems.
- Fixed an issue where unncessarily serialized members in StaticLightingSky component would change each time the scene is changed.
- Fixed a weird behavior in the scalable settings drawing when the space becomes tiny (1212045).
- Fixed a regression in the ray traced indirect diffuse due to the new probe system.
- Fix for range compression factor for probes going negative (now clamped to positive values).
- Fixed path validation when creating new volume profile (case 1229933)
- Fixed a bug where Decal Shader Graphs would not recieve reprojected Position, Normal, or Bitangent data. (1239921)
- Fix reflection hierarchy for CARPAINT in AxF.
- Fix precise fresnel for delta lights for SVBRDF in AxF.
- Fixed the debug exposure mode for display sky reflection and debug view baked lighting
- Fixed MSAA depth resolve when there is no motion vectors
- Fixed various object leaks in HDRP.
- Fixed compile error with XR SubsystemManager.
- Fix for assertion triggering sometimes when saving a newly created lit shader graph (case 1230996)
- Fixed culling of planar reflection probes that change position (case 1218651)
- Fixed null reference when processing lightprobe (case 1235285)
- Fix issue causing wrong planar reflection rendering when more than one camera is present.
- Fix black screen in XR when HDRP package is present but not used.
- Fixed an issue with the specularFGD term being used when the material has a clear coat (lit shader).
- Fixed white flash happening with auto-exposure in some cases (case 1223774)
- Fixed NaN which can appear with real time reflection and inf value
- Fixed an issue that was collapsing the volume components in the HDRP default settings
- Fixed warning about missing bound decal buffer
- Fixed shader warning on Xbox for ResolveStencilBuffer.compute.
- Fixed PBR shader ZTest rendering in deferred.
- Replaced commands incompatible with async compute in light list build process.
- Diffusion Profile and Material references in HDRP materials are now correctly exported to unity packages. Note that the diffusion profile or the material references need to be edited once before this can work properly.
- Fix MaterialBalls having same guid issue
- Fix spelling and grammatical errors in material samples
- Fixed unneeded cookie texture allocation for cone stop lights.
- Fixed scalarization code for contact shadows.
- Fixed volume debug in playmode
- Fixed issue when toggling anything in HDRP asset that will produce an error (case 1238155)
- Fixed shader warning in PCSS code when using Vulkan.
- Fixed decal that aren't working without Metal and Ambient Occlusion option enabled.
- Fixed an error about procedural sky being logged by mistake.
- Fixed shadowmask UI now correctly showing shadowmask disable
- Made more explicit the warning about raytracing and asynchronous compute. Also fixed the condition in which it appears.
- Fixed a null ref exception in static sky when the default volume profile is invalid.
- DXR: Fixed shader compilation error with shader graph and pathtracer
- Fixed SceneView Draw Modes not being properly updated after opening new scene view panels or changing the editor layout.
- VFX: Removed irrelevant queues in render queue selection from HDRP outputs
- VFX: Motion Vector are correctly renderered with MSAA [Case 1240754](https://issuetracker.unity3d.com/product/unity/issues/guid/1240754/)
- Fixed a cause of NaN when a normal of 0-length is generated (usually via shadergraph).
- Fixed issue with screen-space shadows not enabled properly when RT is disabled (case 1235821)
- Fixed a performance issue with stochastic ray traced area shadows.
- Fixed cookie texture not updated when changing an import settings (srgb for example).
- Fixed flickering of the game/scene view when lookdev is running.
- Fixed issue with reflection probes in realtime time mode with OnEnable baking having wrong lighting with sky set to dynamic (case 1238047).
- Fixed transparent motion vectors not working when in MSAA.
- Fix error when removing DecalProjector from component contextual menu (case 1243960)
- Fixed issue with post process when running in RGBA16 and an object with additive blending is in the scene.
- Fixed corrupted values on LayeredLit when using Vertex Color multiply mode to multiply and MSAA is activated.
- Fix conflicts with Handles manipulation when performing a Reset in DecalComponent (case 1238833)
- Fixed depth prepass and postpass being disabled after changing the shader in the material UI.
- Fixed issue with sceneview camera settings not being saved after Editor restart.
- Fixed issue when switching back to custom sensor type in physical camera settings (case 1244350).
- Fixed a null ref exception when running playmode tests with the Rendering Debugger window opened.
- Fixed some GCAlloc in the debug window.
- Fixed shader graphs not casting semi-transparent and color shadows (case 1242617)
- Fixed thin refraction mode not working properly.
- Fixed assert on tests caused by probe culling results being requested when culling did not happen. (case 1246169)
- Fixed over consumption of GPU memory by the Physically Based Sky.
- Fixed an invalid rotation in Planar Reflection Probe editor display, that was causing an error message (case 1182022)
- Put more information in Camera background type tooltip and fixed inconsistent exposure behavior when changing bg type.
- Fixed issue that caused not all baked reflection to be deleted upon clicking "Clear Baked Data" in the lighting menu (case 1136080)
- Fixed an issue where asset preview could be rendered white because of static lighting sky.
- Fixed an issue where static lighting was not updated when removing the static lighting sky profile.
- Fixed the show cookie atlas debug mode not displaying correctly when enabling the clear cookie atlas option.
- Fixed various multi-editing issues when changing Emission parameters.
- Fixed error when undo a Reflection Probe removal in a prefab instance. (case 1244047)
- Fixed Microshadow not working correctly in deferred with LightLayers
- Tentative fix for missing include in depth of field shaders.
- Fixed the light overlap scene view draw mode (wasn't working at all).
- Fixed taaFrameIndex and XR tests 4052 and 4053
- Fixed the prefab integration of custom passes (Prefab Override Highlight not working as expected).
- Cloned volume profile from read only assets are created in the root of the project. (case 1154961)
- Fixed Wizard check on default volume profile to also check it is not the default one in package.
- Fix erroneous central depth sampling in TAA.
- Fixed light layers not correctly disabled when the lightlayers is set to Nothing and Lightlayers isn't enabled in HDRP Asset
- Fixed issue with Model Importer materials falling back to the Legacy default material instead of HDRP's default material when import happens at Editor startup.
- Fixed a wrong condition in CameraSwitcher, potentially causing out of bound exceptions.
- Fixed an issue where editing the Look Dev default profile would not reflect directly in the Look Dev window.
- Fixed a bug where the light list is not cleared but still used when resizing the RT.
- Fixed exposure debug shader with XR single-pass rendering.
- Fixed issues with scene view and transparent motion vectors.
- Fixed black screens for linux/HDRP (1246407)
- Fixed a vulkan and metal warning in the SSGI compute shader.
- Fixed an exception due to the color pyramid not allocated when SSGI is enabled.
- Fixed an issue with the first Depth history was incorrectly copied.
- Fixed path traced DoF focusing issue
- Fix an issue with the half resolution Mode (performance)
- Fix an issue with the color intensity of emissive for performance rtgi
- Fixed issue with rendering being mostly broken when target platform disables VR.
- Workaround an issue caused by GetKernelThreadGroupSizes  failing to retrieve correct group size.
- Fix issue with fast memory and rendergraph.
- Fixed transparent motion vector framesetting not sanitized.
- Fixed wrong order of post process frame settings.
- Fixed white flash when enabling SSR or SSGI.
- The ray traced indrect diffuse and RTGI were combined wrongly with the rest of the lighting (1254318).
- Fixed an exception happening when using RTSSS without using RTShadows.
- Fix inconsistencies with transparent motion vectors and opaque by allowing camera only transparent motion vectors.
- Fix reflection probe frame settings override
- Fixed certain shadow bias artifacts present in volumetric lighting (case 1231885).
- Fixed area light cookie not updated when switch the light type from a spot that had a cookie.
- Fixed issue with dynamic resolution updating when not in play mode.
- Fixed issue with Contrast Adaptive Sharpening upsample mode and preview camera.
- Fix issue causing blocky artifacts when decals affect metallic and are applied on material with specular color workflow.
- Fixed issue with depth pyramid generation and dynamic resolution.
- Fixed an issue where decals were duplicated in prefab isolation mode.
- Fixed an issue where rendering preview with MSAA might generate render graph errors.
- Fixed compile error in PS4 for planar reflection filtering.
- Fixed issue with blue line in prefabs for volume mode.
- Fixing the internsity being applied to RTAO too early leading to unexpected results (1254626).
- Fix issue that caused sky to incorrectly render when using a custom projection matrix.
- Fixed null reference exception when using depth pre/post pass in shadergraph with alpha clip in the material.
- Appropriately constraint blend distance of reflection probe while editing with the inspector (case 1248931)
- Fixed AxF handling of roughness for Blinn-Phong type materials
- Fixed AxF UI errors when surface type is switched to transparent
- Fixed a serialization issue, preventing quality level parameters to undo/redo and update scene view on change.
- Fixed an exception occuring when a camera doesn't have an HDAdditionalCameraData (1254383).
- Fixed ray tracing with XR single-pass.
- Fixed warning in HDAdditionalLightData OnValidate (cases 1250864, 1244578)
- Fixed a bug related to denoising ray traced reflections.
- Fixed nullref in the layered lit material inspector.
- Fixed an issue where manipulating the color wheels in a volume component would reset the cursor every time.
- Fixed an issue where static sky lighting would not be updated for a new scene until it's reloaded at least once.
- Fixed culling for decals when used in prefabs and edited in context.
- Force to rebake probe with missing baked texture. (1253367)
- Fix supported Mac platform detection to handle new major version (11.0) properly
- Fixed typo in the Render Pipeline Wizard under HDRP+VR
- Change transparent SSR name in frame settings to avoid clipping.
- Fixed missing include guards in shadow hlsl files.
- Repaint the scene view whenever the scene exposure override is changed.
- Fixed an error when clearing the SSGI history texture at creation time (1259930).
- Fixed alpha to mask reset when toggling alpha test in the material UI.
- Fixed an issue where opening the look dev window with the light theme would make the window blink and eventually crash unity.
- Fixed fallback for ray tracing and light layers (1258837).
- Fixed Sorting Priority not displayed correctly in the DrawRenderers custom pass UI.
- Fixed glitch in Project settings window when selecting diffusion profiles in material section (case 1253090)
- Fixed issue with light layers bigger than 8 (and above the supported range).
- Fixed issue with culling layer mask of area light's emissive mesh
- Fixed overused the atlas for Animated/Render Target Cookies (1259930).
- Fixed errors when switching area light to disk shape while an area emissive mesh was displayed.
- Fixed default frame settings MSAA toggle for reflection probes (case 1247631)
- Fixed the transparent SSR dependency not being properly disabled according to the asset dependencies (1260271).
- Fixed issue with completely black AO on double sided materials when normal mode is set to None.
- Fixed UI drawing of the quaternion (1251235)
- Fix an issue with the quality mode and perf mode on RTR and RTGI and getting rid of unwanted nans (1256923).
- Fixed unitialized ray tracing resources when using non-default HDRP asset (case 1259467).
- Fixed overused the atlas for Animated/Render Target Cookies (1259930).
- Fixed sky asserts with XR multipass
- Fixed for area light not updating baked light result when modifying with gizmo.
- Fixed robustness issue with GetOddNegativeScale() in ray tracing, which was impacting normal mapping (1261160).
- Fixed regression where moving face of the probe gizmo was not moving its position anymore.
- Fixed XR single-pass macros in tessellation shaders.
- Fixed path-traced subsurface scattering mixing with diffuse and specular BRDFs (1250601).
- Fixed custom pass re-ordering issues.
- Improved robustness of normal mapping when scale is 0, and mapping is extreme (normals in or below the tangent plane).
- Fixed XR Display providers not getting zNear and zFar plane distances passed to them when in HDRP.
- Fixed rendering breaking when disabling tonemapping in the frame settings.
- Fixed issue with serialization of exposure modes in volume profiles not being consistent between HDRP versions (case 1261385).
- Fixed issue with duplicate names in newly created sub-layers in the graphics compositor (case 1263093).
- Remove MSAA debug mode when renderpipeline asset has no MSAA
- Fixed some post processing using motion vectors when they are disabled
- Fixed the multiplier of the environement lights being overriden with a wrong value for ray tracing (1260311).
- Fixed a series of exceptions happening when trying to load an asset during wizard execution (1262171).
- Fixed an issue with Stacklit shader not compiling correctly in player with debug display on (1260579)
- Fixed couple issues in the dependence of building the ray tracing acceleration structure.
- Fix sun disk intensity
- Fixed unwanted ghosting for smooth surfaces.
- Fixing an issue in the recursive rendering flag texture usage.
- Fixed a missing dependecy for choosing to evaluate transparent SSR.
- Fixed issue that failed compilation when XR is disabled.
- Fixed a compilation error in the IES code.
- Fixed issue with dynamic resolution handler when no OnResolutionChange callback is specified.
- Fixed multiple volumes, planar reflection, and decal projector position when creating them from the menu.
- Reduced the number of global keyword used in deferredTile.shader
- Fixed incorrect processing of Ambient occlusion probe (9% error was introduced)
- Fixed multiedition of framesettings drop down (case 1270044)
- Fixed planar probe gizmo

### Changed
- Improve MIP selection for decals on Transparents
- Color buffer pyramid is not allocated anymore if neither refraction nor distortion are enabled
- Rename Emission Radius to Radius in UI in Point, Spot
- Angular Diameter parameter for directional light is no longuer an advanced property
- DXR: Remove Light Radius and Angular Diamater of Raytrace shadow. Angular Diameter and Radius are used instead.
- Remove MaxSmoothness parameters from UI for point, spot and directional light. The MaxSmoothness is now deduce from Radius Parameters
- DXR: Remove the Ray Tracing Environement Component. Add a Layer Mask to the ray Tracing volume components to define which objects are taken into account for each effect.
- Removed second cubemaps used for shadowing in lookdev
- Disable Physically Based Sky below ground
- Increase max limit of area light and reflection probe to 128
- Change default texture for detailmap to grey
- Optimize Shadow RT load on Tile based architecture platforms.
- Improved quality of SSAO.
- Moved RequestShadowMapRendering() back to public API.
- Update HDRP DXR Wizard with an option to automatically clone the hdrp config package and setup raytracing to 1 in shaders file.
- Added SceneSelection pass for TerrainLit shader.
- Simplified Light's type API regrouping the logic in one place (Check type in HDAdditionalLightData)
- The support of LOD CrossFade (Dithering transition) in master nodes now required to enable it in the master node settings (Save variant)
- Improved shadow bias, by removing constant depth bias and substituting it with slope-scale bias.
- Fix the default stencil values when a material is created from a SSS ShaderGraph.
- Tweak test asset to be compatible with XR: unlit SG material for canvas and double-side font material
- Slightly tweaked the behaviour of bloom when resolution is low to reduce artifacts.
- Hidden fields in Light Inspector that is not relevant while in BakingOnly mode.
- Changed parametrization of PCSS, now softness is derived from angular diameter (for directional lights) or shape radius (for point/spot lights) and min filter size is now in the [0..1] range.
- Moved the copy of the geometry history buffers to right after the depth mip chain generation.
- Rename "Luminance" to "Nits" in UX for physical light unit
- Rename FrameSettings "SkyLighting" to "SkyReflection"
- Reworked XR automated tests
- The ray traced screen space shadow history for directional, spot and point lights is discarded if the light transform has changed.
- Changed the behavior for ray tracing in case a mesh renderer has both transparent and opaque submeshes.
- Improve history buffer management
- Replaced PlayerSettings.virtualRealitySupported with XRGraphics.tryEnable.
- Remove redundant FrameSettings RealTimePlanarReflection
- Improved a bit the GC calls generated during the rendering.
- Material update is now only triggered when the relevant settings are touched in the shader graph master nodes
- Changed the way Sky Intensity (on Sky volume components) is handled. It's now a combo box where users can choose between Exposure, Multiplier or Lux (for HDRI sky only) instead of both multiplier and exposure being applied all the time. Added a new menu item to convert old profiles.
- Change how method for specular occlusions is decided on inspector shader (Lit, LitTesselation, LayeredLit, LayeredLitTessellation)
- Unlocked SSS, SSR, Motion Vectors and Distortion frame settings for reflections probes.
- Hide unused LOD settings in Quality Settings legacy window.
- Reduced the constrained distance for temporal reprojection of ray tracing denoising
- Removed shadow near plane from the Directional Light Shadow UI.
- Improved the performances of custom pass culling.
- The scene view camera now replicates the physical parameters from the camera tagged as "MainCamera".
- Reduced the number of GC.Alloc calls, one simple scene without plarnar / probes, it should be 0B.
- Renamed ProfilingSample to ProfilingScope and unified API. Added GPU Timings.
- Updated macros to be compatible with the new shader preprocessor.
- Ray tracing reflection temporal filtering is now done in pre-exposed space
- Search field selects the appropriate fields in both project settings panels 'HDRP Default Settings' and 'Quality/HDRP'
- Disabled the refraction and transmission map keywords if the material is opaque.
- Keep celestial bodies outside the atmosphere.
- Updated the MSAA documentation to specify what features HDRP supports MSAA for and what features it does not.
- Shader use for Runtime Debug Display are now correctly stripper when doing a release build
- Now each camera has its own Volume Stack. This allows Volume Parameters to be updated as early as possible and be ready for the whole frame without conflicts between cameras.
- Disable Async for SSR, SSAO and Contact shadow when aggregated ray tracing frame setting is on.
- Improved performance when entering play mode without domain reload by a factor of ~25
- Renamed the camera profiling sample to include the camera name
- Discarding the ray tracing history for AO, reflection, diffuse shadows and GI when the viewport size changes.
- Renamed the camera profiling sample to include the camera name
- Renamed the post processing graphic formats to match the new convention.
- The restart in Wizard for DXR will always be last fix from now on
- Refactoring pre-existing materials to share more shader code between rasterization and ray tracing.
- Setting a material's Refraction Model to Thin does not overwrite the Thickness and Transmission Absorption Distance anymore.
- Removed Wind textures from runtime as wind is no longer built into the pipeline
- Changed Shader Graph titles of master nodes to be more easily searchable ("HDRP/x" -> "x (HDRP)")
- Expose StartSinglePass() and StopSinglePass() as public interface for XRPass
- Replaced the Texture array for 2D cookies (spot, area and directional lights) and for planar reflections by an atlas.
- Moved the tier defining from the asset to the concerned volume components.
- Changing from a tier management to a "mode" management for reflection and GI and removing the ability to enable/disable deferred and ray bining (they are now implied by performance mode)
- The default FrameSettings for ScreenSpaceShadows is set to true for Camera in order to give a better workflow for DXR.
- Refactor internal usage of Stencil bits.
- Changed how the material upgrader works and added documentation for it.
- Custom passes now disable the stencil when overwriting the depth and not writing into it.
- Renamed the camera profiling sample to include the camera name
- Changed the way the shadow casting property of transparent and tranmissive materials is handeled for ray tracing.
- Changed inspector materials stencil setting code to have more sharing.
- Updated the default scene and default DXR scene and DefaultVolumeProfile.
- Changed the way the length parameter is used for ray traced contact shadows.
- Improved the coherency of PCSS blur between cascades.
- Updated VR checks in Wizard to reflect new XR System.
- Removing unused alpha threshold depth prepass and post pass for fabric shader graph.
- Transform result from CIE XYZ to sRGB color space in EvalSensitivity for iridescence.
- Moved BeginCameraRendering callback right before culling.
- Changed the visibility of the Indirect Lighting Controller component to public.
- Renamed the cubemap used for diffuse convolution to a more explicit name for the memory profiler.
- Improved behaviour of transmission color on transparent surfaces in path tracing.
- Light dimmer can now get values higher than one and was renamed to multiplier in the UI.
- Removed info box requesting volume component for Visual Environment and updated the documentation with the relevant information.
- Improved light selection oracle for light sampling in path tracing.
- Stripped ray tracing subsurface passes with ray tracing is not enabled.
- Remove LOD cross fade code for ray tracing shaders
- Removed legacy VR code
- Add range-based clipping to box lights (case 1178780)
- Improve area light culling (case 1085873)
- Light Hierarchy debug mode can now adjust Debug Exposure for visualizing high exposure scenes.
- Rejecting history for ray traced reflections based on a threshold evaluated on the neighborhood of the sampled history.
- Renamed "Environment" to "Reflection Probes" in tile/cluster debug menu.
- Utilities namespace is obsolete, moved its content to UnityEngine.Rendering (case 1204677)
- Obsolete Utilities namespace was removed, instead use UnityEngine.Rendering (case 1204677)
- Moved most of the compute shaders to the multi_compile API instead of multiple kernels.
- Use multi_compile API for deferred compute shader with shadow mask.
- Remove the raytracing rendering queue system to make recursive raytraced material work when raytracing is disabled
- Changed a few resources used by ray tracing shaders to be global resources (using register space1) for improved CPU performance.
- All custom pass volumes are now executed for one injection point instead of the first one.
- Hidden unsupported choice in emission in Materials
- Temporal Anti aliasing improvements.
- Optimized PrepareLightsForGPU (cost reduced by over 25%) and PrepareGPULightData (around twice as fast now).
- Moved scene view camera settings for HDRP from the preferences window to the scene view camera settings window.
- Updated shaders to be compatible with Microsoft's DXC.
- Debug exposure in debug menu have been replace to debug exposure compensation in EV100 space and is always visible.
- Further optimized PrepareLightsForGPU (3x faster with few shadows, 1.4x faster with a lot of shadows or equivalently cost reduced by 68% to 37%).
- Raytracing: Replaced the DIFFUSE_LIGHTING_ONLY multicompile by a uniform.
- Raytracing: Removed the dynamic lightmap multicompile.
- Raytracing: Remove the LOD cross fade multi compile for ray tracing.
- Cookie are now supported in lightmaper. All lights casting cookie and baked will now include cookie influence.
- Avoid building the mip chain a second time for SSR for transparent objects.
- Replaced "High Quality" Subsurface Scattering with a set of Quality Levels.
- Replaced "High Quality" Volumetric Lighting with "Screen Resolution Percentage" and "Volume Slice Count" on the Fog volume component.
- Merged material samples and shader samples
- Update material samples scene visuals
- Use multi_compile API for deferred compute shader with shadow mask.
- Made the StaticLightingSky class public so that users can change it by script for baking purpose.
- Shadowmask and realtime reflectoin probe property are hide in Quality settings
- Improved performance of reflection probe management when using a lot of probes.
- Ignoring the disable SSR flags for recursive rendering.
- Removed logic in the UI to disable parameters for contact shadows and fog volume components as it was going against the concept of the volume system.
- Fixed the sub surface mask not being taken into account when computing ray traced sub surface scattering.
- MSAA Within Forward Frame Setting is now enabled by default on Cameras when new Render Pipeline Asset is created
- Slightly changed the TAA anti-flicker mechanism so that it is more aggressive on almost static images (only on High preset for now).
- Changed default exposure compensation to 0.
- Refactored shadow caching system.
- Removed experimental namespace for ray tracing code.
- Increase limit for max numbers of lights in UX
- Removed direct use of BSDFData in the path tracing pass, delegated to the material instead.
- Pre-warm the RTHandle system to reduce the amount of memory allocations and the total memory needed at all points.
- DXR: Only read the geometric attributes that are required using the share pass info and shader graph defines.
- DXR: Dispatch binned rays in 1D instead of 2D.
- Lit and LayeredLit tessellation cross lod fade don't used dithering anymore between LOD but fade the tessellation height instead. Allow a smoother transition
- Changed the way planar reflections are filtered in order to be a bit more "physically based".
- Increased path tracing BSDFs roughness range from [0.001, 0.999] to [0.00001, 0.99999].
- Changing the default SSGI radius for the all configurations.
- Changed the default parameters for quality RTGI to match expected behavior.
- Add color clear pass while rendering XR occlusion mesh to avoid leaks.
- Only use one texture for ray traced reflection upscaling.
- Adjust the upscale radius based on the roughness value.
- DXR: Changed the way the filter size is decided for directional, point and spot shadows.
- Changed the default exposure mode to "Automatic (Histogram)", along with "Limit Min" to -4 and "Limit Max" to 16.
- Replaced the default scene system with the builtin Scene Template feature.
- Changed extensions of shader CAS include files.
- Making the planar probe atlas's format match the color buffer's format.
- Removing the planarReflectionCacheCompressed setting from asset.
- SHADERPASS for TransparentDepthPrepass and TransparentDepthPostpass identification is using respectively SHADERPASS_TRANSPARENT_DEPTH_PREPASS and SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
- Moved the Parallax Occlusion Mapping node into Shader Graph.
- Renamed the debug name from SSAO to ScreenSpaceAmbientOcclusion (1254974).
- Added missing tooltips and improved the UI of the aperture control (case 1254916).
- Fixed wrong tooltips in the Dof Volume (case 1256641).
- The `CustomPassLoadCameraColor` and `CustomPassSampleCameraColor` functions now returns the correct color buffer when used in after post process instead of the color pyramid (which didn't had post processes).
- PBR Sky now doesn't go black when going below sea level, but it instead freezes calculation as if on the horizon.
- Fixed an issue with quality setting foldouts not opening when clicking on them (1253088).
- Shutter speed can now be changed by dragging the mouse over the UI label (case 1245007).
- Remove the 'Point Cube Size' for cookie, use the Cubemap size directly.
- VFXTarget with Unlit now allows EmissiveColor output to be consistent with HDRP unlit.
- Only building the RTAS if there is an effect that will require it (1262217).
- Fixed the first ray tracing frame not having the light cluster being set up properly (1260311).
- Render graph pre-setup for ray traced ambient occlusion.
- Avoid casting multiple rays and denoising for hard directional, point and spot ray traced shadows (1261040).
- Making sure the preview cameras do not use ray tracing effects due to a by design issue to build ray tracing acceleration structures (1262166).
- Preparing ray traced reflections for the render graph support (performance and quality).
- Preparing recursive rendering for the render graph port.
- Preparation pass for RTGI, temporal filter and diffuse denoiser for render graph.
- Updated the documentation for the DXR implementation.
- Changed the DXR wizard to support optional checks.
- Changed the DXR wizard steps.
- Preparation pass for RTSSS to be supported by render graph.
- Changed the color space of EmissiveColorLDR property on all shader. Was linear but should have been sRGB. Auto upgrade script handle the conversion.

## [7.1.1] - 2019-09-05

### Added
- Transparency Overdraw debug mode. Allows to visualize transparent objects draw calls as an "heat map".
- Enabled single-pass instancing support for XR SDK with new API cmd.SetInstanceMultiplier()
- XR settings are now available in the HDRP asset
- Support for Material Quality in Shader Graph
- Material Quality support selection in HDRP Asset
- Renamed XR shader macro from UNITY_STEREO_ASSIGN_COMPUTE_EYE_INDEX to UNITY_XR_ASSIGN_VIEW_INDEX
- Raytracing ShaderGraph node for HDRP shaders
- Custom passes volume component with 3 injection points: Before Rendering, Before Transparent and Before Post Process
- Alpha channel is now properly exported to camera render textures when using FP16 color buffer format
- Support for XR SDK mirror view modes
- HD Master nodes in Shader Graph now support Normal and Tangent modification in vertex stage.
- DepthOfFieldCoC option in the fullscreen debug modes.
- Added override Ambient Occlusion option on debug windows
- Added Custom Post Processes with 3 injection points: Before Transparent, Before Post Process and After Post Process
- Added draft of minimal interactive path tracing (experimental) based on DXR API - Support only 4 area light, lit and unlit shader (non-shadergraph)
- Small adjustments to TAA anti flicker (more aggressive on high values).

### Fixed
- Fixed wizard infinite loop on cancellation
- Fixed with compute shader error about too many threads in threadgroup on low GPU
- Fixed invalid contact shadow shaders being created on metal
- Fixed a bug where if Assembly.GetTypes throws an exception due to mis-versioned dlls, then no preprocessors are used in the shader stripper
- Fixed typo in AXF decal property preventing to compile
- Fixed reflection probe with XR single-pass and FPTL
- Fixed force gizmo shown when selecting camera in hierarchy
- Fixed issue with XR occlusion mesh and dynamic resolution
- Fixed an issue where lighting compute buffers were re-created with the wrong size when resizing the window, causing tile artefacts at the top of the screen.
- Fix FrameSettings names and tooltips
- Fixed error with XR SDK when the Editor is not in focus
- Fixed errors with RenderGraph, XR SDK and occlusion mesh
- Fixed shadow routines compilation errors when "real" type is a typedef on "half".
- Fixed toggle volumetric lighting in the light UI
- Fixed post-processing history reset handling rt-scale incorrectly
- Fixed crash with terrain and XR multi-pass
- Fixed ShaderGraph material synchronization issues
- Fixed a null reference exception when using an Emissive texture with Unlit shader (case 1181335)
- Fixed an issue where area lights and point lights where not counted separately with regards to max lights on screen (case 1183196)
- Fixed an SSR and Subsurface Scattering issue (appearing black) when using XR.

### Changed
- Update Wizard layout.
- Remove almost all Garbage collection call within a frame.
- Rename property AdditionalVeclocityChange to AddPrecomputeVelocity
- Call the End/Begin camera rendering callbacks for camera with customRender enabled
- Changeg framesettings migration order of postprocess flags as a pr for reflection settings flags have been backported to 2019.2
- Replaced usage of ENABLE_VR in XRSystem.cs by version defines based on the presence of the built-in VR and XR modules
- Added an update virtual function to the SkyRenderer class. This is called once per frame. This allows a given renderer to amortize heavy computation at the rate it chooses. Currently only the physically based sky implements this.
- Removed mandatory XRPass argument in HDCamera.GetOrCreate()
- Restored the HDCamera parameter to the sky rendering builtin parameters.
- Removed usage of StructuredBuffer for XR View Constants
- Expose Direct Specular Lighting control in FrameSettings
- Deprecated ExponentialFog and VolumetricFog volume components. Now there is only one exponential fog component (Fog) which can add Volumetric Fog as an option. Added a script in Edit -> Render Pipeline -> Upgrade Fog Volume Components.

## [7.0.1] - 2019-07-25

### Added
- Added option in the config package to disable globally Area Lights and to select shadow quality settings for the deferred pipeline.
- When shader log stripping is enabled, shader stripper statistics will be written at `Temp/shader-strip.json`
- Occlusion mesh support from XR SDK

### Fixed
- Fixed XR SDK mirror view blit, cleanup some XRTODO and removed XRDebug.cs
- Fixed culling for volumetrics with XR single-pass rendering
- Fix shadergraph material pass setup not called
- Fixed documentation links in component's Inspector header bar
- Cookies using the render texture output from a camera are now properly updated
- Allow in ShaderGraph to enable pre/post pass when the alpha clip is disabled

### Changed
- RenderQueue for Opaque now start at Background instead of Geometry.
- Clamp the area light size for scripting API when we change the light type
- Added a warning in the material UI when the diffusion profile assigned is not in the HDRP asset


## [7.0.0] - 2019-07-17

### Added
- `Fixed`, `Viewer`, and `Automatic` modes to compute the FOV used when rendering a `PlanarReflectionProbe`
- A checkbox to toggle the chrome gizmo of `ReflectionProbe`and `PlanarReflectionProbe`
- Added a Light layer in shadows that allow for objects to cast shadows without being affected by light (and vice versa).
- You can now access ShaderGraph blend states from the Material UI (for example, **Surface Type**, **Sorting Priority**, and **Blending Mode**). This change may break Materials that use a ShaderGraph, to fix them, select **Edit > Render Pipeline > Reset all ShaderGraph Scene Materials BlendStates**. This syncs the blendstates of you ShaderGraph master nodes with the Material properties.
- You can now control ZTest, ZWrite, and CullMode for transparent Materials.
- Materials that use Unlit Shaders or Unlit Master Node Shaders now cast shadows.
- Added an option to enable the ztest on **After Post Process** materials when TAA is disabled.
- Added a new SSAO (based on Ground Truth Ambient Occlusion algorithm) to replace the previous one.
- Added support for shadow tint on light
- BeginCameraRendering and EndCameraRendering callbacks are now called with probes
- Adding option to update shadow maps only On Enable and On Demand.
- Shader Graphs that use time-dependent vertex modification now generate correct motion vectors.
- Added option to allow a custom spot angle for spot light shadow maps.
- Added frame settings for individual post-processing effects
- Added dither transition between cascades for Low and Medium quality settings
- Added single-pass instancing support with XR SDK
- Added occlusion mesh support with XR SDK
- Added support of Alembic velocity to various shaders
- Added support for more than 2 views for single-pass instancing
- Added support for per punctual/directional light min roughness in StackLit
- Added mirror view support with XR SDK
- Added VR verification in HDRPWizard
- Added DXR verification in HDRPWizard
- Added feedbacks in UI of Volume regarding skies
- Cube LUT support in Tonemapping. Cube LUT helpers for external grading are available in the Post-processing Sample package.

### Fixed
- Fixed an issue with history buffers causing effects like TAA or auto exposure to flicker when more than one camera was visible in the editor
- The correct preview is displayed when selecting multiple `PlanarReflectionProbe`s
- Fixed volumetric rendering with camera-relative code and XR stereo instancing
- Fixed issue with flashing cyan due to async compilation of shader when selecting a mesh
- Fix texture type mismatch when the contact shadow are disabled (causing errors on IOS devices)
- Fixed Generate Shader Includes while in package
- Fixed issue when texture where deleted in ShadowCascadeGUI
- Fixed issue in FrameSettingsHistory when disabling a camera several time without enabling it in between.
- Fixed volumetric reprojection with camera-relative code and XR stereo instancing
- Added custom BaseShaderPreprocessor in HDEditorUtils.GetBaseShaderPreprocessorList()
- Fixed compile issue when USE_XR_SDK is not defined
- Fixed procedural sky sun disk intensity for high directional light intensities
- Fixed Decal mip level when using texture mip map streaming to avoid dropping to lowest permitted mip (now loading all mips)
- Fixed deferred shading for XR single-pass instancing after lightloop refactor
- Fixed cluster and material classification debug (material classification now works with compute as pixel shader lighting)
- Fixed IOS Nan by adding a maximun epsilon definition REAL_EPS that uses HALF_EPS when fp16 are used
- Removed unnecessary GC allocation in motion blur code
- Fixed locked UI with advanded influence volume inspector for probes
- Fixed invalid capture direction when rendering planar reflection probes
- Fixed Decal HTILE optimization with platform not supporting texture atomatic (Disable it)
- Fixed a crash in the build when the contact shadows are disabled
- Fixed camera rendering callbacks order (endCameraRendering was being called before the actual rendering)
- Fixed issue with wrong opaque blending settings for After Postprocess
- Fixed issue with Low resolution transparency on PS4
- Fixed a memory leak on volume profiles
- Fixed The Parallax Occlusion Mappping node in shader graph and it's UV input slot
- Fixed lighting with XR single-pass instancing by disabling deferred tiles
- Fixed the Bloom prefiltering pass
- Fixed post-processing effect relying on Unity's random number generator
- Fixed camera flickering when using TAA and selecting the camera in the editor
- Fixed issue with single shadow debug view and volumetrics
- Fixed most of the problems with light animation and timeline
- Fixed indirect deferred compute with XR single-pass instancing
- Fixed a slight omission in anisotropy calculations derived from HazeMapping in StackLit
- Improved stack computation numerical stability in StackLit
- Fix PBR master node always opaque (wrong blend modes for forward pass)
- Fixed TAA with XR single-pass instancing (missing macros)
- Fixed an issue causing Scene View selection wire gizmo to not appear when using HDRP Shader Graphs.
- Fixed wireframe rendering mode (case 1083989)
- Fixed the renderqueue not updated when the alpha clip is modified in the material UI.
- Fixed the PBR master node preview
- Remove the ReadOnly flag on Reflection Probe's cubemap assets during bake when there are no VCS active.
- Fixed an issue where setting a material debug view would not reset the other exclusive modes
- Spot light shapes are now correctly taken into account when baking
- Now the static lighting sky will correctly take the default values for non-overridden properties
- Fixed material albedo affecting the lux meter
- Extra test in deferred compute shading to avoid shading pixels that were not rendered by the current camera (for camera stacking)

### Changed
- Optimization: Reduce the group size of the deferred lighting pass from 16x16 to 8x8
- Replaced HDCamera.computePassCount by viewCount
- Removed xrInstancing flag in RTHandles (replaced by TextureXR.slices and TextureXR.dimensions)
- Refactor the HDRenderPipeline and lightloop code to preprare for high level rendergraph
- Removed the **Back Then Front Rendering** option in the fabric Master Node settings. Enabling this option previously did nothing.
- Changed shader type Real to translate to FP16 precision on some platforms.
- Shader framework refactor: Introduce CBSDF, EvaluateBSDF, IsNonZeroBSDF to replace BSDF functions
- Shader framework refactor:  GetBSDFAngles, LightEvaluation and SurfaceShading functions
- Replace ComputeMicroShadowing by GetAmbientOcclusionForMicroShadowing
- Rename WorldToTangent to TangentToWorld as it was incorrectly named
- Remove SunDisk and Sun Halo size from directional light
- Remove all obsolete wind code from shader
- Renamed DecalProjectorComponent into DecalProjector for API alignment.
- Improved the Volume UI and made them Global by default
- Remove very high quality shadow option
- Change default for shadow quality in Deferred to Medium
- Enlighten now use inverse squared falloff (before was using builtin falloff)
- Enlighten is now deprecated. Please use CPU or GPU lightmaper instead.
- Remove the name in the diffusion profile UI
- Changed how shadow map resolution scaling with distance is computed. Now it uses screen space area rather than light range.
- Updated MoreOptions display in UI
- Moved Display Area Light Emissive Mesh script API functions in the editor namespace
- direct strenght properties in ambient occlusion now affect direct specular as well
- Removed advanced Specular Occlusion control in StackLit: SSAO based SO control is hidden and fixed to behave like Lit, SPTD is the only HQ technique shown for baked SO.
- Shader framework refactor: Changed ClampRoughness signature to include PreLightData access.
- HDRPWizard window is now in Window > General > HD Render Pipeline Wizard
- Moved StaticLightingSky to LightingWindow
- Removes the current "Scene Settings" and replace them with "Sky & Fog Settings" (with Physically Based Sky and Volumetric Fog).
- Changed how cached shadow maps are placed inside the atlas to minimize re-rendering of them.

## [6.7.0-preview] - 2019-05-16

### Added
- Added ViewConstants StructuredBuffer to simplify XR rendering
- Added API to render specific settings during a frame
- Added stadia to the supported platforms (2019.3)
- Enabled cascade blends settings in the HD Shadow component
- Added Hardware Dynamic Resolution support.
- Added MatCap debug view to replace the no scene lighting debug view.
- Added clear GBuffer option in FrameSettings (default to false)
- Added preview for decal shader graph (Only albedo, normal and emission)
- Added exposure weight control for decal
- Screen Space Directional Shadow under a define option. Activated for ray tracing
- Added a new abstraction for RendererList that will help transition to Render Graph and future RendererList API
- Added multipass support for VR
- Added XR SDK integration (multipass only)
- Added Shader Graph samples for Hair, Fabric and Decal master nodes.
- Add fade distance, shadow fade distance and light layers to light explorer
- Add method to draw light layer drawer in a rect to HDEditorUtils

### Fixed
- Fixed deserialization crash at runtime
- Fixed for ShaderGraph Unlit masternode not writing velocity
- Fixed a crash when assiging a new HDRP asset with the 'Verify Saving Assets' option enabled
- Fixed exposure to properly support TEXTURE2D_X
- Fixed TerrainLit basemap texture generation
- Fixed a bug that caused nans when material classification was enabled and a tile contained one standard material + a material with transmission.
- Fixed gradient sky hash that was not using the exposure hash
- Fixed displayed default FrameSettings in HDRenderPipelineAsset wrongly updated on scripts reload.
- Fixed gradient sky hash that was not using the exposure hash.
- Fixed visualize cascade mode with exposure.
- Fixed (enabled) exposure on override lighting debug modes.
- Fixed issue with LightExplorer when volume have no profile
- Fixed issue with SSR for negative, infinite and NaN history values
- Fixed LightLayer in HDReflectionProbe and PlanarReflectionProbe inspector that was not displayed as a mask.
- Fixed NaN in transmission when the thickness and a color component of the scattering distance was to 0
- Fixed Light's ShadowMask multi-edition.
- Fixed motion blur and SMAA with VR single-pass instancing
- Fixed NaNs generated by phase functionsin volumetric lighting
- Fixed NaN issue with refraction effect and IOR of 1 at extreme grazing angle
- Fixed nan tracker not using the exposure
- Fixed sorting priority on lit and unlit materials
- Fixed null pointer exception when there are no AOVRequests defined on a camera
- Fixed dirty state of prefab using disabled ReflectionProbes
- Fixed an issue where gizmos and editor grid were not correctly depth tested
- Fixed created default scene prefab non editable due to wrong file extension.
- Fixed an issue where sky convolution was recomputed for nothing when a preview was visible (causing extreme slowness when fabric convolution is enabled)
- Fixed issue with decal that wheren't working currently in player
- Fixed missing stereo rendering macros in some fragment shaders
- Fixed exposure for ReflectionProbe and PlanarReflectionProbe gizmos
- Fixed single-pass instancing on PSVR
- Fixed Vulkan shader issue with Texture2DArray in ScreenSpaceShadow.compute by re-arranging code (workaround)
- Fixed camera-relative issue with lights and XR single-pass instancing
- Fixed single-pass instancing on Vulkan
- Fixed htile synchronization issue with shader graph decal
- Fixed Gizmos are not drawn in Camera preview
- Fixed pre-exposure for emissive decal
- Fixed wrong values computed in PreIntegrateFGD and in the generation of volumetric lighting data by forcing the use of fp32.
- Fixed NaNs arising during the hair lighting pass
- Fixed synchronization issue in decal HTile that occasionally caused rendering artifacts around decal borders
- Fixed QualitySettings getting marked as modified by HDRP (and thus checked out in Perforce)
- Fixed a bug with uninitialized values in light explorer
- Fixed issue with LOD transition
- Fixed shader warnings related to raytracing and TEXTURE2D_X

### Changed
- Refactor PixelCoordToViewDirWS to be VR compatible and to compute it only once per frame
- Modified the variants stripper to take in account multiple HDRP assets used in the build.
- Improve the ray biasing code to avoid self-intersections during the SSR traversal
- Update Pyramid Spot Light to better match emitted light volume.
- Moved _XRViewConstants out of UnityPerPassStereo constant buffer to fix issues with PSSL
- Removed GetPositionInput_Stereo() and single-pass (double-wide) rendering mode
- Changed label width of the frame settings to accommodate better existing options.
- SSR's Default FrameSettings for camera is now enable.
- Re-enabled the sharpening filter on Temporal Anti-aliasing
- Exposed HDEditorUtils.LightLayerMaskDrawer for integration in other packages and user scripting.
- Rename atmospheric scattering in FrameSettings to Fog
- The size modifier in the override for the culling sphere in Shadow Cascades now defaults to 0.6, which is the same as the formerly hardcoded value.
- Moved LOD Bias and Maximum LOD Level from Frame Setting section `Other` to `Rendering`
- ShaderGraph Decal that affect only emissive, only draw in emissive pass (was drawing in dbuffer pass too)
- Apply decal projector fade factor correctly on all attribut and for shader graph decal
- Move RenderTransparentDepthPostpass after all transparent
- Update exposure prepass to interleave XR single-pass instancing views in a checkerboard pattern
- Removed ScriptRuntimeVersion check in wizard.

## [6.6.0-preview] - 2019-04-01

### Added
- Added preliminary changes for XR deferred shading
- Added support of 111110 color buffer
- Added proper support for Recorder in HDRP
- Added depth offset input in shader graph master nodes
- Added a Parallax Occlusion Mapping node
- Added SMAA support
- Added Homothety and Symetry quick edition modifier on volume used in ReflectionProbe, PlanarReflectionProbe and DensityVolume
- Added multi-edition support for DecalProjectorComponent
- Improve hair shader
- Added the _ScreenToTargetScaleHistory uniform variable to be used when sampling HDRP RTHandle history buffers.
- Added settings in `FrameSettings` to change `QualitySettings.lodBias` and `QualitySettings.maximumLODLevel` during a rendering
- Added an exposure node to retrieve the current, inverse and previous frame exposure value.
- Added an HD scene color node which allow to sample the scene color with mips and a toggle to remove the exposure.
- Added safeguard on HD scene creation if default scene not set in the wizard
- Added Low res transparency rendering pass.

### Fixed
- Fixed HDRI sky intensity lux mode
- Fixed dynamic resolution for XR
- Fixed instance identifier semantic string used by Shader Graph
- Fixed null culling result occuring when changing scene that was causing crashes
- Fixed multi-edition light handles and inspector shapes
- Fixed light's LightLayer field when multi-editing
- Fixed normal blend edition handles on DensityVolume
- Fixed an issue with layered lit shader and height based blend where inactive layers would still have influence over the result
- Fixed multi-selection handles color for DensityVolume
- Fixed multi-edition inspector's blend distances for HDReflectionProbe, PlanarReflectionProbe and DensityVolume
- Fixed metric distance that changed along size in DensityVolume
- Fixed DensityVolume shape handles that have not same behaviour in advance and normal edition mode
- Fixed normal map blending in TerrainLit by only blending the derivatives
- Fixed Xbox One rendering just a grey screen instead of the scene
- Fixed probe handles for multiselection
- Fixed baked cubemap import settings for convolution
- Fixed regression causing crash when attempting to open HDRenderPipelineWizard without an HDRenderPipelineAsset setted
- Fixed FullScreenDebug modes: SSAO, SSR, Contact shadow, Prerefraction Color Pyramid, Final Color Pyramid
- Fixed volumetric rendering with stereo instancing
- Fixed shader warning
- Fixed missing resources in existing asset when updating package
- Fixed PBR master node preview in forward rendering or transparent surface
- Fixed deferred shading with stereo instancing
- Fixed "look at" edition mode of Rotation tool for DecalProjectorComponent
- Fixed issue when switching mode in ReflectionProbe and PlanarReflectionProbe
- Fixed issue where migratable component version where not always serialized when part of prefab's instance
- Fixed an issue where shadow would not be rendered properly when light layer are not enabled
- Fixed exposure weight on unlit materials
- Fixed Light intensity not played in the player when recorded with animation/timeline
- Fixed some issues when multi editing HDRenderPipelineAsset
- Fixed emission node breaking the main shader graph preview in certain conditions.
- Fixed checkout of baked probe asset when baking probes.
- Fixed invalid gizmo position for rotated ReflectionProbe
- Fixed multi-edition of material's SurfaceType and RenderingPath
- Fixed whole pipeline reconstruction on selecting for the first time or modifying other than the currently used HDRenderPipelineAsset
- Fixed single shadow debug mode
- Fixed global scale factor debug mode when scale > 1
- Fixed debug menu material overrides not getting applied to the Terrain Lit shader
- Fixed typo in computeLightVariants
- Fixed deferred pass with XR instancing by disabling ComputeLightEvaluation
- Fixed bloom resolution independence
- Fixed lens dirt intensity not behaving properly
- Fixed the Stop NaN feature
- Fixed some resources to handle more than 2 instanced views for XR
- Fixed issue with black screen (NaN) produced on old GPU hardware or intel GPU hardware with gaussian pyramid
- Fixed issue with disabled punctual light would still render when only directional light is present

### Changed
- DensityVolume scripting API will no longuer allow to change between advance and normal edition mode
- Disabled depth of field, lens distortion and panini projection in the scene view
- TerrainLit shaders and includes are reorganized and made simpler.
- TerrainLit shader GUI now allows custom properties to be displayed in the Terrain fold-out section.
- Optimize distortion pass with stencil
- Disable SceneSelectionPass in shader graph preview
- Control punctual light and area light shadow atlas separately
- Move SMAA anti-aliasing option to after Temporal Anti Aliasing one, to avoid problem with previously serialized project settings
- Optimize rendering with static only lighting and when no cullable lights/decals/density volumes are present.
- Updated handles for DecalProjectorComponent for enhanced spacial position readability and have edition mode for better SceneView management
- DecalProjectorComponent are now scale independent in order to have reliable metric unit (see new Size field for changing the size of the volume)
- Restructure code from HDCamera.Update() by adding UpdateAntialiasing() and UpdateViewConstants()
- Renamed velocity to motion vectors
- Objects rendered during the After Post Process pass while TAA is enabled will not benefit from existing depth buffer anymore. This is done to fix an issue where those object would wobble otherwise
- Removed usage of builtin unity matrix for shadow, shadow now use same constant than other view
- The default volume layer mask for cameras & probes is now `Default` instead of `Everything`

## [6.5.0-preview] - 2019-03-07

### Added
- Added depth-of-field support with stereo instancing
- Adding real time area light shadow support
- Added a new FrameSettings: Specular Lighting to toggle the specular during the rendering

### Fixed
- Fixed diffusion profile upgrade breaking package when upgrading to a new version
- Fixed decals cropped by gizmo not updating correctly if prefab
- Fixed an issue when enabling SSR on multiple view
- Fixed edition of the intensity's unit field while selecting multiple lights
- Fixed wrong calculation in soft voxelization for density volume
- Fixed gizmo not working correctly with pre-exposure
- Fixed issue with setting a not available RT when disabling motion vectors
- Fixed planar reflection when looking at mirror normal
- Fixed mutiselection issue with HDLight Inspector
- Fixed HDAdditionalCameraData data migration
- Fixed failing builds when light explorer window is open
- Fixed cascade shadows border sometime causing artefacts between cascades
- Restored shadows in the Cascade Shadow debug visualization
- `camera.RenderToCubemap` use proper face culling

### Changed
- When rendering reflection probe disable all specular lighting and for metals use fresnelF0 as diffuse color for bake lighting.

## [6.4.0-preview] - 2019-02-21

### Added
- VR: Added TextureXR system to selectively expand TEXTURE2D macros to texture array for single-pass stereo instancing + Convert textures call to these macros
- Added an unit selection dropdown next to shutter speed (camera)
- Added error helpbox when trying to use a sub volume component that require the current HDRenderPipelineAsset to support a feature that it is not supporting.
- Add mesh for tube light when display emissive mesh is enabled

### Fixed
- Fixed Light explorer. The volume explorer used `profile` instead of `sharedProfile` which instantiate a custom volume profile instead of editing the asset itself.
- Fixed UI issue where all is displayed using metric unit in shadow cascade and Percent is set in the unit field (happening when opening the inspector).
- Fixed inspector event error when double clicking on an asset (diffusion profile/material).
- Fixed nullref on layered material UI when the material is not an asset.
- Fixed nullref exception when undo/redo a light property.
- Fixed visual bug when area light handle size is 0.

### Changed
- Update UI for 32bit/16bit shadow precision settings in HDRP asset
- Object motion vectors have been disabled in all but the game view. Camera motion vectors are still enabled everywhere, allowing TAA and Motion Blur to work on static objects.
- Enable texture array by default for most rendering code on DX11 and unlock stereo instancing (DX11 only for now)

## [6.3.0-preview] - 2019-02-18

### Added
- Added emissive property for shader graph decals
- Added a diffusion profile override volume so the list of diffusion profile assets to use can be chanaged without affecting the HDRP asset
- Added a "Stop NaNs" option on cameras and in the Scene View preferences.
- Added metric display option in HDShadowSettings and improve clamping
- Added shader parameter mapping in DebugMenu
- Added scripting API to configure DebugData for DebugMenu

### Fixed
- Fixed decals in forward
- Fixed issue with stencil not correctly setup for various master node and shader for the depth pass, motion vector pass and GBuffer/Forward pass
- Fixed SRP batcher and metal
- Fixed culling and shadows for Pyramid, Box, Rectangle and Tube lights
- Fixed an issue where scissor render state leaking from the editor code caused partially black rendering

### Changed
- When a lit material has a clear coat mask that is not null, we now use the clear coat roughness to compute the screen space reflection.
- Diffusion profiles are now limited to one per asset and can be referenced in materials, shader graphs and vfx graphs. Materials will be upgraded automatically except if they are using a shader graph, in this case it will display an error message.

## [6.2.0-preview] - 2019-02-15

### Added
- Added help box listing feature supported in a given HDRenderPipelineAsset alongs with the drawbacks implied.
- Added cascade visualizer, supporting disabled handles when not overriding.

### Fixed
- Fixed post processing with stereo double-wide
- Fixed issue with Metal: Use sign bit to find the cache type instead of lowest bit.
- Fixed invalid state when creating a planar reflection for the first time
- Fix FrameSettings's LitShaderMode not restrained by supported LitShaderMode regression.

### Changed
- The default value roughness value for the clearcoat has been changed from 0.03 to 0.01
- Update default value of based color for master node
- Update Fabric Charlie Sheen lighting model - Remove Fresnel component that wasn't part of initial model + Remap smoothness to [0.0 - 0.6] range for more artist friendly parameter

### Changed
- Code refactor: all macros with ARGS have been swapped with macros with PARAM. This is because the ARGS macros were incorrectly named.

## [6.1.0-preview] - 2019-02-13

### Added
- Added support for post-processing anti-aliasing in the Scene View (FXAA and TAA). These can be set in Preferences.
- Added emissive property for decal material (non-shader graph)

### Fixed
- Fixed a few UI bugs with the color grading curves.
- Fixed "Post Processing" in the scene view not toggling post-processing effects
- Fixed bake only object with flag `ReflectionProbeStaticFlag` when baking a `ReflectionProbe`

### Changed
- Removed unsupported Clear Depth checkbox in Camera inspector
- Updated the toggle for advanced mode in inspectors.

## [6.0.0-preview] - 2019-02-23

### Added
- Added new API to perform a camera rendering
- Added support for hair master node (Double kajiya kay - Lambert)
- Added Reset behaviour in DebugMenu (ingame mapping is right joystick + B)
- Added Default HD scene at new scene creation while in HDRP
- Added Wizard helping to configure HDRP project
- Added new UI for decal material to allow remapping and scaling of some properties
- Added cascade shadow visualisation toggle in HD shadow settings
- Added icons for assets
- Added replace blending mode for distortion
- Added basic distance fade for density volumes
- Added decal master node for shader graph
- Added HD unlit master node (Cross Pipeline version is name Unlit)
- Added new Rendering Queue in materials
- Added post-processing V3 framework embed in HDRP, remove postprocess V2 framework
- Post-processing now uses the generic volume framework
-   New depth-of-field, bloom, panini projection effects, motion blur
-   Exposure is now done as a pre-exposition pass, the whole system has been revamped
-   Exposure now use EV100 everywhere in the UI (Sky, Emissive Light)
- Added emissive intensity (Luminance and EV100 control) control for Emissive
- Added pre-exposure weigth for Emissive
- Added an emissive color node and a slider to control the pre-exposure percentage of emission color
- Added physical camera support where applicable
- Added more color grading tools
- Added changelog level for Shader Variant stripping
- Added Debug mode for validation of material albedo and metalness/specularColor values
- Added a new dynamic mode for ambient probe and renamed BakingSky to StaticLightingSky
- Added command buffer parameter to all Bind() method of material
- Added Material validator in Rendering Debugger
- Added code to future support of DXR (not enabled)
- Added support of multiviewport
- Added HDRenderPipeline.RequestSkyEnvironmentUpdate function to force an update from script when sky is set to OnDemand
- Added a Lighting and BackLighting slots in Lit, StackLit, Fabric and Hair master nodes
- Added support for overriding terrain detail rendering shaders, via the render pipeline editor resources asset
- Added xrInstancing flag support to RTHandle
- Added support for cullmask for decal projectors
- Added software dynamic resolution support
- Added support for "After Post-Process" render pass for unlit shader
- Added support for textured rectangular area lights
- Added stereo instancing macros to MSAA shaders
- Added support for Quarter Res Raytraced Reflections (not enabled)
- Added fade factor for decal projectors.
- Added stereo instancing macros to most shaders used in VR
- Added multi edition support for HDRenderPipelineAsset

### Fixed
- Fixed logic to disable FPTL with stereo rendering
- Fixed stacklit transmission and sun highlight
- Fixed decals with stereo rendering
- Fixed sky with stereo rendering
- Fixed flip logic for postprocessing + VR
- Fixed copyStencilBuffer pass for some specific platforms
- Fixed point light shadow map culling that wasn't taking into account far plane
- Fixed usage of SSR with transparent on all master node
- Fixed SSR and microshadowing on fabric material
- Fixed blit pass for stereo rendering
- Fixed lightlist bounds for stereo rendering
- Fixed windows and in-game DebugMenu sync.
- Fixed FrameSettings' LitShaderMode sync when opening DebugMenu.
- Fixed Metal specific issues with decals, hitting a sampler limit and compiling AxF shader
- Fixed an issue with flipped depth buffer during postprocessing
- Fixed normal map use for shadow bias with forward lit - now use geometric normal
- Fixed transparent depth prepass and postpass access so they can be use without alpha clipping for lit shader
- Fixed support of alpha clip shadow for lit master node
- Fixed unlit master node not compiling
- Fixed issue with debug display of reflection probe
- Fixed issue with phong tessellations not working with lit shader
- Fixed issue with vertex displacement being affected by heightmap setting even if not heightmap where assign
- Fixed issue with density mode on Lit terrain producing NaN
- Fixed issue when going back and forth from Lit to LitTesselation for displacement mode
- Fixed issue with ambient occlusion incorrectly applied to emissiveColor with light layers in deferred
- Fixed issue with fabric convolution not using the correct convolved texture when fabric convolution is enabled
- Fixed issue with Thick mode for Transmission that was disabling transmission with directional light
- Fixed shutdown edge cases with HDRP tests
- Fixed slowdow when enabling Fabric convolution in HDRP asset
- Fixed specularAA not compiling in StackLit Master node
- Fixed material debug view with stereo rendering
- Fixed material's RenderQueue edition in default view.
- Fixed banding issues within volumetric density buffer
- Fixed missing multicompile for MSAA for AxF
- Fixed camera-relative support for stereo rendering
- Fixed remove sync with render thread when updating decal texture atlas.
- Fixed max number of keyword reach [256] issue. Several shader feature are now local
- Fixed Scene Color and Depth nodes
- Fixed SSR in forward
- Fixed custom editor of Unlit, HD Unlit and PBR shader graph master node
- Fixed issue with NewFrame not correctly calculated in Editor when switching scene
- Fixed issue with TerrainLit not compiling with depth only pass and normal buffer
- Fixed geometric normal use for shadow bias with PBR master node in forward
- Fixed instancing macro usage for decals
- Fixed error message when having more than one directional light casting shadow
- Fixed error when trying to display preview of Camera or PlanarReflectionProbe
- Fixed LOAD_TEXTURE2D_ARRAY_MSAA macro
- Fixed min-max and amplitude clamping value in inspector of vertex displacement materials
- Fixed issue with alpha shadow clip (was incorrectly clipping object shadow)
- Fixed an issue where sky cubemap would not be cleared correctly when setting the current sky to None
- Fixed a typo in Static Lighting Sky component UI
- Fixed issue with incorrect reset of RenderQueue when switching shader in inspector GUI
- Fixed issue with variant stripper stripping incorrectly some variants
- Fixed a case of ambient lighting flickering because of previews
- Fixed Decals when rendering multiple camera in a single frame
- Fixed cascade shadow count in shader
- Fixed issue with Stacklit shader with Haze effect
- Fixed an issue with the max sample count for the TAA
- Fixed post-process guard band for XR
- Fixed exposure of emissive of Unlit
- Fixed depth only and motion vector pass for Unlit not working correctly with MSAA
- Fixed an issue with stencil buffer copy causing unnecessary compute dispatches for lighting
- Fixed multi edition issue in FrameSettings
- Fixed issue with SRP batcher and DebugDisplay variant of lit shader
- Fixed issue with debug material mode not doing alpha test
- Fixed "Attempting to draw with missing UAV bindings" errors on Vulkan
- Fixed pre-exposure incorrectly apply to preview
- Fixed issue with duplicate 3D texture in 3D texture altas of volumetric?
- Fixed Camera rendering order (base on the depth parameter)
- Fixed shader graph decals not being cropped by gizmo
- Fixed "Attempting to draw with missing UAV bindings" errors on Vulkan.


### Changed
- ColorPyramid compute shader passes is swapped to pixel shader passes on platforms where the later is faster.
- Removing the simple lightloop used by the simple lit shader
- Whole refactor of reflection system: Planar and reflection probe
- Separated Passthrough from other RenderingPath
- Update several properties naming and caption based on feedback from documentation team
- Remove tile shader variant for transparent backface pass of lit shader
- Rename all HDRenderPipeline to HDRP folder for shaders
- Rename decal property label (based on doc team feedback)
- Lit shader mode now default to Deferred to reduce build time
- Update UI of Emission parameters in shaders
- Improve shader variant stripping including shader graph variant
- Refactored render loop to render realtime probes visible per camera
- Enable SRP batcher by default
- Shader code refactor: Rename LIGHTLOOP_SINGLE_PASS => LIGHTLOOP_DISABLE_TILE_AND_CLUSTER and clean all usage of LIGHTLOOP_TILE_PASS
- Shader code refactor: Move pragma definition of vertex and pixel shader inside pass + Move SURFACE_GRADIENT definition in XXXData.hlsl
- Micro-shadowing in Lit forward now use ambientOcclusion instead of SpecularOcclusion
- Upgraded FrameSettings workflow, DebugMenu and Inspector part relative to it
- Update build light list shader code to support 32 threads in wavefronts on some platforms
- LayeredLit layers' foldout are now grouped in one main foldout per layer
- Shadow alpha clip can now be enabled on lit shader and haor shader enven for opaque
- Temporal Antialiasing optimization for Xbox One X
- Parameter depthSlice on SetRenderTarget functions now defaults to -1 to bind the entire resource
- Rename SampleCameraDepth() functions to LoadCameraDepth() and SampleCameraDepth(), same for SampleCameraColor() functions
- Improved Motion Blur quality.
- Update stereo frame settings values for single-pass instancing and double-wide
- Rearrange FetchDepth functions to prepare for stereo-instancing
- Remove unused _ComputeEyeIndex
- Updated HDRenderPipelineAsset inspector
- Re-enable SRP batcher for metal
- Updated Frame Settings UX in the HDRP Settings and Camera

## [5.2.0-preview] - 2018-11-27

### Added
- Added option to run Contact Shadows and Volumetrics Voxelization stage in Async Compute
- Added camera freeze debug mode - Allow to visually see culling result for a camera
- Added support of Gizmo rendering before and after postprocess in Editor
- Added support of LuxAtDistance for punctual lights

### Fixed
- Fixed Debug.DrawLine and Debug.Ray call to work in game view
- Fixed DebugMenu's enum resetted on change
- Fixed divide by 0 in refraction causing NaN
- Fixed disable rough refraction support
- Fixed refraction, SSS and atmospheric scattering for VR
- Fixed forward clustered lighting for VR (double-wide).
- Fixed Light's UX to not allow negative intensity
- Fixed HDRenderPipelineAsset inspector broken when displaying its FrameSettings from project windows.
- Fixed forward clustered lighting for VR (double-wide).
- Fixed HDRenderPipelineAsset inspector broken when displaying its FrameSettings from project windows.
- Fixed Decals and SSR diable flags for all shader graph master node (Lit, Fabric, StackLit, PBR)
- Fixed Distortion blend mode for shader graph master node (Lit, StackLit)
- Fixed bent Normal for Fabric master node in shader graph
- Fixed PBR master node lightlayers
- Fixed shader stripping for built-in lit shaders.

### Changed
- Rename "Regular" in Diffusion profile UI "Thick Object"
- Changed VBuffer depth parametrization for volumetric from distanceRange to depthExtent - Require update of volumetric settings - Fog start at near plan
- SpotLight with box shape use Lux unit only

## [5.1.0-preview] - 2018-11-19

### Added

- Added a separate Editor resources file for resources Unity does not take when it builds a Player.
- You can now disable SSR on Materials in Shader Graph.
- Added support for MSAA when the Supported Lit Shader Mode is set to Both. Previously HDRP only supported MSAA for Forward mode.
- You can now override the emissive color of a Material when in debug mode.
- Exposed max light for Light Loop Settings in HDRP asset UI.
- HDRP no longer performs a NormalDBuffer pass update if there are no decals in the Scene.
- Added distant (fall-back) volumetric fog and improved the fog evaluation precision.
- Added an option to reflect sky in SSR.
- Added a y-axis offset for the PlanarReflectionProbe and offset tool.
- Exposed the option to run SSR and SSAO on async compute.
- Added support for the _GlossMapScale parameter in the Legacy to HDRP Material converter.
- Added wave intrinsic instructions for use in Shaders (for AMD GCN).


### Fixed
- Fixed sphere shaped influence handles clamping in Reflection Probes.
- Fixed Reflection Probe data migration for projects created before using HDRP.
- Fixed UI of Layered Material where Unity previously rendered the scrollbar above the Copy button.
- Fixed Material tessellations parameters Start fade distance and End fade distance. Originally, Unity clamped these values when you modified them.
- Fixed various distortion and refraction issues - handle a better fall-back.
- Fixed SSR for multiple views.
- Fixed SSR issues related to self-intersections.
- Fixed shape density volume handle speed.
- Fixed density volume shape handle moving too fast.
- Fixed the Camera velocity pass that we removed by mistake.
- Fixed some null pointer exceptions when disabling motion vectors support.
- Fixed viewports for both the Subsurface Scattering combine pass and the transparent depth prepass.
- Fixed the blend mode pop-up in the UI. It previously did not appear when you enabled pre-refraction.
- Fixed some null pointer exceptions that previously occurred when you disabled motion vectors support.
- Fixed Layered Lit UI issue with scrollbar.
- Fixed cubemap assignation on custom ReflectionProbe.
- Fixed Reflection Probes’ capture settings' shadow distance.
- Fixed an issue with the SRP batcher and Shader variables declaration.
- Fixed thickness and subsurface slots for fabric Shader master node that wasn't appearing with the right combination of flags.
- Fixed d3d debug layer warning.
- Fixed PCSS sampling quality.
- Fixed the Subsurface and transmission Material feature enabling for fabric Shader.
- Fixed the Shader Graph UV node’s dimensions when using it in a vertex Shader.
- Fixed the planar reflection mirror gizmo's rotation.
- Fixed HDRenderPipelineAsset's FrameSettings not showing the selected enum in the Inspector drop-down.
- Fixed an error with async compute.
- MSAA now supports transparency.
- The HDRP Material upgrader tool now converts metallic values correctly.
- Volumetrics now render in Reflection Probes.
- Fixed a crash that occurred whenever you set a viewport size to 0.
- Fixed the Camera physic parameter that the UI previously did not display.
- Fixed issue in pyramid shaped spotlight handles manipulation

### Changed

- Renamed Line shaped Lights to Tube Lights.
- HDRP now uses mean height fog parametrization.
- Shadow quality settings are set to All when you use HDRP (This setting is not visible in the UI when using SRP). This avoids Legacy Graphics Quality Settings disabling the shadows and give SRP full control over the Shadows instead.
- HDRP now internally uses premultiplied alpha for all fog.
- Updated default FrameSettings used for realtime Reflection Probes when you create a new HDRenderPipelineAsset.
- Remove multi-camera support. LWRP and HDRP will not support multi-camera layered rendering.
- Updated Shader Graph subshaders to use the new instancing define.
- Changed fog distance calculation from distance to plane to distance to sphere.
- Optimized forward rendering using AMD GCN by scalarizing the light loop.
- Changed the UI of the Light Editor.
- Change ordering of includes in HDRP Materials in order to reduce iteration time for faster compilation.
- Added a StackLit master node replacing the InspectorUI version. IMPORTANT: All previously authored StackLit Materials will be lost. You need to recreate them with the master node.

## [5.0.0-preview] - 2018-09-28

### Added
- Added occlusion mesh to depth prepass for VR (VR still disabled for now)
- Added a debug mode to display only one shadow at once
- Added controls for the highlight created by directional lights
- Added a light radius setting to punctual lights to soften light attenuation and simulate fill lighting
- Added a 'minRoughness' parameter to all non-area lights (was previously only available for certain light types)
- Added separate volumetric light/shadow dimmers
- Added per-pixel jitter to volumetrics to reduce aliasing artifacts
- Added a SurfaceShading.hlsl file, which implements material-agnostic shading functionality in an efficient manner
- Added support for shadow bias for thin object transmission
- Added FrameSettings to control realtime planar reflection
- Added control for SRPBatcher on HDRP Asset
- Added an option to clear the shadow atlases in the debug menu
- Added a color visualization of the shadow atlas rescale in debug mode
- Added support for disabling SSR on materials
- Added intrinsic for XBone
- Added new light volume debugging tool
- Added a new SSR debug view mode
- Added translaction's scale invariance on DensityVolume
- Added multiple supported LitShadermode and per renderer choice in case of both Forward and Deferred supported
- Added custom specular occlusion mode to Lit Shader Graph Master node

### Fixed
- Fixed a normal bias issue with Stacklit (Was causing light leaking)
- Fixed camera preview outputing an error when both scene and game view where display and play and exit was call
- Fixed override debug mode not apply correctly on static GI
- Fixed issue where XRGraphicsConfig values set in the asset inspector GUI weren't propagating correctly (VR still disabled for now)
- Fixed issue with tangent that was using SurfaceGradient instead of regular normal decoding
- Fixed wrong error message display when switching to unsupported target like IOS
- Fixed an issue with ambient occlusion texture sometimes not being created properly causing broken rendering
- Shadow near plane is no longer limited at 0.1
- Fixed decal draw order on transparent material
- Fixed an issue where sometime the lookup texture used for GGX convolution was broken, causing broken rendering
- Fixed an issue where you wouldn't see any fog for certain pipeline/scene configurations
- Fixed an issue with volumetric lighting where the anisotropy value of 0 would not result in perfectly isotropic lighting
- Fixed shadow bias when the atlas is rescaled
- Fixed shadow cascade sampling outside of the atlas when cascade count is inferior to 4
- Fixed shadow filter width in deferred rendering not matching shader config
- Fixed stereo sampling of depth texture in MSAA DepthValues.shader
- Fixed box light UI which allowed negative and zero sizes, thus causing NaNs
- Fixed stereo rendering in HDRISky.shader (VR)
- Fixed normal blend and blend sphere influence for reflection probe
- Fixed distortion filtering (was point filtering, now trilinear)
- Fixed contact shadow for large distance
- Fixed depth pyramid debug view mode
- Fixed sphere shaped influence handles clamping in reflection probes
- Fixed reflection probes data migration for project created before using hdrp
- Fixed ambient occlusion for Lit Master Node when slot is connected

### Changed
- Use samplerunity_ShadowMask instead of samplerunity_samplerLightmap for shadow mask
- Allow to resize reflection probe gizmo's size
- Improve quality of screen space shadow
- Remove support of projection model for ScreenSpaceLighting (SSR always use HiZ and refraction always Proxy)
- Remove all the debug mode from SSR that are obsolete now
- Expose frameSettings and Capture settings for reflection and planar probe
- Update UI for reflection probe, planar probe, camera and HDRP Asset
- Implement proper linear blending for volumetric lighting via deep compositing as described in the paper "Deep Compositing Using Lie Algebras"
- Changed  planar mapping to match terrain convention (XZ instead of ZX)
- XRGraphicsConfig is no longer Read/Write. Instead, it's read-only. This improves consistency of XR behavior between the legacy render pipeline and SRP
- Change reflection probe data migration code (to update old reflection probe to new one)
- Updated gizmo for ReflectionProbes
- Updated UI and Gizmo of DensityVolume

## [4.0.0-preview] - 2018-09-28

### Added
- Added a new TerrainLit shader that supports rendering of Unity terrains.
- Added controls for linear fade at the boundary of density volumes
- Added new API to control decals without monobehaviour object
- Improve Decal Gizmo
- Implement Screen Space Reflections (SSR) (alpha version, highly experimental)
- Add an option to invert the fade parameter on a Density Volume
- Added a Fabric shader (experimental) handling cotton and silk
- Added support for MSAA in forward only for opaque only
- Implement smoothness fade for SSR
- Added support for AxF shader (X-rite format - require special AxF importer from Unity not part of HDRP)
- Added control for sundisc on directional light (hack)
- Added a new HD Lit Master node that implements Lit shader support for Shader Graph
- Added Micro shadowing support (hack)
- Added an event on HDAdditionalCameraData for custom rendering
- HDRP Shader Graph shaders now support 4-channel UVs.

### Fixed
- Fixed an issue where sometimes the deferred shadow texture would not be valid, causing wrong rendering.
- Stencil test during decals normal buffer update is now properly applied
- Decals corectly update normal buffer in forward
- Fixed a normalization problem in reflection probe face fading causing artefacts in some cases
- Fix multi-selection behavior of Density Volumes overwriting the albedo value
- Fixed support of depth texture for RenderTexture. HDRP now correctly output depth to user depth buffer if RenderTexture request it.
- Fixed multi-selection behavior of Density Volumes overwriting the albedo value
- Fixed support of depth for RenderTexture. HDRP now correctly output depth to user depth buffer if RenderTexture request it.
- Fixed support of Gizmo in game view in the editor
- Fixed gizmo for spot light type
- Fixed issue with TileViewDebug mode being inversed in gameview
- Fixed an issue with SAMPLE_TEXTURECUBE_SHADOW macro
- Fixed issue with color picker not display correctly when game and scene view are visible at the same time
- Fixed an issue with reflection probe face fading
- Fixed camera motion vectors shader and associated matrices to update correctly for single-pass double-wide stereo rendering
- Fixed light attenuation functions when range attenuation is disabled
- Fixed shadow component algorithm fixup not dirtying the scene, so changes can be saved to disk.
- Fixed some GC leaks for HDRP
- Fixed contact shadow not affected by shadow dimmer
- Fixed GGX that works correctly for the roughness value of 0 (mean specular highlgiht will disappeard for perfect mirror, we rely on maxSmoothness instead to always have a highlight even on mirror surface)
- Add stereo support to ShaderPassForward.hlsl. Forward rendering now seems passable in limited test scenes with camera-relative rendering disabled.
- Add stereo support to ProceduralSky.shader and OpaqueAtmosphericScattering.shader.
- Added CullingGroupManager to fix more GC.Alloc's in HDRP
- Fixed rendering when multiple cameras render into the same render texture

### Changed
- Changed the way depth & color pyramids are built to be faster and better quality, thus improving the look of distortion and refraction.
- Stabilize the dithered LOD transition mask with respect to the camera rotation.
- Avoid multiple depth buffer copies when decals are present
- Refactor code related to the RT handle system (No more normal buffer manager)
- Remove deferred directional shadow and move evaluation before lightloop
- Add a function GetNormalForShadowBias() that material need to implement to return the normal used for normal shadow biasing
- Remove Jimenez Subsurface scattering code (This code was disabled by default, now remove to ease maintenance)
- Change Decal API, decal contribution is now done in Material. Require update of material using decal
- Move a lot of files from CoreRP to HDRP/CoreRP. All moved files weren't used by Ligthweight pipeline. Long term they could move back to CoreRP after CoreRP become out of preview
- Updated camera inspector UI
- Updated decal gizmo
- Optimization: The objects that are rendered in the Motion Vector Pass are not rendered in the prepass anymore
- Removed setting shader inclue path via old API, use package shader include paths
- The default value of 'maxSmoothness' for punctual lights has been changed to 0.99
- Modified deferred compute and vert/frag shaders for first steps towards stereo support
- Moved material specific Shader Graph files into corresponding material folders.
- Hide environment lighting settings when enabling HDRP (Settings are control from sceneSettings)
- Update all shader includes to use absolute path (allow users to create material in their Asset folder)
- Done a reorganization of the files (Move ShaderPass to RenderPipeline folder, Move all shadow related files to Lighting/Shadow and others)
- Improved performance and quality of Screen Space Shadows

## [3.3.0-preview] - 2018-01-01

### Added
- Added an error message to say to use Metal or Vulkan when trying to use OpenGL API
- Added a new Fabric shader model that supports Silk and Cotton/Wool
- Added a new HDRP Lighting Debug mode to visualize Light Volumes for Point, Spot, Line, Rectangular and Reflection Probes
- Add support for reflection probe light layers
- Improve quality of anisotropic on IBL

### Fixed
- Fix an issue where the screen where darken when rendering camera preview
- Fix display correct target platform when showing message to inform user that a platform is not supported
- Remove workaround for metal and vulkan in normal buffer encoding/decoding
- Fixed an issue with color picker not working in forward
- Fixed an issue where reseting HDLight do not reset all of its parameters
- Fixed shader compile warning in DebugLightVolumes.shader

### Changed
- Changed default reflection probe to be 256x256x6 and array size to be 64
- Removed dependence on the NdotL for thickness evaluation for translucency (based on artist's input)
- Increased the precision when comparing Planar or HD reflection probe volumes
- Remove various GC alloc in C#. Slightly better performance

## [3.2.0-preview] - 2018-01-01

### Added
- Added a luminance meter in the debug menu
- Added support of Light, reflection probe, emissive material, volume settings related to lighting to Lighting explorer
- Added support for 16bit shadows

### Fixed
- Fix issue with package upgrading (HDRP resources asset is now versionned to worarkound package manager limitation)
- Fix HDReflectionProbe offset displayed in gizmo different than what is affected.
- Fix decals getting into a state where they could not be removed or disabled.
- Fix lux meter mode - The lux meter isn't affected by the sky anymore
- Fix area light size reset when multi-selected
- Fix filter pass number in HDUtils.BlitQuad
- Fix Lux meter mode that was applying SSS
- Fix planar reflections that were not working with tile/cluster (olbique matrix)
- Fix debug menu at runtime not working after nested prefab PR come to trunk
- Fix scrolling issue in density volume

### Changed
- Shader code refactor: Split MaterialUtilities file in two parts BuiltinUtilities (independent of FragInputs) and MaterialUtilities (Dependent of FragInputs)
- Change screen space shadow rendertarget format from ARGB32 to RG16

## [3.1.0-preview] - 2018-01-01

### Added
- Decal now support per channel selection mask. There is now two mode. One with BaseColor, Normal and Smoothness and another one more expensive with BaseColor, Normal, Smoothness, Metal and AO. Control is on HDRP Asset. This may require to launch an update script for old scene: 'Edit/Render Pipeline/Single step upgrade script/Upgrade all DecalMaterial MaskBlendMode'.
- Decal now supports depth bias for decal mesh, to prevent z-fighting
- Decal material now supports draw order for decal projectors
- Added LightLayers support (Base on mask from renderers name RenderingLayers and mask from light name LightLayers - if they match, the light apply) - cost an extra GBuffer in deferred (more bandwidth)
- When LightLayers is enabled, the AmbientOclusion is store in the GBuffer in deferred path allowing to avoid double occlusion with SSAO. In forward the double occlusion is now always avoided.
- Added the possibility to add an override transform on the camera for volume interpolation
- Added desired lux intensity and auto multiplier for HDRI sky
- Added an option to disable light by type in the debug menu
- Added gradient sky
- Split EmissiveColor and bakeDiffuseLighting in forward avoiding the emissiveColor to be affect by SSAO
- Added a volume to control indirect light intensity
- Added EV 100 intensity unit for area lights
- Added support for RendererPriority on Renderer. This allow to control order of transparent rendering manually. HDRP have now two stage of sorting for transparent in addition to bact to front. Material have a priority then Renderer have a priority.
- Add Coupling of (HD)Camera and HDAdditionalCameraData for reset and remove in inspector contextual menu of Camera
- Add Coupling of (HD)ReflectionProbe and HDAdditionalReflectionData for reset and remove in inspector contextual menu of ReflectoinProbe
- Add macro to forbid unity_ObjectToWorld/unity_WorldToObject to be use as it doesn't handle camera relative rendering
- Add opacity control on contact shadow

### Fixed
- Fixed an issue with PreIntegratedFGD texture being sometimes destroyed and not regenerated causing rendering to break
- PostProcess input buffers are not copied anymore on PC if the viewport size matches the final render target size
- Fixed an issue when manipulating a lot of decals, it was displaying a lot of errors in the inspector
- Fixed capture material with reflection probe
- Refactored Constant Buffers to avoid hitting the maximum number of bound CBs in some cases.
- Fixed the light range affecting the transform scale when changed.
- Snap to grid now works for Decal projector resizing.
- Added a warning for 128x128 cookie texture without mipmaps
- Replace the sampler used for density volumes for correct wrap mode handling

### Changed
- Move Rendering Debugger "Windows from Windows->General-> Rendering Debugger windows" to "Windows from Windows->Analysis-> Rendering Debugger windows"
- Update detail map formula for smoothness and albedo, goal it to bright and dark perceptually and scale factor is use to control gradient speed
- Refactor the Upgrade material system. Now a material can be update from older version at any time. Call Edit/Render Pipeline/Upgrade all Materials to newer version
- Change name EnableDBuffer to EnableDecals at several place (shader, hdrp asset...), this require a call to Edit/Render Pipeline/Upgrade all Materials to newer version to have up to date material.
- Refactor shader code: BakeLightingData structure have been replace by BuiltinData. Lot of shader code have been remove/change.
- Refactor shader code: All GBuffer are now handled by the deferred material. Mean ShadowMask and LightLayers are control by lit material in lit.hlsl and not outside anymore. Lot of shader code have been remove/change.
- Refactor shader code: Rename GetBakedDiffuseLighting to ModifyBakedDiffuseLighting. This function now handle lighting model for transmission too. Lux meter debug mode is factor outisde.
- Refactor shader code: GetBakedDiffuseLighting is not call anymore in GBuffer or forward pass, including the ConvertSurfaceDataToBSDFData and GetPreLightData, this is done in ModifyBakedDiffuseLighting now
- Refactor shader code: Added a backBakeDiffuseLighting to BuiltinData to handle lighting for transmission
- Refactor shader code: Material must now call InitBuiltinData (Init all to zero + init bakeDiffuseLighting and backBakeDiffuseLighting ) and PostInitBuiltinData

## [3.0.0-preview] - 2018-01-01

### Fixed
- Fixed an issue with distortion that was using previous frame instead of current frame
- Fixed an issue where disabled light where not upgrade correctly to the new physical light unit system introduce in 2.0.5-preview

### Changed
- Update assembly definitions to output assemblies that match Unity naming convention (Unity.*).

## [2.0.5-preview] - 2018-01-01

### Added
- Add option supportDitheringCrossFade on HDRP Asset to allow to remove shader variant during player build if needed
- Add contact shadows for punctual lights (in additional shadow settings), only one light is allowed to cast contact shadows at the same time and so at each frame a dominant light is choosed among all light with contact shadows enabled.
- Add PCSS shadow filter support (from SRP Core)
- Exposed shadow budget parameters in HDRP asset
- Add an option to generate an emissive mesh for area lights (currently rectangle light only). The mesh fits the size, intensity and color of the light.
- Add an option to the HDRP asset to increase the resolution of volumetric lighting.
- Add additional ligth unit support for punctual light (Lumens, Candela) and area lights (Lumens, Luminance)
- Add dedicated Gizmo for the box Influence volume of HDReflectionProbe / PlanarReflectionProbe

### Changed
- Re-enable shadow mask mode in debug view
- SSS and Transmission code have been refactored to be able to share it between various material. Guidelines are in SubsurfaceScattering.hlsl
- Change code in area light with LTC for Lit shader. Magnitude is now take from FGD texture instead of a separate texture
- Improve camera relative rendering: We now apply camera translation on the model matrix, so before the TransformObjectToWorld(). Note: unity_WorldToObject and unity_ObjectToWorld must never be used directly.
- Rename positionWS to positionRWS (Camera relative world position) at a lot of places (mainly in interpolator and FragInputs). In case of custom shader user will be required to update their code.
- Rename positionWS, capturePositionWS, proxyPositionWS, influencePositionWS to positionRWS, capturePositionRWS, proxyPositionRWS, influencePositionRWS (Camera relative world position) in LightDefinition struct.
- Improve the quality of trilinear filtering of density volume textures.
- Improve UI for HDReflectionProbe / PlanarReflectionProbe

### Fixed
- Fixed a shader preprocessor issue when compiling DebugViewMaterialGBuffer.shader against Metal target
- Added a temporary workaround to Lit.hlsl to avoid broken lighting code with Metal/AMD
- Fixed issue when using more than one volume texture mask with density volumes.
- Fixed an error which prevented volumetric lighting from working if no density volumes with 3D textures were present.
- Fix contact shadows applied on transmission
- Fix issue with forward opaque lit shader variant being removed by the shader preprocessor
- Fixed compilation errors on platforms with limited XRSetting support.
- Fixed apply range attenuation option on punctual light
- Fixed issue with color temperature not take correctly into account with static lighting
- Don't display fog when diffuse lighting, specular lighting, or lux meter debug mode are enabled.

## [2.0.4-preview] - 2018-01-01

### Fixed
- Fix issue when disabling rough refraction and building a player. Was causing a crash.

## [2.0.3-preview] - 2018-01-01

### Added
- Increased debug color picker limit up to 260k lux

## [2.0.2-preview] - 2018-01-01

### Added
- Add Light -> Planar Reflection Probe command
- Added a false color mode in rendering debug
- Add support for mesh decals
- Add flag to disable projector decals on transparent geometry to save performance and decal texture atlas space
- Add ability to use decal diffuse map as mask only
- Add visualize all shadow masks in lighting debug
- Add export of normal and roughness buffer for forwardOnly and when in supportOnlyForward mode for forward
- Provide a define in lit.hlsl (FORWARD_MATERIAL_READ_FROM_WRITTEN_NORMAL_BUFFER) when output buffer normal is used to read the normal and roughness instead of caclulating it (can save performance, but lower quality due to compression)
- Add color swatch to decal material

### Changed
- Change Render -> Planar Reflection creation to 3D Object -> Mirror
- Change "Enable Reflector" name on SpotLight to "Angle Affect Intensity"
- Change prototype of BSDFData ConvertSurfaceDataToBSDFData(SurfaceData surfaceData) to BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)

### Fixed
- Fix issue with StackLit in deferred mode with deferredDirectionalShadow due to GBuffer not being cleared. Gbuffer is still not clear and issue was fix with the new Output of normal buffer.
- Fixed an issue where interpolation volumes were not updated correctly for reflection captures.
- Fixed an exception in Light Loop settings UI

## [2.0.1-preview] - 2018-01-01

### Added
- Add stripper of shader variant when building a player. Save shader compile time.
- Disable per-object culling that was executed in C++ in HD whereas it was not used (Optimization)
- Enable texture streaming debugging (was not working before 2018.2)
- Added Screen Space Reflection with Proxy Projection Model
- Support correctly scene selection for alpha tested object
- Add per light shadow mask mode control (i.e shadow mask distance and shadow mask). It use the option NonLightmappedOnly
- Add geometric filtering to Lit shader (allow to reduce specular aliasing)
- Add shortcut to create DensityVolume and PlanarReflection in hierarchy
- Add a DefaultHDMirrorMaterial material for PlanarReflection
- Added a script to be able to upgrade material to newer version of HDRP
- Removed useless duplication of ForwardError passes.
- Add option to not compile any DEBUG_DISPLAY shader in the player (Faster build) call Support Runtime Debug display

### Changed
- Changed SupportForwardOnly to SupportOnlyForward in render pipeline settings
- Changed versioning variable name in HDAdditionalXXXData from m_version to version
- Create unique name when creating a game object in the rendering menu (i.e Density Volume(2))
- Re-organize various files and folder location to clean the repository
- Change Debug windows name and location. Now located at:  Windows -> General -> Rendering Debugger

### Removed
- Removed GlobalLightLoopSettings.maxPlanarReflectionProbes and instead use value of GlobalLightLoopSettings.planarReflectionProbeCacheSize
- Remove EmissiveIntensity parameter and change EmissiveColor to be HDR (Matching Builtin Unity behavior) - Data need to be updated - Launch Edit -> Single Step Upgrade Script -> Upgrade all Materials emissionColor

### Fixed
- Fix issue with LOD transition and instancing
- Fix discrepency between object motion vector and camera motion vector
- Fix issue with spot and dir light gizmo axis not highlighted correctly
- Fix potential crash while register debug windows inputs at startup
- Fix warning when creating Planar reflection
- Fix specular lighting debug mode (was rendering black)
- Allow projector decal with null material to allow to configure decal when HDRP is not set
- Decal atlas texture offset/scale is updated after allocations (used to be before so it was using date from previous frame)

## [0.0.0-preview] - 2018-01-01

### Added
- Configure the VolumetricLightingSystem code path to be on by default
- Trigger a build exception when trying to build an unsupported platform
- Introduce the VolumetricLightingController component, which can (and should) be placed on the camera, and allows one to control the near and the far plane of the V-Buffer (volumetric "froxel" buffer) along with the depth distribution (from logarithmic to linear)
- Add 3D texture support for DensityVolumes
- Add a better mapping of roughness to mipmap for planar reflection
- The VolumetricLightingSystem now uses RTHandles, which allows to save memory by sharing buffers between different cameras (history buffers are not shared), and reduce reallocation frequency by reallocating buffers only if the rendering resolution increases (and suballocating within existing buffers if the rendering resolution decreases)
- Add a Volumetric Dimmer slider to lights to control the intensity of the scattered volumetric lighting
- Add UV tiling and offset support for decals.
- Add mipmapping support for volume 3D mask textures

### Changed
- Default number of planar reflection change from 4 to 2
- Rename _MainDepthTexture to _CameraDepthTexture
- The VolumetricLightingController has been moved to the Interpolation Volume framework and now functions similarly to the VolumetricFog settings
- Update of UI of cookie, CubeCookie, Reflection probe and planar reflection probe to combo box
- Allow enabling/disabling shadows for area lights when they are set to baked.
- Hide applyRangeAttenuation and FadeDistance for directional shadow as they are not used

### Removed
- Remove Resource folder of PreIntegratedFGD and add the resource to RenderPipeline Asset

### Fixed
- Fix ConvertPhysicalLightIntensityToLightIntensity() function used when creating light from script to match HDLightEditor behavior
- Fix numerical issues with the default value of mean free path of volumetric fog
- Fix the bug preventing decals from coexisting with density volumes
- Fix issue with alpha tested geometry using planar/triplanar mapping not render correctly or flickering (due to being wrongly alpha tested in depth prepass)
- Fix meta pass with triplanar (was not handling correctly the normal)
- Fix preview when a planar reflection is present
- Fix Camera preview, it is now a Preview cameraType (was a SceneView)
- Fix handling unknown GPUShadowTypes in the shadow manager.
- Fix area light shapes sent as point lights to the baking backends when they are set to baked.
- Fix unnecessary division by PI for baked area lights.
- Fix line lights sent to the lightmappers. The backends don't support this light type.
- Fix issue with shadow mask framesettings not correctly taken into account when shadow mask is enabled for lighting.
- Fix directional light and shadow mask transition, they are now matching making smooth transition
- Fix banding issues caused by high intensity volumetric lighting
- Fix the debug window being emptied on SRP asset reload
- Fix issue with debug mode not correctly clearing the GBuffer in editor after a resize
- Fix issue with ResetMaterialKeyword not resetting correctly ToggleOff/Roggle Keyword
- Fix issue with motion vector not render correctly if there is no depth prepass in deferred

## [0.0.0-preview] - 2018-01-01

### Added
- Screen Space Refraction projection model (Proxy raycasting, HiZ raymarching)
- Screen Space Refraction settings as volume component
- Added buffered frame history per camera
- Port Global Density Volumes to the Interpolation Volume System.
- Optimize ImportanceSampleLambert() to not require the tangent frame.
- Generalize SampleVBuffer() to handle different sampling and reconstruction methods.
- Improve the quality of volumetric lighting reprojection.
- Optimize Morton Order code in the Subsurface Scattering pass.
- Planar Reflection Probe support roughness (gaussian convolution of captured probe)
- Use an atlas instead of a texture array for cluster transparent decals
- Add a debug view to visualize the decal atlas
- Only store decal textures to atlas if decal is visible, debounce out of memory decal atlas warning.
- Add manipulator gizmo on decal to improve authoring workflow
- Add a minimal StackLit material (work in progress, this version can be used as template to add new material)

### Changed
- EnableShadowMask in FrameSettings (But shadowMaskSupport still disable by default)
- Forced Planar Probe update modes to (Realtime, Every Update, Mirror Camera)
- Screen Space Refraction proxy model uses the proxy of the first environment light (Reflection probe/Planar probe) or the sky
- Moved RTHandle static methods to RTHandles
- Renamed RTHandle to RTHandleSystem.RTHandle
- Move code for PreIntegratedFDG (Lit.shader) into its dedicated folder to be share with other material
- Move code for LTCArea (Lit.shader) into its dedicated folder to be share with other material

### Removed
- Removed Planar Probe mirror plane position and normal fields in inspector, always display mirror plane and normal gizmos

### Fixed
- Fix fog flags in scene view is now taken into account
- Fix sky in preview windows that were disappearing after a load of a new level
- Fix numerical issues in IntersectRayAABB().
- Fix alpha blending of volumetric lighting with transparent objects.
- Fix the near plane of the V-Buffer causing out-of-bounds look-ups in the clustered data structure.
- Depth and color pyramid are properly computed and sampled when the camera renders inside a viewport of a RTHandle.
- Fix decal atlas debug view to work correctly when shadow atlas view is also enabled
- Fix TransparentSSR with non-rendergraph.
- Fix shader compilation warning on SSR compute shader.
