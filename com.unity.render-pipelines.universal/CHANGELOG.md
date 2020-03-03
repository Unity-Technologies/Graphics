# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Added the option to strip Terrain hole Shader variants.
- Added support for additional Directional Lights. The amount of additional Directional Lights is limited by the maximum Per-object Lights in the Render Pipeline Asset.
- Added default implementations of OnPreprocessMaterialDescription for FBX, Obj, Sketchup and 3DS file formats.
- Added Transparency Sort Mode and Transparency Sort Axis to 2DRendererData.
- Added support for a user defined default material to 2DRendererData.
- Added the option to toggle shadow receiving on transparent objects.
- Added XR multipass rendering. Multipass rendering is a requirement on many VR platforms and allows graceful fallback when single-pass rendering isn't available.
- Added support for Camera Stacking when using the Forward Renderer. This introduces the Camera `Render Type` property. A Base Camera can be initialized with either the Skybox or Solid Color, and can combine its output with that of one or more Overlay Cameras. An Overlay Camera is always initialized with the contents of the previous Camera that rendered in the Camera Stack.
- Fixed NaNs in tonemap algorithms (neutral and ACES) on Nintendo Switch.
- Added AssetPostprocessors and Shadergraphs to handle Arnold Standard Surface and 3DsMax Physical material import from FBX.
- Added `[MainTexture]` and `[MainColor]` shader property attributes to URP shader properties. These will link script material.mainTextureOffset and material.color to `_BaseMap` and `_BaseColor` shader properties.
- Added the option to specify the maximum number of visible lights. If you set a value, lights are sorted based on their distance from the Camera.
- Added the option to control the transparent layer separately in the Forward Renderer.
- Added the ability to set individual RendererFeatures to be active or not, use `ScriptableRendererFeature.SetActive(bool)` to set whether a Renderer Feature will execute,  `ScriptableRendererFeature.isActive` can be used to check the current active state of the Renderer Feature.

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
- UniversalRenderPipelineAsset no longer supports presets [case 1197020](https://issuetracker.unity3d.com/issues/urp-reset-functionality-does-not-work-on-preset-of-universalrenderpipelineassets)
- The number of maximum visible lights is now determined by whether the platform is mobile or not.
- Renderer Feature list is now redesigned to fit more closely to the Volume Profile UI, this vastly improves UX and reliability of the Renderer Features List.

### Fixed
- Fixed an issue where linear to sRGB conversion occurred twice on certain Android devices.
- Fixed an issue where there were 2 widgets showing the outer angle of a spot light.
- Fixed an issue where Unity rendered fullscreen quads with the pink error shader when you enabled the Stop NaN post-processing pass.
- Fixed an issue where Terrain hole Shader changes were missing. [case 1179808](https://issuetracker.unity3d.com/issues/terrain-brush-tool-is-not-drawing-when-paint-holes-is-selected)
- Fixed an issue where the Shader Graph `SceneDepth` node didn't work with XR single-pass (double-wide) rendering. See [case 1123069](https://issuetracker.unity3d.com/issues/lwrp-vr-shadergraph-scenedepth-doesnt-work-in-single-pass-rendering).
- Fixed Unlit and BakedLit shader compilations in the meta pass.
- Fixed an issue where the Bokeh Depth of Field shader would fail to compile on PS4.
- Fixed an issue where the Scene lighting button didn't work when you used the 2D Renderer.
- Fixed a performance regression when you used the 2D Renderer.
- Fixed an issue where the Freeform 2D Light gizmo didn't correctly show the Falloff offset.
- Fixed an issue where the 2D Renderer rendered nothing when you used shadow-casting lights with incompatible Renderer2DData.
- Fixed an issue where Prefab previews were incorrectly lit when you used the 2D Renderer.
- Fixed an issue where the Light didn't update correctly when you deleted a Sprite that a Sprite 2D Light uses.
- Fixed an issue where 2D Lighting was broken for Perspective Cameras.
- Fixed an issue where resetting a Freeform 2D Light would throw null reference exceptions. [case 1184536](https://issuetracker.unity3d.com/issues/lwrp-changing-light-type-to-freeform-after-clicking-on-reset-throws-multiple-arguementoutofrangeexception)
- Fixed an issue where Freeform 2D Lights were not culled correctly when there was a Falloff Offset.
- Fixed an issue where Tilemap palettes were invisible in the Tile Palette window when the 2D Renderer was in use. [case 1162550](https://issuetracker.unity3d.com/issues/adding-tiles-in-the-tile-palette-makes-the-tiles-invisible)
- Fixed issue where black emission would cause unneccesary inspector UI repaints [case 1105661](https://issuetracker.unity3d.com/issues/lwrp-inspector-window-is-being-repainted-when-using-the-material-with-emission-enabled-and-set-to-black-00-0)
- Fixed user LUT sampling being done in Linear instead of sRGB.
- Fixed an issue when trying to get the Renderer via API on the first frame [case 1189196](https://issuetracker.unity3d.com/product/unity/issues/guid/1189196/)
- Fixed a material leak on domain reload.
- Fixed an issue where deleting an entry from the Renderer List and then undoing that change could cause a null reference. [case 1191896](https://issuetracker.unity3d.com/issues/nullreferenceexception-when-attempting-to-remove-entry-from-renderer-features-list-after-it-has-been-removed-and-then-undone)
- Fixed an issue where the user would get an error if they removed the Additional Camera Data component. [case 1189926](https://issuetracker.unity3d.com/issues/unable-to-remove-universal-slash-hd-additional-camera-data-component-serializedobject-target-destroyed-error-is-thrown)
- Fixed post-processing with XR single-pass rendering modes.
- Fixed an issue where Cinemachine v2.4 couldn't be used together with Universal RP due to a circular dependency between the two packages.
- Fixed an issue that caused shaders containing `HDRP` string in their path to be stripped from the build.
- Fixed an issue that caused only selected object to render in SceneView when Wireframe drawmode was selected.
- Fixed Renderer Features UI tooltips. [case 1191901](https://issuetracker.unity3d.com/issues/forward-renderers-render-objects-layer-mask-tooltip-is-incorrect-and-contains-a-typo)
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
- Fixed incorrect light attenuation on Nintendo Switch.
- Fixed an issue where the Volume System would not use the Cameras Transform when no `Volume Trigger` was set.
- Fixed an issue where post processing disappeared when using custom renderers and SMAA or no AA
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
- Upgrading to UniversalRP is designed to be almost seamless from the user side.
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
- Shader type Real translates to FP16 precision on Nintendo Switch.

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
- Fixed fp16 overflow in Switch in specular calculation
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
- Fixed compilation errors on Nintendo Switch (limited XRSetting support).

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
