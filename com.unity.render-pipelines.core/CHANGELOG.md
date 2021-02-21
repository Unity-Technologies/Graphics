# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [10.4.0] - 2020-01-26

### Added
- Support for the XboxSeries platform has been added.

### Fixed
- Fixed parameters order on inspectors for Volume Components without custom editor
- Fixed the display name of a Volume Parameter when is defined the attribute InspectorName

## [10.3.1] - 2020-01-26

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [10.3.0] - 2020-11-16

### Added
- New function in GeometryTools.hlsl to calculate triangle edge and full triangle culling.
- Support for the PlayStation 5 platform has been added.

### Fixed
- Fixed a bug in FreeCamera which would only provide a speed boost for the first frame when pressing the Shfit key.
- Fixed missing warning UI about Projector component being unsupported (case 1300327).

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
