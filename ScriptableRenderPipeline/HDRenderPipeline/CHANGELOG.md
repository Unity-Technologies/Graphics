# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
 - Planar Reflection Probe support roughness (gaussian convolution of captured probe)
 - Screen Space Refraction projection model (Proxy raycasting, HiZ raymarching)
 - Screen Space Refraction settings as volume component
 - Added buffered frame history per camera

### Changed
 - Depth and color pyramid are properly computed and sampled when the camera renders inside a viewport of a RTHandle.
 - Forced Planar Probe update modes to (Realtime, Every Update, Mirror Camera)
 - Removed Planar Probe mirror plane position and normal fields in inspector, always display mirror plane and normal gizmos
 - Screen Space Refraction proxy model uses the proxy of the first environment light (Reflection probe/Planar probe) or the sky
 - Moved RTHandle static methods to RTHandles
 - Renamed RTHandle to RTHandleSystem.RTHandle

## [0.1.6] - 2018-xx-yy

### New features and functionality
- 

### Bug fixes
- Fix numerical issues in IntersectRayAABB().
- Fix alpha blending of volumetric lighting with transparent objects.
- Fix the near plane of the V-Buffer causing out-of-bounds look-ups in the clustered data structure.

### Miscellaneous improvements
- Port Global Density Volumes to the Interpolation Volume System.
- Optimize ImportanceSampleLambert() to not require the tangent frame.
- Generalize SampleVBuffer() to handle different sampling and reconstruction methods.
- Improve the quality of volumetric lighting reprojection.
- Optimize Morton Order code in the Subsurface Scattering pass.

### Removals and deprecations
- 
