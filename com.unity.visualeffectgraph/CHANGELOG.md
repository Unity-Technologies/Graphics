# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).


## [6.9.0-preview] - 2019-07-05
### Fixed
- Handle a possible exception (ReflectionTypeLoadException) while using VFXParameterBinderEditor

## [6.8.0-preview] - 2019-06-12
### Added
- Make multiselection work in a way that do not assume that the same parameter will have the same index in the property sheet.
- auto recompile when changing shaderpath
- auto recompile new vfx
- better detection of default shader path
- Bitfield control.
- Move HDRP outputs to HDRP package + expose HDRP queue selection
- Add exposure weight control for HDRP outputs
- Subgraphs

## [6.7.0-preview] - 2019-05-21
### Added
- Distortion Outputs (Quad / Mesh)
- Motion-Vector-Based Flipbook Interpolation
- Color mapping mode for unlit outputs (Textured/Gradient Mapped)
- Add Triangle and Octagon primitives for particle outputs
- Set Attribute is now spaceable on a specific set of attributes (position, velocity, axis...)
- Trigger : GPUEvent Rate (Over time or Distance)

### Fixed
- Fix shader compilation error with debug views
- Improve AA line rendering
- Fix screen space size block
- Crash chaining two spawners each other [Case 1135299](https://issuetracker.unity3d.com/issues/crash-chaining-two-spawners-to-each-other-produces-an-infinite-loop)
- Inspector : Exposed parameters disregard the initial value [Case 1126471](https://issuetracker.unity3d.com/issues/parameters-exposed-parameters-disregard-the-initial-value)
- Asset name now displayed in compile errors and output context shaders
- Fix for linking spawner to spawner while first spawner is linked to initialize + test 
- Fix space of spaceable slot not copy pasted + test
- Position (Circle) does not take the Center Z value into account [Case 1146850](https://issuetracker.unity3d.com/issues/blocks-position-circle-does-not-take-the-center-z-value-into-account)
- Add Exposure Weight for emissive in lit outputs

## [6.6.0-preview] - 2019-04-01
### Added
- Addressing mode for Sequential blocks
- Invert transform available on GPU
- Add automatic depth buffer reference for main camera (for position and collision blocks)
- Total Time for PreWarm in Visual Effect Asset inspector
- Support for unlit output with LWRP
- Add Terrain Parameter Binder + Terrain Type
- Add UI Parameter Binders : Slider, Toggle
- Add Input Parameter Binders : Axis, Button, Key, Mouse, Touch
- Add Other Parameter Binders : Previous Position, Hierarchy Attribute Map, Multi-Position, Enabled

### Fixed
- Undo Redo while changing space
- Type declaration was unmodifiable due to exception during space intialization
- Fix unexpected issue when plugging per particle data into hash of per component fixed random
- Missing asset reimport when exception has been thrown during graph compilation
- Fix exception when using a Oriented Box Volume node [Case 1110419](https://issuetracker.unity3d.com/issues/operator-indexoutofrangeexception-when-using-a-volume-oriented-box-node)
- Add missing blend value slot in Inherit Source Attribute blocks [Case 1120568](https://issuetracker.unity3d.com/issues/source-attribute-blend-source-attribute-blocks-are-not-useful-without-the-blend-value)
- Visual Effect Inspector Cosmetic Improvements
- Missing graph invalidation in VFXGraph.OnEnable, was causing trouble with value invalidation until next recompilation
- Issue that remove the edge when dragging an edge from slot to the same slot.
- Exception when undoing an edge deletion on a dynamic operator. 
- Exception regarding undo/redo when dragging a edge linked to a dynamic operator on another slot.
- Exception while removing a sub-slot of a dynamic operator

## [6.5.0-preview] - 2019-03-07

## [6.4.0-preview] - 2019-02-21 

## [6.3.0-preview] - 2019-02-18

## [6.2.0-preview] - 2019-02-15
### Changed
- Code refactor: all macros with ARGS have been swapped with macros with PARAM. This is because the ARGS macros were incorrectly named

### Fixed
- Better Handling of Null or Missing Parameter Binders (Editor + Runtime)
- Fixes in VFX Raycast Binder
- Fixes in VFX Parameter Binder Editor

## [6.1.0-preview] - 2019-02-13

## [6.0.0-preview] - 2019-02-23
### Added
- Add spawnTime & spawnCount operator
- Add seed slot to constant random mode of Attribute from curve and map
- Add customizable function in VariantProvider to replace the default cartesian product
- Add Inverse Lerp node
- Expose light probes parameters in VisualEffect inspector

### Fixed
- Some fixes in noise library
- Some fixes in the Visual Effect inspector
- Visual Effects menu is now in the right place
- Remove some D3D11, metal and C# warnings
- Fix in sequential line to include the end point
- Fix a bug with attributes in Attribute from curve
- Fix source attributes not being taken into account for attribute storage
- Fix legacy render path shader compilation issues
- Small fixes in Parameter Binder editor
- Fix fog on decals
- Saturate alpha component in outputs
- Fixed scaleY in ConnectTarget
- Incorrect toggle rectangle in VisualEffect inspector
- Shader compilation with SimpleLit and debug display

## [5.2.0-preview] - 2018-11-27
### Added
- Prewarm mechanism

### Fixed
- Handle data loss of overriden parameters better

### Optimized
- Improved iteration times by not compiling initial shader variant

## [4.3.0-preview] - 2018-11-23

Initial release
