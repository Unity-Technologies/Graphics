# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [7.3.1] - 2020-03-11

### Added
- Added the exposure sliders to the planar reflection probe preview
- Added a warning and workaround instructions that appear when you enable XR single-pass after the first frame with the XR SDK.
- Added an "enable" toggle to the SSR volume component.

### Fixed
- Fixed issue with AssetPostprocessors dependencies causing models to be imported twice when upgrading the package version.
- Fix player build DX12
- Fix issue with AO being misaligned when multiple view are visible.
- Fix issue that caused the clamp of camera rotation motion for motion blur to be ineffective.
- Fixed culling of lights with XR SDK
- Fixed memory stomp in shadow caching code, leading to overflow of Shadow request array and runtime errors.
- Fixed an issue related to transparent objects reading the ray traced indirect diffuse buffer
- Fixed an issue with filtering ray traced area lights when the intensity is high or there is an exposure.
- Fixed ill-formed include path in Depth Of Field shader.
- Fixed a bug in semi-transparent shadows (object further than the light casting shadows)
- Fix state enabled of default volume profile when in package.
- Fixed removal of MeshRenderer and MeshFilter on adding Light component. 
- Fixed a bug in debug light volumes.
- Fixed the culling was not disposed error in build log.
- Fixed an issue where fog sky color mode could sample NaNs in the sky cubemap.
- Fixed a leak in the PBR sky renderer.
- Added a tooltip to the Ambient Mode parameter in the Visual Envionment volume component.
- Static lighting sky now takes the default volume into account (this fixes discrepancies between baked and realtime lighting).
- Fixed a leak in the sky system.
- Hide reflection probes in the renderer components.
- Removed MSAA Buffers allocation when lit shader mode is set to "deferred only".
- Fixed invalid cast for realtime reflection probes (case 1220504)
- Fixed invalid game view rendering when disabling all cameras in the scene (case 1105163)
- Fixed infinite reload loop while displaying Light's Shadow's Link Light Layer in Inspector of Prefab Asset.
- Fixed the cookie atlas size and planar atlas size being too big after an upgrade of the HDRP asset.
- Fixed alpha clipping test (comparison was '>', now '>=')
- Fixed preview camera (eg. shader graph preview) when path tracing is on
- Fixed DXR player build
- Fixed compilation issue with linux vulkan and raytrace shader
- Fixed the HDRP asset migration code not being called after an upgrade of the package
- Fixed draw renderers custom pass out of bound exception
- Fixed an issue with emissive light meshes not being in the RAS.
- Fixed a warning due to StaticLightingSky when reloading domain in some cases.
- Fixed the MaxLightCount being displayed when the light volume debug menu is on ColorAndEdge.
- Fix an exception in case two LOD levels are using the same mesh renderer.
- Fixed error in the console when switching shader to decal in the material UI.
- Fixed z-fighting in scene view when scene lighting is off (case 1203927)

### Changed
- Renamed the cubemap used for diffuse convolution to a more explicit name for the memory profiler.
- Light dimmer can now get values higher than one and was renamed to multiplier in the UI. 
- Removed info box requesting volume component for Visual Environment and updated the documentation with the relevant information.
- Add range-based clipping to box lights (case 1178780)
- Improve area light culling (case 1085873)

## [7.2.0] - 2020-02-10

### Added
- Added the possibility to have ray traced colored and semi-transparent shadows on directional lights.
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

### Fixed
- Update documentation of HDRISky-Backplate, precise how to have Ambient Occlusion on the Backplate
- Fixed TerrainLitGUI when per-pixel normal property is not present.
- Fixed a bug due to depth history begin overriden too soon
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

### Changed
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
- Hide the Probes section in the Renderer editos because it was unused.
- Moved BeginCameraRendering callback right before culling.
- Changed the visibility of the Indirect Lighting Controller component to public.

## [7.1.8] - 2020-01-20

### Fixed
- Fixed white and dark flashes on scenes with very high or very low exposure when Automatic Exposure is being used.
- Fixed memory leak in Sky when in matcap mode.
	
### Changed
- On Xbox and PS4 you will also need to download the com.unity.render-pipeline.platform (ps4 or xboxone) package from the appropriate platform developer forum

## [7.1.7] - 2019-12-11

### Added
- Added a check in the custom post process template to throw an error if the default shader is not found.

### Fixed
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
- Fixed a bug in dxr due to depth history begin overriden too soon
- Fixed CustomPassSampleCameraColor scale issue when called from Before Transparent injection point.
- Fixed corruption of AO in baked probes.
- Fixed issue with upgrade of projects that still had Very High as shadow filtering quality.
- Removed shadow near plane from the Directional Light Shadow UI.
- Fixed performance issue with performances of custom pass culling.

## [7.1.6] - 2019-11-22

### Added
- Added Backplate projection from the HDRISky
- Added Shadow Matte in UnlitMasterNode, which only received shadow without lighting
- Added support for depth copy with XR SDK
- Added debug setting to Render Pipeline Debug Window to list the active XR views
- Added an option to filter the result of the volumetric lighting (off by default).
- Added a transmission multiplier for directional lights
- Added XR single-pass test mode to Render Pipeline Debug Window
- Added debug setting to Render Pipeline Window to list the active XR views
- Added a new refraction mode for the Lit shader (thin). Which is a box refraction with small thickness values
- Added the code to support Barn Doors for Area Lights based on a shaderconfig option.
- Added HDRPCameraBinder property binder for Visual Effect Graph
- Added "Celestial Body" controls to the Directional Light
- Added new parameters to the Physically Based Sky
- Added Reflections to the DXR Wizard

### Fixed
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
- Fixed an issue with Metal Shader Compiler and GTAO shader for metal
- Fixed resources load issue while upgrading HDRP package.
- Fixed LOD fade mask by accounting for field of view
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

### Changed
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

## [7.1.5] - 2019-11-15

### Fixed
- Fixed black reflection probes the first time loading a project

## [7.1.4] - 2019-11-13

### Added
- Added XR single-pass setting into HDRP asset
- Added a penumbra tint option for lights

### Fixed
- Fixed EOL for some files
- Fixed scene view rendering with volumetrics and XR enabled
- Fixed decals to work with multiple cameras
- Fixed optional clear of GBuffer (Was always on)
- Fixed render target clears with XR single-pass rendering
- Fixed HDRP samples file hierarchy
- Fixed Light units not matching light type
- Fixed QualitySettings panel not displaying HDRP Asset

### Changed
- Changed parametrization of PCSS, now softness is derived from angular diameter (for directional lights) or shape radius (for point/spot lights) and min filter size is now in the [0..1] range.
- Moved the copy of the geometry history buffers to right after the depth mip chain generation.
- Rename "Luminance" to "Nits" in UX for physical light unit
- Rename FrameSettings "SkyLighting" to "SkyReflection"

## [7.1.3] - 2019-11-04

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
- Added hability to name LightLayers in HDRenderPipelineAsset
- Added a range compression factor for Reflection Probe and Planar Reflection Probe to avoid saturation of colors.
- Added path tracing support for directional, point and spot lights, as well as emission from Lit and Unlit.
- Added non temporal version of SSAO.
- Added more detailed ray tracing stats in the debug window
- Added Disc area light (bake only)
- Added a warning in the material UI to prevent transparent + subsurface-scattering combination.

### Fixed
- Sorting, undo, labels, layout in the Lighting Explorer.
- Fixed sky settings and materials in Shader Graph Samples package
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
- VFX: Removed z-fight glitches that could appear when using deferred depth prepass and lit quad primitives
- VFX: Preserve specular option for lit outputs (matches HDRP lit shader)
- Fixed init of debug for FrameSettingsHistory on SceneView camera
- Added a fix script to handle the warning 'referenced script in (GameObject 'SceneIDMap') is missing'
- Fix Wizard load when none selected for RenderPipelineAsset
- Fixed issue with unclear naming of debug menu for decals.

### Changed
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

## [7.1.2] - 2019-09-19

### Fixed
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
- Shader type Real translates to FP16 precision on Nintendo Switch.
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
- Added Material validator in Render Pipeline Debug
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
- Fixed copyStencilBuffer pass for Switch
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
- ColorPyramid compute shader passes is swapped to pixel shader passes on platforms where the later is faster (Nintendo Switch).
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
- Update build light list shader code to support 32 threads in wavefronts on Switch
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
- Move Render Pipeline Debug "Windows from Windows->General-> Render Pipeline debug windows" to "Windows from Windows->Analysis-> Render Pipeline debug windows"
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
- Fixed compilation errors on Nintendo Switch (limited XRSetting support).
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
- Change Debug windows name and location. Now located at:  Windows -> General -> Render Pipeline Debug

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
