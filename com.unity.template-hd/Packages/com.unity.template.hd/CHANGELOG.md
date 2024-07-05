# Changelog
All notable changes to this project template will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [14.1.5] - 2024-07-03

### Fixed
- Frame Settings invalid enum in Screenshot Camera 6

## [14.1.4] - 2023-05-15

### Changed
- Enable occlusion for all lens flares

## [14.1.3] - 2023-03-10

### Changed
- Set D3D12 as a default for UWP platforms
- Check incremental GC in player settings

### Fixed
- Fixed vfx diffusion profile reference

## [14.1.2] - 2022-12-08

### Changed
- For Ray Tracing and Ray Tracing(Realtime GI) assets, DLLS was enabled. Forced resolution of 75%
- 3rd Person controller camera properties were adjusted. Its basically now closer.
- Default quality levels changed for consoles
- Bloom scattering reduction to 0.4
- Removes the local duplicated Diffusion Profiles (Skin & Foliage) to rely on those provided in HDRP package

### Fixed
- Fixed an issue with IET layout

## [14.1.1] - 2022-11-09

### Fixed
- Remove default sounds from the 3rd person controller asset
- Remove a debug plane gameobject
- Update all obsolete materials

## [14.1.0] - 2022-07-06

### Added
- Added new HDRP assets "Very High" and "Ultra" and corresponding quality levels have been created for raytracing
- Added new 3rd person character control 
- Added "Additional properties" page for the IET
- Added missing prefab edition scene

### Fixed
- Fixed issue with new hdrp cubemap atlas system
- Fixed wrong position for the collision proxy of the lounge chair

### Changed
- Switch to DX12 by default undre windows
- IET has been updated
- Updated Images showing the Unity material ball
- The default low/medium/high values for SSR and RTAO have been retuned to offer better results out of the box

## [14.0.0] - 2022-07-06

### Fixed
- Fixed issue with new hdrp cubemap atlas system

## [13.4.1] - 2022-03-16

### Added
- Added PS5 to QualitySettings.asset.
- Added GameCore platforms to QualitySettings.asset.

## [13.4.0] - 2021-10-18

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [13.3.0] - 2021-10-18

### Fixed
- Convert Unity Sphere to new one

## [13.2.0] - 2021-10-18

### Fixed
- Fixed new input system codepath not working if you had a controller but no keyboard or mouse.

## [13.1.0] - 2021-09-28

### Changed
- Update manifest to include required packages

## [13.0.0] - 2021-09-01

### Changed
- Update SRP package to 13.0.0

## [12.2.0] - 2021-08-25

### Changed
- Add tutorial mode

## [12.1.1] - 2021-08-10

### Changed
- Fixed a warning for `FR_Platform_01_LOD0`

## [12.1.0] - 2021-01-11

### Added
- Add support for Realtime Enlighten

## [12.0.0] - 2021-01-11

### Changed
- Update SRP package to 12.0.0

## [11.0.0] - 2020-10-21

### Changed
- Update SRP package to 11.0.0

## [10.2.0] - 2020-10-19

### Changed
- Update SRP package to 10.2.0

## [10.1.0] - 2020-10-12

### Added
- Initial creation of 2020 HD Template
