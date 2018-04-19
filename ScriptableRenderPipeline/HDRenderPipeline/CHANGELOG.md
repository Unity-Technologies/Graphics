# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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
- Generalize and SampleVBuffer() to handle different sampling and reconstruction methods.
- Improve the quality of volumetric lighting reprojection.
- Optimize Morton Order code in the Subsurface Scattering pass.

### Removals and deprecations
- 
