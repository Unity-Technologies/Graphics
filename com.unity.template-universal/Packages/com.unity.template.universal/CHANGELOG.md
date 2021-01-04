# Changelog
All notable changes to this project template will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

### Changed 
- Update SRP package to 11.0.0

## [10.2.0] - 2020-10-19

### Changed 
- Update SRP package to 10.2.0

## [10.1.0] - 2020-10-12

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [10.0.0] - 2020-08-03

### Changed
- Updated version to Universal RP version 10.0.0
- EditorSettings.lineEndingsForNewScripts property now defaults to OSNative

### Fixed
- Fixed camera from always rendering into a RenderTexture, this caused un-needed performance slowdown

## [7.1.7] - 2019-12-03
- Editor will force text serialization to occur on one line

## [7.1.6] - 2019-11-22
- Update version to Universal RP version 7.1.6

## [7.1.5] - 2019-11-15
- Update version to Universal RP version 7.1.5

## [7.1.2] - 2019-10-10
 - Physics.reuseCollisionCallbacks now defaults to true
 - Physics2D.reuseCollisionCallbacks now defaults to true

## [7.1.1] - 2019-09-04
- Updated to Universal RP version 7.1.1
- Applied Quality pipeline assets to their respective quality levels
- Organized prefabs into Nested Prefabs for cleaner structure
- Fixed double materials on the Jigsaw

## [7.0.2] - 2019-08-05
- PlayerSettings.graphicsJobs is now false for Mac, iOS, Android, tvOS platforms

## [7.0.1] - 2019-07-25
- Updated to SRP package 7.0.1

## [7.0.0] - 2019-07-12
- Converted template to Universal RP
- Updated to SRP package 7.0.0
- Fixed inconsistencies between pipeline asset settings
- Updated readme to better reflect current status and info for Universal RP
- Updated icon
- Converted Post-processing from v2 to v3

## [6.9.0] - 2019-07-04
- Updated to SRP package 6.9.0
- Remove VSTU package since we will not ship it

## [3.7.1] - 2019-06-19
- Updated to SRP package 6.8.1

## [3.7.0] - 2019-06-13
- Updated to SRP package 6.8.0

## [3.6.0] - 2019-05-17
- Updated to SRP package 6.7.0
- Adding test framework package to manifest. 
- Updating project settings to make sure the sample scene is opened on load.

## [3.5.2] - 2019-03-15
- Fixed incorrect default property setting for ProjectSettings.SupportedNpadStyles 

## [3.5.1] - 2019-03-13
- PlayerSettings.legacyClampBlendShapeWeights property now defaults to false.
- EditorSettings.lineEndingsForNewScripts property now defaults to OSNative.

## [3.5.0] - 2019-03-12
- Updating text mesh pro version to 2.0.0.

## [3.4.0] - 2019-03-09
- Updating LWRP version to 6.5.2

## [3.3.0] - 2019-03-07
- Updating LWRP version to 6.5.0

## [3.2.1] - 2019-03-05
- PlayerSettings.displayResolutionDialog property now defaults to false.

## [3.2.0] - 2019-02-21
- Fixing an error in the readme.asset .
- Updating the LW version for bugfixes.

## [3.1.0] - 2019-02-19
- Adding information about package manager to the readme.

## [3.0.0] - 2019-02-12
- Updating LW package version
- Removing deprecated packages from manifest

## [2.1.0] - 2019-02-04
- Corrected some default values in project settings.

## [2.0.0] - 2019-01-30

### Changed
- Enabled HoloLens `depthBufferSharingEnabled` by default.
- LW version updated to 5.3.1

## [1.4.2] - 2019-01-22

### Changed
- Removed unneeded manifest packages

## [1.4.1] - 2018-12-07

### Changed
- LW version updated to 5.2.3

## [1.4.0] - 2018-12-06

### Changed
- LW version updated to 5.2.2
- Directional Light is now set to realtime
- Main light shadow resolution is now 2084px

## [1.3.0] - 2018-11-30

### Changed
- LW version updated to 5.2.1

## [1.2.0] - 2018-11-27

### Changed
- android-vulkan-default
- LW version updated to 5.2.0
- Update new project templates to use 4.x scripting runtime

## [1.1.1] - 2018-11-08

### Fixed
- Physics.reuseCollisionCallbacks property now defaults to true.
- Physics2D.reuseCollisionCallbacks property now defaults to true.
- Physics.autoSyncTransforms property now defaults to false.
- Physics2D.autoSyncTransforms property now defaults to false.

## [1.1.0] - 2018-10-24

### Changed
- Updating LW Version 
- AndroidTVCompatibility to false


## [1.0.6] - 2018-09-24

### Changed
- Oculus XR settings default to dash support and depth export enabled.
- Updating default webgl memory size
- Updating default upload manager ring buffer size to 16 mb
- removing platform overrides for textures presets
- updating lw version to 4.0.0-preview

## [1.0.5] - 2018-09-06

### Changed
- LW Version update to 3.3.0

## [1.0.4] - 2018-07-16

### Changed
- LW Version update to 3.0.0
- Adding collision to floor mesh

## [1.0.3] - 2018-06-06

### Changed
- Migrating old lightweight templates into package format 

## [1.0.2] - 2018-06-01

### Changed
- Lightweight Package version updated to "com.unity.render-pipelines.lightweight": "1.1.10-preview"
- Static Mesh import settings have been updated to show best options (was default import settings before)
- Texture import settings updated with platform size override (4k for andriod and ios) (all textures already much smaller than this fyi)
- Audio preset updated with platform differences for ios and android. Ios is always MP3 and Android is always Vorbis
- Texture preset max size forces to 4k for androidand ios
- Exit sample added to camera script
- Fixed Timestep in Time Manger updated from 0.0167 to 0.02
- Removed Vertex Lighting from all lightweight assets
- Added soft shadows to Lightweight high quality and medium quality assets

## [1.0.0] - 2018-02-25

### Added
- Sample static meshes to show best practices
- Light probs
- Reflection Probs

### Changed
- Removed cinemachine and text mesh pro
- Updated package version for LW 
- Additional setting and lighting polish
- Removing basic content (red cube)

## [0.0.5] - 2018-01-29

### Added
- cinemachine and text mesh pro packages

## [0.0.4] - 2018-01-29

### Added
- cinemachine and text mesh pro packages

## [0.0.3] - 2018-01-26 

### Changed 
- Updating Shadergraph and lightweight to new version
- Updating lighting settings based on internal feedback from lighting team

## [0.0.2] - 2017-12-12
### Added
- Packages for Lightweight SRP and Shadergraph
- Simple example content - red cube 

### Changed
- Project and Lighting Settings adjusted for use with Lightweight Render Pipeline

## [0.0.1] - 2017-12-05

### Added
- Initial Project Creation for Unity Lightweight Project Template \com.unity.template.lightweight.

