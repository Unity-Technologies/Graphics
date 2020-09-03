# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [7.5.1] - 2020-09-02
### Added
- Added Degrees / Radians conversion subgraphs in samples

### Fixed
- Prevent from creating a context in VisualEffectSugraphOperator by draggingfrom an output slot.
- Don't show the blocks window when context cant have blocks
- Avoid NullReferenceException when VisualEffectAsset is null if VFXPropertyBinder [Case 1219061](https://issuetracker.unity3d.com/product/unity/issues/guid/1219061/)
- Missing Reset function in VFXPropertyBinder [Case 1219063](https://issuetracker.unity3d.com/product/unity/issues/guid/1219063/)
- Fix issue with strips outputs that could cause too many vertices to be renderered
- SpawnIndex attribute returns correct value in update and outputs contexts
- Disable Reset option in context menu for all VFXObject [Case 1251519](https://issuetracker.unity3d.com/product/unity/issues/guid/1251519/) & [Case 1251533](https://issuetracker.unity3d.com/product/unity/issues/guid/1251533/)
- Avoid other NullReferenceException using property binders
- Fix culture issues when generating attributes defines in shaders [Case 1222819](https://issuetracker.unity3d.com/product/unity/issues/guid/1222819/)
- Move the VFXPropertyBinder from Update to LateUpdate [Case 1254340](https://issuetracker.unity3d.com/product/unity/issues/guid/1254340/)
- Properties in blackboard are now exposed by default
- Dissociated Colors for bool, uint and int
- De-nicified attribute name (conserve case) in Set Custom Attribute title
- Changed the default "No Asset" message when opening the visual effect graph window
- Subgraphs are not in hardcoded categories anymore : updated default subgraph templates + Samples to add meaningful categories.
- Enum fields having headers show the header in the inspector as well.
- Fix incorrect propagation on expression flags [Case 1246738](https://issuetracker.unity3d.com/product/unity/issues/guid/1246738/)
- fix crash when creating a loop in subgraph operators [Case 1251523](https://issuetracker.unity3d.com/product/unity/issues/guid/1251523/)
- Prefab highlight missing for initial event name toggle [Case 1263012](https://issuetracker.unity3d.com/product/unity/issues/guid/1263012/)
- Make VisualEffect created from the GameObject menu have unique names [Case 1262989](https://issuetracker.unity3d.com/product/unity/issues/guid/1262989/)
- Normals with non uniform scales are correctly computed [Case 1246989](https://issuetracker.unity3d.com/product/unity/issues/guid/1246989/)
- Fix exposed Texture2DArray and Cubemap types from shader graph not being taken into account in Output Mesh [Case 1265221](https://issuetracker.unity3d.com/product/unity/issues/guid/1265221/)
- Optimize display of inspector when there is a lot of exposed VFX properties.
- Correctly frame the whole graph, when opening the Visual Effect Editor
- fix issue with multiselection and objectfields [Case 1250378](https://issuetracker.unity3d.com/issues/vfx-removing-texture-asset-while-multiediting-working-incorrectly)
- Handle correctly disabled alphaTreshold material slot in shaderGraph.
- Allow world position usage in shaderGraph plugged into an alpha/opacity output [Case 1259511](https://issuetracker.unity3d.com/product/unity/issues/guid/1259511/)

## [7.4.1] - 2020-06-03

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [7.4.0] - 2020-05-22
### Added
- Allow filtering enums in VFXModels' VFXSettings

### Fixed
- Remove some shader warnings
- Fixed Sample Flipbbook Texture File Names
- height of initial event name.
- fix colorfield height.
- Prevent capacity from being 0 [Case 1233044](https://issuetracker.unity3d.com/product/unity/issues/guid/1233044/)
- Fix for dragged parameters order when there are categories
- Avoid NullReferenceException in Previous Position Binder" component. [Case 1242351](https://issuetracker.unity3d.com/product/unity/issues/guid/1242351/)

## [7.3.0] - 2020-03-11
### Fixed
- Runtime compilation now compiles correctly when constant folding several texture ports that reference the same texture [Case 1193602](https://issuetracker.unity3d.com/issues/output-shader-errors-when-compiling-the-runtime-shader-of-a-lit-output-with-exposed-but-unassigned-additional-maps)
- Add message in inspector for unreachable properties due to VisualEffectAsset stored in AssetBundle [Case 1193602](https://issuetracker.unity3d.com/product/unity/issues/guid/1203616/)
- pCache importer and exporter tool was keeping a lock on texture or pCache files [Case 1185677](https://issuetracker.unity3d.com/product/unity/issues/guid/1185677/)
- Age particles checkbox was incorrectly hidden [Case 1221557](https://issuetracker.unity3d.com/product/unity/issues/guid/1221557/)
- Handle correctly direction, position & vector types in AppendVector operator [Case 1111867](https://issuetracker.unity3d.com/product/unity/issues/guid/1111867/)
- Fix various bugs in Position (Cone) block [Case 1111053] (https://issuetracker.unity3d.com/product/unity/issues/guid/1111053/)
- renamed soft particle fade distance, made attributes adding text, add in tooltip not label
- Fix for nesting of VFXSubgraphContexts
- Convert inline to exposed property / Quick expose property sets correct default value in parent
- Missing Packing.hlsl include while using an unlit shadergraph
- Fix space issues with blocks and operators taking a camera as input

## [7.2.0] - 2020-02-10
### Added
- Right click on edge to create a interstitial node.
- Range and Min attributes support on int and uint parameters
- New Construct Matrix from Vector4 operator

### Fixed
- Moved VFX Event Tester Window visibility to Component Play Controls SceneView Window
- Universal Render Pipeline : Fog integration for Exponential mode [Case 1177594] (https://issuetracker.unity3d.com/issues/urp-slash-fog-vfx-particles)
- Fix typo in strip tangent computation
- Filter out Direction type on inconsistent operator [Case 1201681](https://issuetracker.unity3d.com/product/unity/issues/guid/1201681/)
- Exclude MouseEvent, RigidBodyCollision, TriggerEvent & Sphere binders when physics modules isn't available
- Visual Effect Activation Track : Handle empty string in ExposedProperty
- in some cases AABox position gizmo would not move when dragged.
- Inspector doesn't trigger any exception if VisualEffectAsset comes from an Asset Bundle [case 1203616](https://issuetracker.unity3d.com/issues/visual-effect-component-is-not-fully-shown-in-the-inspector-if-vfx-is-loaded-from-asset-bundle)
- OnStop Event to the start of a Spawn Context makes it also trigger when OnPlay is sent [Case 1198339] https://issuetracker.unity3d.com/product/unity/issues/guid/1198339/
- Remove unexpected public API : UnityEditor.VFX.VFXSeedMode & IncrementStripIndexOnStart

## [7.1.8] - 2020-01-20

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [7.1.7] - 2019-12-11
### Fixed
- subgraph block into subgraph context no longer forget parameter values.
- Fix exception when compiling an asset with a turbulence block in absolute mode
- Fixed GetCustomAttribute that was locked to Current
- Shader compilation now works when using view direction in shader graph
- Depth Position and Collision blocks now work correctly in local space systems

## [7.1.6] - 2019-11-22
### Added
- New quad distortion output for particle strips
- New attribute for strips: particleCountInStrip
- New options for quad strips texture mapping: swap UV and custom mapping

### Fixed
- Fix shader interpolants with shader graph and particle strips
- SpawnIndex attribute is now working correctly in Initialize context
- Remove useless VFXLibrary clears that caused pop-up menu to take long opening times
- Make sure the subgraph is added to the graph when we set the setting. Fix exception on Convert To Subgraph.
- Subgraph operators appear on drag edge on graph.
- Allow alpha clipping of motion vector for transparent outputs [Case 1192930] (https://issuetracker.unity3d.com/product/unity/issues/guid/1192930/)
- Removed Unnecessary reference to HDRP Runtime Assembly in VFX Runtime Assembly

## [7.1.5] - 2019-11-15

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [7.1.4] - 2019-11-13

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [7.1.3] - 2019-09-19
### Added
- Tooltips for Attributes
- Custom Inspector for Spawn context, delay settings are more user friendly.
- Quick Expose Property : Holding Alt + Release Click in an Empty space while making property edges creates a new exposed property of corresponding type with current slot value.
- Octagon & Triangle support for planar distortion output
- Custom Z axis option for strip output
- Custom Inspector for Update context, display update position/rotation instead of integration
- Tooltips to blocks, nodes, contexts, and various menus and options
- New operators: Exp, Log and LoadTexture

### Fixed
- Infinite recompilation using subgraph [Case 1186191] (https://issuetracker.unity3d.com/product/unity/issues/guid/1186191/)
- Modifying a shader used by an output mesh context now automatically updates the currently edited VFX
- Possible loss of shadergraph reference in unlit output
- ui : toolbar item wrap instead of overlapping.
- Selection Pass for Universal and High Definition Render Pipeline
- Copy/Paste not deserializing correctly for Particle Strip data
- WorldPosition, AbsoluteWorldPosition & ScreenPos in shadergraph integration
- Optimize VFXAssetEditor when externalize is activated
- TransformVector|Position|Direction & DistanceToSphere|Plane|Line have now spaceable outputs
- Filter out motion vector output for lower resolution & after post-process render passes [Case 1192932] (https://issuetracker.unity3d.com/product/unity/issues/guid/1192932/)
- Sort compute on metal failing with BitonicSort128 [Case 1126095] (https://issuetracker.unity3d.com/issues/osx-unexpected-spawn-slash-capacity-results-when-sorting-is-set-to-auto-slash-on)
- Fix alpha clipping with shader graph
- Fix output settings correctly filtered dependeing on shader graph use or not
- Fix some cases were normal/tangent were not passes as interpolants with shader graph
- Make normals/tangents work in unlit output with shader graph
- Sample Scene Color & Scene Depth from Shader Graph Integration using High Definition and Universal Render Pipeline

## [7.1.2] - 2019-09-19
### Fixed
- Universal Render Pipeline : Fog integration for Exponential mode [Case 1177594](https://issuetracker.unity3d.com/issues/urp-slash-fog-vfx-particles)
- Correct VFXSettings display in Shader Graph compatible outputs
- No more NullReference on sub-outputs after domain reload
- Fix typo in strip tangent computation

## [7.1.1] - 2019-09-05
### Added
- Moved High Definition templates and includes to com.unity.render-pipelines.high-definition package
- Navigation commands for subgraph.
- Allow choosing the place to save vfx subgraph.
- Particle strips for trails and ribbons. (Experimental)
- Shadergraph integration into vfx. (Experimental)

### Fixed
- Using struct as subgraph parameters.
- Objectproperty not consuming delete key.
- Converting a subgraph operator inside a subgraph operator with outputs.
- Selecting a GameObject with a VFX Property Binder spams exception.
- Wrong motion vector while modifying local matrix of a VisualEffect.
- Convert output settings copy.
- Fixed some outputs failing to compile when used with certain UV Modes [Case 1126200] (https://issuetracker.unity3d.com/issues/output-some-outputs-fail-to-compile-when-used-with-certain-uv-modes)
- Removed Gradient Mapping Mode from some outputs type where it was irrelevant [Case 1164045]
- Soft Particles work with Distortion outputs [Case 1167426] (https://issuetracker.unity3d.com/issues/output-soft-particles-do-not-work-with-distortion-outputs)
- category rename rect.
- copy settings while converting an output
- toolbar toggle appearing light with light skin.
- multiselection of gradient in visual effect graph
- clipped "reseed" in visual effect editor
- Unlit outputs are no longer pre-exposed by default in HDRP
- Augmented generated HLSL floatN precision [Case 1177730] (https://issuetracker.unity3d.com/issues/vfx-graph-7x7-flipbook-particles-flash-and-dont-animate-correctly-in-play-mode-or-in-edit-mode-with-vfx-graph-closed)
- Spherical coordinates to Rectangular (Cartesians) coordinates node input: angles are now expressed in radians
- Turbulence noise updated: noise type and frequency can be specified [Case  1141282] (https://issuetracker.unity3d.com/issues/vfx-particles-flicker-when-blend-mode-is-set-to-alpha-turbulence-block-is-enabled-and-there-is-more-than-50000-particles)
- Color and Depth camera buffer access in HDRP now use Texture2DArray instead of Texture2D
- Output Mesh with shader graph now works as expected

## [7.0.1] - 2019-07-25
### Added
- Add Position depth operator along with TransformVector4 and LoadTexture2D expressions.

### Fixed
- Inherit attribute block appears three times [Case 1166905](https://issuetracker.unity3d.com/issues/attributes-each-inherit-attribute-block-appears-3-times-in-the-search-and-some-have-a-seed-attribute)
- Unexpected exception : `Trying to modify space on a not spaceable slot` error when adding collision or conform blocks [Case 1163442](https://issuetracker.unity3d.com/issues/block-trying-to-modify-space-on-a-not-spaceable-slot-error-when-adding-collision-or-conform-blocks)

## [7.0.0] - 2019-07-17
### Added
- Make multiselection work in a way that do not assume that the same parameter will have the same index in the property sheet.
- auto recompile when changing shaderpath
- auto recompile new vfx
- better detection of default shader path
- Bitfield control.
- Initial Event Name inspector for visual effect asset and component
- Subgraphs
- Move HDRP outputs to HDRP package + expose HDRP queue selection
- Add exposure weight control for HDRP outputs
- Shader macros for XR single-pass instancing
- XR single-pass instancing support for indirect draws
- Inverse trigonometric operators (atan, atan2, asin, acos)
- Replaced Orient : Fixed rotation with new option Orient : Advanced
- Loop & Delay integrated to the spawn system
- Motion Vector support for PlanarPrimitive & Mesh outputs

### Fixed
- Handle a possible exception (ReflectionTypeLoadException) while using VFXParameterBinderEditor
- Renamed Parameter Binders to Property Binders. (This will cause breaking serialization for these PropertyBinders : VFXAudioSpectrumBinder, VFXInputMouseBinder, VFXInputMouseBinder, VFXInputTouchBinder, VFXInputTouchBinder, VFXRaycastBinder, VFXTerrainBinder, VFXUIDropdownBinder, VFXUISliderBinder, VFXUIToggleBinder)
- Renamed Namespace `UnityEngine.Experimental.VFX.Utility` to `UnityEngine.VFX.Utility`
- Fix normal bending factor computation for primitive outputs
- Automatic template path detection based on SRP in now working correctly

## [6.7.0-preview] - 2019-05-16
### Added
- Distortion Outputs (Quad / Mesh)
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
