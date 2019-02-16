# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [4.10.0-preview] - 2019-02-16

## [4.9.0-preview] - 2019-01-28

## [4.8.0-preview] - 2019-01-16

## [4.7.0-preview] - 2019-01-13

## [4.6.0-preview] - 2018-12-07

### Fixed
- Fixed a typo in ERROR_ON_UNSUPPORTED_FUNCTION() that was causing the shader compiler to run out of memory in GLES2. [Case 1104271] (https://issuetracker.unity3d.com/issues/mobile-os-restarts-because-of-high-memory-usage-when-compiling-shaders-for-opengles2)

## [4.3.0-preview] - 2018-11-23

## [4.2.0-preview] - 2018-11-16

### Added
- Added a define for determining if any instancing path is taken.

## [4.1.0-preview] - 2018-10-18

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

## [3.3.0]

## [3.2.0]

## [3.1.0]

### Added
- Add PCSS shadow filter
- Added Core EditMode tests
- Added Core unsafe utilities

### Improvements
- Improved volume UI & styling
- Fixed CoreUtils.QuickSort infinite loop when two elements in the list are equals.

### Changed
- Moved root files into folders for easier maintenance

## [0.1.6] - 2018-xx-yy

### Changelog starting

Started Changelog
