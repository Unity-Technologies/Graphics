# Changelog
All notable changes to this project template will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [7.3.1] - 2020-03-13
- Update HDRP package to 7.3.0

## [7.2.0] - 2020-01-31
- Update HDRP package to 7.2.1

## [7.1.8] - 2020-01-20
- Update HDRP package to 7.1.8

## [7.1.7] - 2019-12-16
- Update HDRP package to 7.1.7

## [7.1.6] - 2019-11-22
- Update HDRP package to 7.1.6
- Update Samplescene lights to use scalability settings and set correct radius and angular diameter property

## [7.1.5] - 2019-11-15
- Update HDRP package to 7.1.5

## [7.1.2] - 2019-09-13

 - Physics2D.reuseCollisionCallbacks now defaults to true

## [7.1.1] - 2019-09-05

- Updated HDRP Package
- Updated Fog components

## [7.0.3] - 2019-08-09

- Added Stadia to QualitySettings.asset

## [7.0.2] - 2019-08-05
- PlayerSettings.graphicsJobs is now false for Mac, iOS, Android, tvOS platforms

## [7.0.1] - 2019-07-25
- Updated HDRP package to 7.0.1

## [7.0.0] - 2019-07-12
- Updated HDRP package to 7.0.0

## [4.0.0] - 2019-04-30
- removed legacyinputhelpers package from the manifest.
- rolled major version to support new version of unity

## [3.2.4] - 2019-04-23
- Graphics API for iOS is not longer automatic and removed GLES2 from list

## [3.2.3] - 2019-03-15
- Fixed incorrect default property setting for ProjectSettings.SupportedNpadStyles 

## [3.2.2] - 2019-03-13
- EditorSettings.lineEndingsForNewScripts property now defaults to OSNative.

## [3.2.1] - 2019-03-05
- PlayerSettings.displayResolutionDialog property now defaults to false.

## [3.2.0] - 2019-02-21
- Fixing a bug in the Readme.asset .
- Updating HD version for bug fixes.

## [3.1.0] - 2019-02-19
- Updating readme with information for package manager. 

## [3.0.0] - 2019-02-12
- Updating HD version 6.0.0-preview
- Removing deprecating packages from manifest

## [2.4.0] - 2019-02-11
- Graphics API for LInux is now manually set to Vulkan.

## [2.3.0] - 2019-02-08
- setup post processing v3
- remove post processing v2 profiles
- apply TAA on the camera
- tweak sunlight color and intensity
- reset HDRP asset default values
- change max shadow distance and cascade settings to fix https://fogbugz.unity3d.com/f/cases/1098489/
- keep only one quality setting
- populate HDRPDefaultResources folder for the new "New scene" workflow
- Player settings : uncheck "Clamp blendshapes"
- Graphics settings : tier settings reset to default
- Preset manager : Remove default presets for diffusion profile asset and light

## [2.2.0] - 2019-02-04
- Corrected some default values in project settings.

## [2.1.0] 2019-02-01
- Fixed `-preview` tag on HD version 5.3.1
- Updated default settings in HDRP Asset

## [2.0.0] - 2019-01-30
- Updating HD version 5.3.1
- Enable HoloLens `depthBufferSharingEnabled` by default.

## [1.4.2] - 2019-01-22

### Changed
- Removing unneeded manifest entries

## [1.4.1] - 2018-12-07

### Changed
- Updating HD version 5.2.3

## [1.4.0] - 2018-12-06

### Changed
- Updating HD version 5.2.2-preview
- Changed antialiasing to TAA

## [1.3.0] - 2018-11-27

### Changed
- Updating HD version 5.2.1-preview

## [1.2.0] - 2018-11-27

### Changed
- Updating HD version 5.2.0-preview
- Update new project templates to use 4.x scripting runtime

## [1.1.1] - 2018-11-08

### Fixed
- Physics.reuseCollisionCallbacks property now defaults to true.
- Physics2D.reuseCollisionCallbacks property now defaults to true.
- Physics.autoSyncTransforms property now defaults to false.
- Physics2D.autoSyncTransforms property now defaults to false.

## [1.1.0] - 2018-10-24

### Changed
- Updating HD version
- AndroidTVCompatibility to false

## [1.0.6] - 2018-09-24

### Changed
- Oculus XR settings default to dash support and depth export enabled.
- updated default webgl memory size
- updated default upload manager ring buffer to 16mb
- HD updated to 4.0.0-preview
- updating PP Vinette to be less extreme
- fixing position of reflection probes 

## [1.0.5] - 2018-09-06

### Changed
- Updated HD version number

## [1.0.4] - 2018-07-17

### Changed
- Migrating old templates into package format 
- Updating version
- adjusting spot light value for upgrade
- adding collider to ground

## [1.0.3] - 2018-06-01

### Changed
- Package updates
- Static Mesh import settings have been updated to show best options (was default import settings before)
- Fixed default values for SSS
- Fixed default values for probe cache size
- Fog Height Attenuation updated
- Adding to readme about hdri asset store item

## [1.0.2] - 2018-x-xx

### Changed
- Blendshape setting, version update

## [1.0.1] - 2018-x-xx

### Changed
- Version Update

## [1.0.0] - 2018-2-25

### Added
- Sample static meshes to show best practices
- Light probs
- Reflection Probs

### Changed
- Removed cinemachine and text mesh pro
- Updated package version for HD 
- Additional setting and lighting polish
- Removing basic content (red cube)

## [0.0.5] - 2018-1-26

### Changed
- Updating to HD version 0.1.26, changes to lighting settings

## [0.0.4] - 2017-12-20

### Changed
- Updating to HD version 0.1.21

## [0.0.3] - 2017-12-18

### Changed
- Removing motion blur from post 

## [0.0.2] - 2017-12-15

### Added
- Scene settings and basic sample content setup appropriately for the High Definition render pipeline

###Changed
- Updated to include important settings for High Definition render pipeline.

## [0.0.1] - 2017-12-07

### Added 
- Initial creation of HD Template *Unity Package \com.unity.template.HD*.
