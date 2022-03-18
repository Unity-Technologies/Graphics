# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [14.0.32] - 2021-03-09

This release note are for version 2022.a10

## [14.0.21] - 2021-02-07

This release note are for version 2022.a9

## [14.0.11] - 2021-01-04

This release note are for version 2022.a8

### Added
- Added denoising for the path tracer.
- Added an initial version of under water rendering for the water system.
- Added option to animate APV sample noise to smooth it out when TAA is enabled.
- Added default DOTS compatible loading shader (MaterialLoading.shader)
- Add #pragma editor_sync_compilation directive to MaterialError.shader
- Added the culling matrix and near plane for lights, so that they can be custom-culled with the BatchRenderGroup API.
- Added an optional CPU simulation for the water system.
- Added new Unity material ball matching the new Unity logo.

### Changed
- Moved custom Sensor Lidar path tracing code to the SensorSDK package.
- Optimized real time probe rendering by avoid an unnecessary copy per face.

### Fixed
- Fixed couple bugs in the volumetric clouds shader code.
- Fixed PBR Dof using the wrong resolution for COC min/max filter, and also using the wrong parameters when running post TAAU stabilization. (case 1388961)
- Fixed the list of included HDRP asset used for stripping in the build process.
- Fixed HDRP camera debug panel rendering foldout.
- Fixed issue with Final Image Histogram displaying a flat histogram on certain GPUs and APIs.
- Fixed various issues with render graph viewer when entering playmode.
- Fixed Probe Debug view misbehaving with fog.
- Fixed issue showing controls for Probe Volumes when Enlighten is enabled and therefore Probe Volumes are not supported.
- Fixed null reference issue in CollectLightsForRayTracing (case 1398381)
- Fixed camera motion vector pass reading last frame depth texture
- Fixed issue with shader graph custom velocity and VFX (case 1388149)
- Fixed motion vector rendering with shader graph with planar primitive (case 1398313)
- Fixed issue in APV with scenes saved only once when creating them.
- Fixed probe volume baking not generating any probes on mac.
- Fix a few UX issues in APV.
- Fixed issue with detail normals when scale is null (case 1399548).
- Fixed compilation errors on ps5 shaders.

