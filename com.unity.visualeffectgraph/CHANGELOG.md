# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [5.16.1-preview] - 2019-05-22
### Changed
- This package now requires Unity 2019.1.3f1 or later to run.

## [5.16.0-preview] - 2019-05-20

### Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [5.15.0-preview] - 2019-05-13
### Changed
The version number for this package has increased due to a version update of a related graphics package.

## [5.14.0-preview] - 2019-05-09
### Fixed
- Improve AA line rendering
- Fix screen space size block
- Crash chaining two spawners each other [Case 1135299](https://issuetracker.unity3d.com/issues/crash-chaining-two-spawners-to-each-other-produces-an-infinite-loop)
- Inspector : Exposed parameters disregard the initial value [Case 1126471](https://issuetracker.unity3d.com/issues/parameters-exposed-parameters-disregard-the-initial-value)
- Fix for linking spawner to spawner while first spawner is linked to initialize + test 
- Deactivate pre-exposure multiplier in unlit outputs

## [5.13.0-preview] - 2019-04-15

## [5.12.0-preview] - 2019-04-11
### Fixed
- Fix shader compilation error with debug views

## [5.11.0-preview] - 2019-04-01

## [5.10.0-preview] - 2019-03-19

## [5.9.0-preview] - 2019-03-15
### Fixed
- Issue that remove the edge when dragging an edge from slot to the same slot.
- Exception when undoing an edge deletion on a dynamic operator. 
- Exception regarding undo/redo when dragging a edge linked to a dynamic operator on another slot.
- Missing graph invalidation in VFXGraph.OnEnable, was causing trouble with value invalidation until next recompilation

## [5.8.0-preview] - 2019-03-13
### Added
- Addressing mode for Sequential blocks
- Invert transform available on GPU
- Add automatic depth buffer reference for main camera (for position and collision blocks)
- Total Time for PreWarm in Visual Effect Asset inspector
- Support for unlit output with LWRP

### Fixed
- Better Handling of Null or Missing Parameter Binders (Editor + Runtime)
- Undo Redo while changing space
- Type declaration was unmodifiable due to exception during space intialization
- Fix unexpected issue when plugging per particle data into hash of per component fixed random
- Missing asset reimport when exception has been thrown during graph compilation
- Fix exception when using a Oriented Box Volume node [Case 1110419](https://issuetracker.unity3d.com/issues/operator-indexoutofrangeexception-when-using-a-volume-oriented-box-node)
- Add missing blend value slot in Inherit Source Attribute blocks [Case 1120568](https://issuetracker.unity3d.com/issues/source-attribute-blend-source-attribute-blocks-are-not-useful-without-the-blend-value)
- Visual Effect Inspector Cosmetic Improvements
- Exception while removing a sub-slot of a dynamic operator

## [5.7.0-preview] - 2019-03-07

## [5.6.0-preview] - 2019-02-21

## [5.5.0-preview] - 2019-02-18
### Changed
- Code refactor: all macros with ARGS have been swapped with macros with PARAM. This is because the ARGS macros were incorrectly named

## [5.4.0-preview] - 2019-02-11
### Fixed
- Incorrect toggle rectangle in VisualEffect inspector
- Shader compilation with SimpleLit and debug display

## [5.3.1-preview] - 2019-01-28

## [5.3.0-preview] - 2019-01-28
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

## [5.2.0-preview] - 2018-11-27
### Added
- Prewarm mechanism

### Fixed
- Handle data loss of overriden parameters better

### Optimized
- Improved iteration times by not compiling initial shader variant

## [4.3.0-preview] - 2018-11-23

Initial release
