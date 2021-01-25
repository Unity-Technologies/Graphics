# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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
- Fixed issue with faulty shadow transition when view is close to an object under some aspect ratio conditions
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
- Fixed a null ref exception when running playmode tests with the render pipeline debug window opened.
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
- Fix TransparentSSR with non-rendergraph.
- Fix shader compilation warning on SSR compute shader.
