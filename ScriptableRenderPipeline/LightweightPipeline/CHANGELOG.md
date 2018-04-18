# Changelog

## [1.1.4-preview]
### Improvements
 - Terrain and grass shaders ported
 - Updated materials and shader default albedo and specular color to midgrey.
 - Exposed _ScaledScreenParams to shader. It works the same as _ScreenParams but takes pipeline RenderScale into consideration
 - Performance Improvements in mobile
 
### Bugfixes
 - Fixed SRP Shader library issue that was causing all constants to be highp in mobile
 - Fixed shader error that prevented LWRP to build to UWP
 - Fixed shader compilation errors in Linux due to case sensitive includes
 - Fixed Rendering Texture flipping issue
 - Fixed Standard Particles shader cutout and blending modes
 - Fixed crash caused by using projectors
 - Fixed issue that was causing Shadow Strength to not be computed on mobile
 - Fixed Material Upgrader issue that caused editor to SoftLocks
 - Fixed GI in Unlit shader
 - Fixed null reference in the Unlit material shader GUI

## [1.1.2-preview]
### Improvements
 - Performance improvements in mobile  
  
### Bugfixes
 - Fixed shadows on GLES 2.0
 - Fixed CPU performance regression in shadow rendering
 - Fixed alpha clip shadow issues
 - Fixed unmatched command buffer error message
 - Fixed null reference exception caused by missing resource in LWRP
 - Fixed issue that was causing Camera clear flags was being ignored in mobile

## [1.1.1-preview]
### Improvements
 - Added Cascade Split selection UI
 - Shadowmap uses 16bit format instead of 32bit.
 - Added SHADER_HINT_NICE_QUALITY. If user defines this to 1 in the shader Lightweight pipeline will favor quality even on mobile platforms.
 - Small shader performance improvements

### Bugfixes
 - Fixed Subtractive Mode
 - Shadow Distance does not accept negative values anymore

## [0.1.24]
### Improvements
 - Refactored lightweight standard shaders and shader library to improve ease of use.
 - Added Light abstraction layer on lightweight shader library.
 - HDR RT now uses what format is configured in Tier settings.
 - Optimized tile LOAD op on mobile.
 - Added HDR global setting on pipeline asset. 
 - Added Soft Particles settings on pipeline asset.
 - Ported particles shaders to SRP library
 - Reduced GC pressure
 - Reduced shader variant count by ~56% by improving fog and lightmap keywords
 - Converted LW shader library files to use real/half when necessary.

### Bugfixes
 - Fixed realtime shadows on OpenGL
 - Fixed shader compiler errors in GLES 2.0
 - Fixed issue sorting issues when BeforeTransparent custom fx was enabled.
 - Fixed VR single pass rendering.
 - Fixed viewport rendering issues when rendering to backbuffer.
 - Fixed viewport rendering issues when rendering to with MSAA turned off.
 - Fixed multi-camera rendering.

## [0.1.23]
### Improvements
 - Shaders ported to the new SRP shader library. 
 - Constant Buffer Refactor to use new Batcher
 - Shadow filtering and bias improved.
 - Pipeline now updates color constants in gamma when in Gamma colorspace.
 - UI Improvements (Rendering features not supported by LW are hidden)
 - Optimized ALU and CB usage on Shadows.
 - Reduced shader variant count by ~33% by improving shadow and light classification keywords
 - Default resources were removed from the pipeline asset.
 
### Bugfixes
 - Fixed shader include path when using SRP from package manager.
 - Fixed spot light attenuation to match Unity Built-in pipeline.
 - Fixed depth pre-pass clearing issue.

## [0.1.12]
### Improvements
 - Realtime shadow filtering was improved. 
 - Standard Unlit shader now has an option to sample GI.
 - Added Material Upgrader for stock Unity Mobile and Legacy Shaders.
 - UI improvements

### Bugfixes
 - Fixed an issue that was including unreferenced shaders in the build.
 - Fixed a null reference caused by Particle System component lights.

## [0.1.11]
 Initial Release.




