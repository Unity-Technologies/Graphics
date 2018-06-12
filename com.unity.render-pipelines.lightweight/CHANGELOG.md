# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Fixed
- GLCore compute buffer compiler error
- Occlusion strength not being applied on LW standard shaders
- CopyDepth pass is being called even when a depth from prepass is available
- GLES2 shader compiler error in IntegrationTests
- Can't set RenderScale and ShadowDistance by script
- VR Single Pass Instancing shadows

## [2.0.0-preview]

### Added
- Explicit render target load/store actions were added to improve tile utilization
- Camera opaque color can be requested on the pipeline asset. It can be accessed in the shader by defining a _CameraOpaqueTexture. This can be used as an alternative to GrabPass.
- Dynamic Batching can be enabled in the pipeline asset
- Pipeline now strips unused or invalid variants and passes based on selected pipeline capabilities in the asset. This reduces build and memory consuption on target.
- Shader stripping settings were added to pipeline asset

### Changed
#### Pipeline
- Pipeline code is now more modular and extensible. A ForwardRenderer class is initialized by the pipeline with RenderingData and it's responsible for enqueueing and executing passes. In the future pluggable renderers will be supported.
- On mobile 1 directional light + up to 4 local lights (point or spot) are computed
- On other platforms 1 directional light + up to 8 local lights are computed
- Multiple shadow casting lights are supported. Currently only 1 directional + 4 spots light shadows.
#### Shading Framework
- Directional Lights are always considered a main light in shader. They have a fast shading path with no branching and no indexing.
- GetMainLight() is provided in shader to initialize Light struct with main light shading data. 
- Directional lights have a dedicated shadowmap for performance reasons. Shadow coord always comes from interpolator.
- MainLigthRealtimeShadowAttenuation(float4 shadowCoord) is provided to compute main light realtime shadows.
- Spot and Point lights are always shaded in the light loop. Branching on uniform and indexing happens when shading them.
- GetLight(half index, float3 positionWS) is provided in shader to initialize Light struct for spot and point lights.
- Spot light shadows are baked into a single shadow atlas.
- Shadow coord for spot lights is always computed on fragment.
- Use LocalLightShadowAttenuation(int lightIndex, float3 positionWS) to comppute realtime shadows for spot lights.

### Fixed
- Issue that was causing VR on Android to render black
- Camera viewport issues
- UWP build issues
- Prevent nested camera rendering in the pipeline

## [1.1.4-preview]

### Added
 - Terrain and grass shaders ported
 - Updated materials and shader default albedo and specular color to midgrey.
 - Exposed _ScaledScreenParams to shader. It works the same as _ScreenParams but takes pipeline RenderScale into consideration
 - Performance Improvements in mobile

### Fixed
 - SRP Shader library issue that was causing all constants to be highp in mobile
 - shader error that prevented LWRP to build to UWP
 - shader compilation errors in Linux due to case sensitive includes
 - Rendering Texture flipping issue
 - Standard Particles shader cutout and blending modes
 - crash caused by using projectors
 - issue that was causing Shadow Strength to not be computed on mobile
 - Material Upgrader issue that caused editor to SoftLocks
 - GI in Unlit shader
 - Null reference in the Unlit material shader GUI

## [1.1.2-preview]

### Changed
 - Performance improvements in mobile  

### Fixed
 - Shadows on GLES 2.0
 - CPU performance regression in shadow rendering
 - Alpha clip shadow issues
 - Unmatched command buffer error message
 - Null reference exception caused by missing resource in LWRP
 - Issue that was causing Camera clear flags was being ignored in mobile


## [1.1.1-preview]

### Added
 - Added Cascade Split selection UI
 - Added SHADER_HINT_NICE_QUALITY. If user defines this to 1 in the shader Lightweight pipeline will favor quality even on mobile platforms.
 
### Changed
 - Shadowmap uses 16bit format instead of 32bit.
 - Small shader performance improvements

### Fixed
 - Subtractive Mode
 - Shadow Distance does not accept negative values anymore

 
## [0.1.24]

### Added
 - Added Light abstraction layer on lightweight shader library.
 - Added HDR global setting on pipeline asset. 
 - Added Soft Particles settings on pipeline asset.
 - Ported particles shaders to SRP library
 
### Changed
 - HDR RT now uses what format is configured in Tier settings.
 - Refactored lightweight standard shaders and shader library to improve ease of use.
 - Optimized tile LOAD op on mobile.
 - Reduced GC pressure
 - Reduced shader variant count by ~56% by improving fog and lightmap keywords
 - Converted LW shader library files to use real/half when necessary.
 
### Fixed
 - Realtime shadows on OpenGL
 - Shader compiler errors in GLES 2.0
 - Issue sorting issues when BeforeTransparent custom fx was enabled.
 - VR single pass rendering.
 - Viewport rendering issues when rendering to backbuffer.
 - Viewport rendering issues when rendering to with MSAA turned off.
 - Multi-camera rendering.

## [0.1.23]

### Added
 - UI Improvements (Rendering features not supported by LW are hidden)

### Changed
 - Shaders were ported to the new SRP shader library. 
 - Constant Buffer refactor to use new Batcher
 - Shadow filtering and bias improved.
 - Pipeline now updates color constants in gamma when in Gamma colorspace.
 - Optimized ALU and CB usage on Shadows.
 - Reduced shader variant count by ~33% by improving shadow and light classification keywords
 - Default resources were removed from the pipeline asset.

### Fixed
 - Fixed shader include path when using SRP from package manager.
 - Fixed spot light attenuation to match Unity Built-in pipeline.
 - Fixed depth pre-pass clearing issue.

## [0.1.12]

### Added
 - Standard Unlit shader now has an option to sample GI.
 - Added Material Upgrader for stock Unity Mobile and Legacy Shaders.
 - UI improvements

### Changed
- Realtime shadow filtering was improved. 

### Fixed
 - Fixed an issue that was including unreferenced shaders in the build.
 - Fixed a null reference caused by Particle System component lights.


## [0.1.11]
 Initial Release.
