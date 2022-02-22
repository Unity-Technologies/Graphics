# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [12.1.6] - 2022-02-09

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [12.1.5] - 2022-01-14

### Added
- Added common support code for FSR

### Fixed
- Fixed undo in for `DebugUI.EnumFields` on the rendering debugger. (case 1386964)
- Fixed unnecessary memory allocation inside FSR's RCAS shader constants helper function.
- Fixed texture gather macros for GLCore and moved them from target 4.6 to target 4.5.
- Fixed cubemap array macros for GLCore.

### Changed
- Removed FSR_ENABLE_16BIT option from FSRCommon.hlsl. The 16-bit FSR implementation is now automatically enabled when supported by the target platform.

## [12.1.4] - 2021-12-07

### Fixed
- Fixed XR support in CoreUtils.DrawFullscreen function.
- Fixed an issue causing Render Graph execution errors after a random amount of time.
- Fixed issue in DynamicResolutionHandler when camera request was turned off at runtime, the ScalableBufferManager would leak state and not unset DRS state (case 1383093).
- Fixed the issue with the special Turkish i, when looking for the m_IsGlobal property in VolumeEditor. (case 1276892)
- Fixed IES profile importer handling of overflow (outside 0-1 range) of attenutation splines values.

## [12.1.3] - 2021-11-17
Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [12.1.2] - 2021-10-22

### Fixed
- Fixed serialization of DebugStateFlags, the internal Enum was not being serialized.
- Fixed issue when changing volume profiles at runtime with a script (case 1364256).

## [12.1.1] - 2021-10-04

### Fixed
- Fixed black pixel issue in AMD FidelityFX RCAS implementation
- Fixed a critical issue on android devices & lens flares. Accidentally creating a 16 bit texture was causing gpus not supporting them to fail.

## [12.1.0] - 2021-09-23

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [12.0.0] - 2021-01-11

### Added
- Support for the PlayStation 5 platform has been added.
- Support for additional properties for Volume Components without custom editor
- Added VolumeComponentMenuForRenderPipelineAttribute to specify a volume component only for certain RenderPipelines.
- Calculating correct rtHandleScale by considering the possible pixel rounding when DRS is on
- Support for the PlayStation 5 platform has been added.
- Support for the XboxSeries platform has been added.
- Added Editor window that allow showing an icon to browse the documentation
- New method DrawHeaders for VolumeComponentsEditors
- Unification of Material Editor Headers Scopes
- New API functions with no side effects in DynamicResolutionHandler, to retrieve resolved drs scale and to apply DRS on a size.
- Added helper for Volumes (Enable All Overrides, Disable All Overrides, Remove All Overrides).
- Added a blitter utility class. Moved from HDRP to RP core.
- Added a realtime 2D texture atlas utility classes. Moved from HDRP to RP core.
- New methods on CoreEditorDrawers, to allow adding a label on a group before rendering the internal drawers
- Method to generate a Texture2D of 1x1 with a plain color
- Red, Green, Blue Texture2D on CoreEditorStyles
- New API in DynamicResolutionHandler to handle multicamera rendering for hardware mode. Changing cameras and resetting scaling per camera should be safe.
- Added SpeedTree8MaterialUpgrader, which provides utilities for upgrading and importing SpeedTree 8 assets to scriptable render pipelines.
- Adding documentation links to Light Sections
- Support for Lens Flare Data Driven (from images and Procedural shapes), on HDRP
- New SRPLensFlareData Asset
- Adding documentation links to Light Sections.
- Added sampling noise to probe volume sampling position to hide seams between subdivision levels.
- Added DebugUI.Foldout.isHeader property to allow creating full-width header foldouts in Rendering Debugger.
- Added DebugUI.Flags.IsHidden to allow conditional display of widgets in Rendering Debugger.
- Added "Expand/Collapse All" buttons to Rendering Debugger window menu.
- Added mouse & touch input support for Rendering Debugger runtime UI, and fix problems when InputSystem package is used.
- Add automatic spaces to enum display names used in Rendering Debugger and add support for InspectorNameAttribute.
- Adding new API functions inside DynamicResolutionHandler to get mip bias. This allows dynamic resolution scaling applying a bias on the frame to improve on texture sampling detail.
- Added a reminder if the data of probe volume might be obsolete.
- Added new API function inside DynamicResolutionHandler and new settings in GlobalDynamicResolutionSettings to control low res transparency thresholds. This should help visuals when the screen percentage is too low.
- Added common include file for meta pass functionality (case 1211436)
- Added OverridablePropertyScope (for VolumeComponentEditor child class only) to handle the Additional Property, the override checkbox and disable display and decorator attributes in one scope.
- Added IndentLevelScope (for VolumeComponentEditor child class only) to handle indentation of the field and the checkbox.
- Added an option to change the visibilty of the Volumes Gizmos (Solid, Wireframe, Everything), available at Preferences > Core Render Pipeline
- Added class for drawing shadow cascades `UnityEditor.Rendering.ShadowCascadeGUI.DrawShadowCascades`.
- Added UNITY_PREV_MATRIX_M and UNITY_PREV_MATRIX_I_M shader macros to support instanced motion vector rendering
- Added new API to customize the rtHandleProperties of a particular RTHandle. This is a temporary work around to assist with viewport setup of Custom post process when dealing with DLSS or TAAU
- Added `IAdditionalData` interface to identify the additional datas on the core package.

### Fixed
- Help boxes with fix buttons do not crop the label.
- Fixed missing warning UI about Projector component being unsupported (case 1300327).
- Fixed the display name of a Volume Parameter when is defined the attribute InspectorName
- Calculating correct rtHandleScale by considering the possible pixel rounding when DRS is on
- Problem on domain reload of Volume Parameter Ranges and UI values
- Fixed Right Align of additional properties on Volume Components Editors
- Fixed normal bias field of reference volume being wrong until the profile UI was displayed.
- Fixed L2 for Probe Volumes.
- When adding Overrides to the Volume Profile, only show Volume Components from the current Pipeline.
- Fixed assertion on compression of L1 coefficients for Probe Volume.
- Explicit half precision not working even when Unified Shader Precision Model is enabled.
- Fixed ACES filter artefact due to half float error on some mobile platforms.
- Fixed issue displaying a warning of different probe reference volume profiles even when they are equivalent.
- Fixed missing increment/decrement controls from DebugUIIntField & DebugUIUIntField widget prefabs.
- Fixed IES Importer related to new API on core.
- Fixed a large, visible stretch ratio in a LensFlare Image thumbnail.
- Fixed Undo from script refreshing thumbnail.
- Fixed cropped thumbnail for Image with non-uniform scale and rotation
- Skip wind calculations for Speed Tree 8 when wind vector is zero (case 1343002)
- Fixed memory leak when changing SRP pipeline settings, and having the player in pause mode.
- Fixed alignment in Volume Components
- Virtual Texturing fallback texture sampling code correctly honors the enableGlobalMipBias when virtual texturing is disabled.
- Fixed LightAnchor too much error message, became a HelpBox on the Inspector.
- Fixed library function SurfaceGradientFromTriplanarProjection to match the mapping convention used in SampleUVMappingNormalInternal.hlsl and fix its description.
- Fixed Volume Gizmo size when rescaling parent GameObject
- Fixed rotation issue now all flare rotate on positive direction (1348570)
- Fixed error when change Lens Flare Element Count followed by undo (1346894)
- Fixed Lens Flare Thumbnails
- Fixed Lens Flare 'radialScreenAttenuationCurve invisible'
- Fixed Lens Flare rotation for Curve Distribution
- Fixed potentially conflicting runtime Rendering Debugger UI command by adding an option to disable runtime UI altogether (1345783).
- Fixed Lens Flare position for celestial at very far camera distances. It now locks correctly into the celestial position regardless of camera distance (1363291)
- Fixed issues caused by automatically added EventSystem component, required to support Rendering Debugger Runtime UI input. (1361901)
- Fixed API to draw color temperature for Lights.

### Changed
- Improved the warning messages for Volumes and their Colliders.
- Changed Window/Render Pipeline/Render Pipeline Debug to Window/Analysis/Rendering Debugger
- Changed Window/Render Pipeline/Look Dev to Window/Analysis/Look Dev
- Changed Window/Render Pipeline/Render Graph Viewer to Window/Analysis/Render Graph Viewer
- Changed Window/Render Pipeline/Graphics Compositor to Window/Rendering/Graphics Compositor
- Volume Gizmo Color setting is now under Colors->Scene->Volume Gizmo
- Volume Gizmo alpha changed from 0.5 to 0.125
- Moved Edit/Render Pipeline/Generate Shader Includes to Edit/Rendering/Generate Shader Includes
- Moved Assets/Create/LookDev/Environment Library to Assets/Create/Rendering/Environment Library (Look Dev)
- Changed Nintendo Switch specific half float fixes in color conversion routines to all platforms.
- Improved load asset time for probe volumes.
- ClearFlag.Depth does not implicitely clear stencil anymore. ClearFlag.Stencil added.
- The RTHandleSystem no longer requires a specific number of sample for MSAA textures. Number of samples can be chosen independently for all textures.
- Platform ShaderLibrary API headers now have a new macro layer for 2d texture sampling macros. This layer starts with PLATFORM_SAMPLE2D definition, and it gives the possibility of injecting sampling behavior on a render pipeline level. For example: being able to a global mip bias for temporal upscalers.
- Update icon for IES, LightAnchor and LensFlare
- LensFlare (SRP) can be now disabled per element
- LensFlare (SRP) tooltips now refer to meters.
- Serialize the Probe Volume asset as binary to improve footprint on disk and loading speed.
- LensFlare Element editor now have Thumbnail preview
- Improved IntegrateLDCharlie() to use uniform stratified sampling for faster convergence towards the ground truth
- DynamicResolutionHandler.GetScaledSize function now clamps, and never allows to return a size greater than its input.
- Removed DYNAMIC_RESOLUTION snippet on lens flare common shader. Its not necessary any more on HDRP, which simplifies the shader.
- Made occlusion Radius for lens flares in directional lights, be independant of the camera's far plane.

## [11.0.0] - 2020-10-21

### Fixed
- Fixed the default background color for previews to use the original color.
- Fixed spacing between property fields on the Volume Component Editors.
- Fixed ALL/NONE to maintain the state on the Volume Component Editors.
- Fixed the selection of the Additional properties from ALL/NONE when the option "Show additional properties" is disabled
- Fixed ACES tonemaping for Nintendo Switch by forcing some shader color conversion functions to full float precision.
- Fixed a bug in FreeCamera which would only provide a speed boost for the first frame when pressing the Shfit key.

### Added
- New View Lighting Tool, a component which allow to setup light in the camera space
- New function in GeometryTools.hlsl to calculate triangle edge and full triangle culling.
- Several utils functions to access SphericalHarmonicsL2 in a more verbose and intuitive fashion.

## [10.2.0] - 2020-10-19

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [10.1.0] - 2020-10-12

### Added
- Added context options "Move to Top", "Move to Bottom", "Expand All" and "Collapse All" for volume components.
- Added the support of input system V2

### Fixed
- Fixed the scene view to scale correctly when hardware dynamic resolution is enabled (case 1158661)
- Fixed game view artifacts on resizing when hardware dynamic resolution was enabled
- Fixed issue that caused `UNITY_REVERSED_Z` and `UNITY_UV_STARTS_AT_TOP` being defined in platforms that don't support it.

### Changed
- LookDev menu item entry is now disabled if the current pipeline does not support it.

## [10.0.0] - 2019-06-10

### Added
- Add rough version of ContextualMenuDispatcher to solve conflict amongst SRP.
- Add api documentation for TextureCombiner.
- Add tooltips in LookDev's toolbar.
- Add XRGraphicsAutomatedTests helper class.

### Fixed
- Fixed compile errors for platforms with no VR support
- Replaced reference to Lightweight Render Pipeline by Universal Render Pipeline in the package description
- Fixed LighProbes when using LookDev.
- Fix LookDev minimal window size.
- Fix object rotation at instentiation to keep the one in prefab or used in hierarchy.
- Fixed shader compile errors when trying to use tessellation shaders with PlayStation VR on PS4.
- Fixed shader compile errors about LODDitheringTransition not being supported in GLES2.
- Fix `WaveIsFirstLane()` to ignore helper lanes in fragment shaders on PS4.
- Fixed a bug where Unity would crash if you tried to remove a Camera component from a GameObject using the Inspector window, while other components dependended on the Camera component.
- Fixed errors due to the debug menu when enabling the new input system.
- Fix LookDev FPS manipulation in view
- Fix LookDev zoom being stuck when going near camera pivot position
- Fix LookDev manipulation in view non responsive if directly using an HDRI
- Fix LookDev behaviour when user delete the EnvironmentLibrary asset
- Fix LookDev SunPosition button position
- Fix LookDev EnvironmentLibrary tab when asset is deleted
- Fix LookDev used Cubemap when asset is deleted
- Fixed the definition of `rcp()` for GLES2.
- Fixed copy/pasting of Volume Components when loading a new scene
- Fix LookDev issue when adding a GameObject containing a Volume into the LookDev's view.
- Fixed duplicated entry for com.unity.modules.xr in the runtime asmdef file
- Fixed the texture curve being destroyed from another thread than main (case 1211754)
- Fixed unreachable code in TextureXR.useTexArray
- Fixed GC pressure caused by `VolumeParameter<T>.GetHashCode()`
- Fixed issue when LookDev window is opened and the CoreRP Package is updated to a newer version.
- Fix LookDev's camera button layout.
- Fix LookDev's layout vanishing on domain reload.
- Fixed issue with the shader TransformWorldToHClipDir function computing the wrong result.
- Fixed division by zero in `V_SmithJointGGX` function.
- Fixed null reference exception in LookDev when setting the SRP to one not implementing LookDev (case 1245086)
- Fix LookDev's undo/redo on EnvironmentLibrary (case 1234725)
- Fix a compil error on OpenGL ES2 in directional lightmap sampling shader code
- Fix hierarchicalbox gizmo outside facing check in symetry or homothety mode no longer move the center
- Fix artifacts on Adreno 630 GPUs when using ACES Tonemapping
- Fixed a null ref in the volume component list when there is no volume components in the project.
- Fixed issue with volume manager trying to access a null volume.
- HLSL codegen will work with C# file using both the `GenerateHLSL` and C# 7 features.

### Changed
- Restored usage of ENABLE_VR to fix compilation errors on some platforms.
- Only call SetDirty on an object when actually modifying it in SRP updater utility
- Set depthSlice to -1 by default on SetRenderTarget() to clear all slices of Texture2DArray by default.
- ResourceReloader will now add additional InvalidImport check while it cannot load due to AssetDatabase not available.
- Replaced calls to deprecated PlayerSettings.virtualRealitySupported property.
- Enable RWTexture2D, RWTexture2DArray, RWTexture3D in gles 3.1
- Updated macros to be compatible with the new shader preprocessor.
- Updated shaders to be compatible with Microsoft's DXC.
- Changed CommandBufferPool.Get() to create an unnamed CommandBuffer. (No profiling markers)
- Deprecating VolumeComponentDeprecad, using HideInInspector or Obsolete instead

## [7.1.1] - 2019-09-05

### Added
- Add separated debug mode in LookDev.

### Changed
- Replaced usage of ENABLE_VR in XRGraphics.cs by a version define (ENABLE_VR_MODULE) based on the presence of the built-in VR module
- `ResourceReloader` now works on non-public fields.
- Removed `normalize` from `UnpackNormalRGB` to match `UnpackNormalAG`.
- Fixed shadow routines compilation errors when "real" type is a typedef on "half".
- Removed debug menu in non development build.


## [7.0.1] - 2019-07-25

### Fixed
- Fixed a precision issue with the ACES tonemapper on mobile platforms.

## [7.0.0] - 2019-07-17

### Added
- First experimental version of the LookDev. Works with all SRP. Only branched on HDRP at the moment.
- LookDev out of experimental

## [6.7.0-preview] - 2019-05-16

## [6.6.0] - 2019-04-01
### Fixed
- Fixed compile errors in XRGraphics.cs when ENABLE_VR is not defined

## [6.5.0] - 2019-03-07

## [6.4.0] - 2019-02-21
### Added
- Enabled support for CBUFFER on OpenGL Core and OpenGL ES 3 backends.

## [6.3.0] - 2019-02-18

## [6.2.0] - 2019-02-15

## [6.1.0] - 2019-02-13

## [6.0.0] - 2019-02-23
### Fixed
- Fixed a typo in ERROR_ON_UNSUPPORTED_FUNCTION() that was causing the shader compiler to run out of memory in GLES2. [Case 1104271] (https://issuetracker.unity3d.com/issues/mobile-os-restarts-because-of-high-memory-usage-when-compiling-shaders-for-opengles2)

## [5.2.0] - 2018-11-27

## [5.1.0] - 2018-11-19
### Added
- Added a define for determining if any instancing path is taken.

### Changed
- The Core SRP package is no longer in preview.

## [5.0.0-preview] - 2018-10-18
### Changed
- XRGraphicConfig has been changed from a read-write control of XRSettings to XRGraphics, a read-only accessor to XRSettings. This improves consistency of XR behavior between the legacy render pipeline and SRP.
- XRGraphics members have been renamed to match XRSettings, and XRGraphics has been modified to only contain accessors potentially useful to SRP
- You can now have up to 16 additional shadow-casting lights.
### Fixed
- LWRP no longer executes shadow passes when there are no visible shadow casters in a Scene. Previously, this made the Scene render as too dark, overall.


## [4.0.0-preview] - 2018-09-28
### Added
- Space transform functions are now defined in `ShaderLibrary/SpaceTransforms.hlsl`.
### Changed
- Removed setting shader inclue path via old API, use package shader include paths

## [3.3.0] - 2018-01-01

## [3.2.0] - 2018-01-01

## [3.1.0] - 2018-01-01

### Added
- Add PCSS shadow filter
- Added Core EditMode tests
- Added Core unsafe utilities

### Improvements
- Improved volume UI & styling
- Fixed CoreUtils.QuickSort infinite loop when two elements in the list are equals.

### Changed
- Moved root files into folders for easier maintenance
