# Changelog
All notable changes to this project template will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [16.1.2] - 2025-12-24

### Added

- Fix lighting discrepencies due to problems during previous update.

## [16.1.1] - 2025-10-04

### Added

- Opened starting room with an outside pier, added water system and replaced Sky with PBR Sky with atmospheric scattering.

## [16.0.11] - 2025-01-27

### Changed

- Upgrade input system version to 1.12.0 so that a warning message is shown on platforms which require additional platform specific input system packages to be installed.
- Set InputHandler to input system exclusive

## [16.0.10] - 2024-07-03

### Fixed

- Frame Settings invalid enum in Screenshot Camera 6

## [16.0.9] - 2024-05-15

### Changed

- Fixed tutorial typos
- Updated tutorial documentation links

## [16.0.8] - 2024-04-05

### Changed

- Upgrade input system version to 1.8.1
- Set InputHandler to both
- Create InputActions.asset

## [16.0.7] - 2024-02-13

### Changed

- Downgraded input system version to 1.7.0

## [16.0.6] - 2024-01-31

### Changed

- Updated input system version
- Changed active input handler to Input System

## [16.0.5] - 2023-11-15

### Changed
- Changing the default quality from Low to Medium.

## [16.0.4] - 2023-09-01

### Changed

- Updated Environment Volume to fix an issue where no sky was attached
- Updated Tutorial start section

### Added

- Added subtle SSLF effect by default

## [16.0.3] - 2023-06-22

### Changed
- Removed Global Volume from scene (Default Volume Profile is now configured only through HDRP Global Settings)
- Updated Tutorial for Probe Volumes

## [16.0.2] - 2023-06-12

### Changed
Changes a few objects to no longer use lightmaps (bamboo floor, bamboo cage, long table 3rd room, column 3rd room)

### Added
- Added Adaptative probe volume support and tutorials
- Removes previous probe system and tutorials

## [16.0.1] - 2023-05-15

### Changed
- Enable occlusion for all lens flares

## [16.0.0] - 2023-05-10

### Changed

- Removed deprecated TextMeshPro package dependency
- Upgraded UGUI package to version 2.0.0

## [15.1.3] - 2023-03-10

### Added
- Added water rendering override to default volume profile

### Changed
- Set D3D12 as a default for UWP platforms
- Check incremental GC in player settings

### Fixed
- Fixed vfx diffusion profile reference

## [15.1.2] - 2022-12-08

### Changed
- For Ray Tracing and Ray Tracing(Realtime GI) assets, DLLS was enabled. Forced resolution of 75%
- 3rd Person controller camera properties were adjusted. Its basically now closer.
- Default quality levels changed for consoles
- Bloom scattering reduction to 0.4
- Removes the local duplicated Diffusion Profiles (Skin & Foliage) to rely on those provided in HDRP package

### Fixed
- Fixed an issue with IET layout

## [15.1.1] - 2022-11-09

### Fixed
- Remove default sounds from the 3rd person controller asset
- Remove a debug plane gameobject

## [15.1.0] - 2022-07-06

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

## [15.0.0] - 2022-07-06

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
