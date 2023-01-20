# Changelog
All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).


## [12.1.10] - 2023-01-18

This version is compatible with Unity 2021.3.18f1.

Version Updated
The version number for this package has increased due to a version update of a related graphics package.


## [12.1.9] - 2022-12-12

This version is compatible with Unity 2021.3.16f1.

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [12.1.8] - 2022-11-04

This version is compatible with Unity 2021.3.14f1.

### Changed
- Reduced time taken by code generation when a shader graph asset is imported

### Fixed
- Fixed a compilation bug in BiRP Target in some variants with lightmaps.

## [12.1.7] - 2022-03-29

This version is compatible with Unity 2021.2.19f1.

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [12.1.6] - 2022-02-09

This version is compatible with Unity 2021.2.14f1.

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [12.1.5] - 2022-01-14

This version is compatible with Unity 2021.2.12f1.

### Fixed
  - Fixed an issue where edges connected to SubGraphNodes would sometimes get lost on upgrading a pre-targets graphs [1379996](https://issuetracker.unity3d.com/product/unity/issues/guid/1379996/)

## [12.1.4] - 2021-12-07

### Fixed
  - Fixed bug with Shader Graph subwindows having their header text overflow when the window is resized smaller than the title [1378203]
  - Fixed issue where Duplicating/Copy-Pasting last keyword in the blackboard throws an exception [1394378]
  - Fixed an issue where some graphs with incorrectly formatted data would not display their shader inputs in the blackboard [1384315]
  - Fixed the behavior of checkerboard node with raytracing
  - Fixed a ShaderGraph warning when connecting a node using Object Space BiTangent to the vertex stage [1361512] (https://issuetracker.unity3d.com/issues/shader-graph-cross-implicit-truncation-of-vector-type-errors-are-thrown-when-connecting-transform-node-to-vertex-block)
  - Fixed a validation error in ShaderGraph when using the SimpleNoise node both inside and outside a subgraph [1383046] (https://issuetracker.unity3d.com/issues/validation-error-is-usually-thrown-when-simple-noise-node-is-both-in-a-shadergraph-and-in-a-sub-graph)

## [12.1.3] - 2021-11-17

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [12.1.2] - 2021-10-22

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [12.1.1] - 2021-10-04

### Fixed
  - Fixed a bug that caused the Scene Depth Node in Eye space to not work correctly when using an orthographic camera [1311272].
  - Fixed missing shader keyword stage during keyword copying.

## [12.1.0] - 2021-09-23

### Added
  - Adding control of anisotropic settings on inline Sampler state nodes in ShaderGraph.

### Fixed
  - Fixed bug where an exception was thrown on undo operation after adding properties to a category [1348910] (https://fogbugz.unity3d.com/f/cases/1348910/)
  - Fixed unhandled exception when loading a subgraph with duplicate slots [1369039].
  - Fixed how graph errors were displayed when variant limits were reached [1355815]

## [12.0.0] - 2021-01-11

### Added
  - Added categories to the blackboard, enabling more control over the organization of shader properties and keywords in the Shader Graph tool. These categories are also reflected in the Material Inspector for URP + HDRP, for materials created from shader graphs.
  - Added ability to define custom vertex-to-fragment interpolators.
  - Support for the XboxSeries platform has been added.
  - Stereo Eye Index, Instance ID, and Vertex ID nodes added to the shadergraph library.
  - Added information about selecting and unselecting items to the Blackboard article.
  - Added View Vector Node documentation
  - Added custom interpolator thresholds on shadergraph project settings page.
  - Added custom interpolator documentation
  - Added subshadergraphs for SpeedTree 8 shadergraph support: SpeedTree8Wind, SpeedTree8ColorAlpha, SpeedTree8Billboard.
  - Added an HLSL file implementing a version of the Unity core LODDitheringTransition function which can be used in a Shader Graph
  - Added a new target for the built-in render pipeline, including Lit and Unlit sub-targets.
  - Added stage control to ShaderGraph Keywords, to allow fragment or vertex-only keywords.
  - For Texture2D properties, added linearGrey and red as options for default texture mode.
  - For Texture2D properties, changed the "bump" option to be called "Normal Map", and will now tag these properties with the [NormalMap] tag.
  - Added `Branch On Input Connection` node. This node can be used inside a subgraph to branch on the connection state of an exposed property.
  - Added `Use Custom Binding` option to properties. When this option is enabled, a property can be connected to a `Branch On Input Connection` node. The user provides a custom label that will be displayed on the exposed property, when it is disconnected in a graph.
  - Added new dropdown property type for subgraphs, to allow compile time branching that can be controlled from the parent graph, via the subgraph instance node.
  - Added `Dropdown` node per dropdown property, that can be used to configure the desired branch control.
  - Added selection highlight and picking shader passes for URP target.
  - Added the ability to mark textures / colors as \[MainTexture\] and \[MainColor\].
  - Added the ability to enable tiling and offset controls for a Texture2D input.
  - Added the Split Texture Transform node to allow using/overriding the provided tiling and offset from a texture input.
  - Added `Calculate Level Of Detail Texture 2D` node, for calculating a Texture2D LOD level.
  - Added `Gather Texture 2D` node, for retrieving the four samples (red component only) that would be used for bilinear interpolation when sampling a Texture2D.
  - Added toggle "Disable Global Mip Bias" in Sample Texture 2D and Sample Texture 2D array node. This checkbox disables the runtimes automatic Mip Bias, which for instance can be activated during dynamic resolution scaling.
  - Added `Sprite` option to Main Preview, which is similar to `Quad` but does not allow rotation. `Sprite` is used as the default preview for URP Sprite shaders.
  - Added Tessellation Option to PositionNode settings, to provide access to the pre-displaced tessellated position.
  - Added visible errors for invalid stage capability connections to shader graph.
  - Added a ShaderGraph animated preview framerate throttle.
  - Added many node synonyms for the Create Node search so that it's easier to find nodes.

### Changed
 - Updated searcher package dependency version to 4.9.1
- Properties and Keywords are no longer separated by type on the blackboard. Categories allow for any combination of properties and keywords to be grouped together as the user defines.
- Vector2/Vector3/Vector4 property types will now be properly represented by a matching Vector2/Vector3/Vector4 UI control in the URP + HDRP Material Inspector as opposed to the fallback Vector4 field that was used for any multi-dimensional vector type in the past.
- Updated/corrected View Direction documentation
- Change Asset/Create/Shader/Blank Shader Graph to Asset/Create/Shader Graph/Blank Shader Graph
- Change Asset/Create/Shader/Sub Graph to Asset/Create/Shader Graph/Sub Graph
- Change Asset/Create/Shader/VFX Shader Graph to Asset/Create/Shader Graph/VFX Shader Graph
- Adjusted Blackboard article to clarify multi-select functionality
- Limited max number of inspectable items in the Inspector View to 20 items
- Added borders to inspector items styling, to better differentiate between separate items
- Updated Custom Function Node to use new ShaderInclude asset type instead of TextAsset (.hlsl and .cginc softcheck remains).
- Change BranchOnInputNode to choose NotConnected branch when generating Preview
- Only ShaderGraph keywords count towards the shader permutation variant limit, SubGraph keywords do not.
- ShaderGraph SubGraphs will now report errors and warnings in a condensed single error.
- Changed "Create Node" action in ShaderGraph stack separator context menu to "Add Block Node" and added it to main stack context menu
- GatherTexture2D and TexelSize nodes now support all shader stages.

### Fixed
 - Fixed a usability issue where in some cases searcher would suggest one collapsed category of results that user would have to manually expand anyway
 - Fixed bug that causes search results to not be visible sometimes in the searcher window [1366061]
 - Fixed bug that causes exceptions to be thrown when using the up/down arrow keys with search list focused [1358016]
 - Fixed bug that causes some searcher items to be irreversibly collapsed due to expand icon disappearing on collapsing those items [1366074]
 - Fixed bug that caused incorrect search results with non whitespaced queries for nodes with spaces in their name and for subgraphs [1359158]
- Fixed bug where it was not possible to switch to Graph Settings tab in Inspector if multiple nodes and an edge was selected [1357648] (https://fogbugz.unity3d.com/f/cases/1357648/)
- Fixed an issue where fog node density was incorrectly calculated.
- Fixed inspector property header styling
- Added padding to the blackboard window to prevent overlapping of resize region and scrollbars interfering with user interaction
- Blackboard now properly handles selection persistence of items between undo and redos
- Fixed the Custom Editor GUI field in the Graph settings that was ignored.
- Node included HLSL files are now tracked more robustly, so they work after file moves and renames [1301915] (https://issuetracker.unity3d.com/product/unity/issues/guid/1301915/)
- Prevent users from setting enum keywords with duplicate reference names and invalid characters [1287335]
- Fixed a bug where old preview property values would be used for node previews after an undo operation.
- Clean up console error reporting from node shader compilation so errors are reported in the graph rather than the Editor console [1296291] (https://issuetracker.unity3d.com/product/unity/issues/guid/1296291/)
- Fixed treatment of node precision in subgraphs, now allows subgraphs to switch precisions based on the subgraph node [1304050] (https://issuetracker.unity3d.com/issues/precision-errors-when-theres-a-precision-discrepancy-between-subgraphs-and-parent-graphs)
- Fixed an issue where the Rectangle Node could lose detail at a distance.  New control offers additional method that preserves detail better [1156801]
- Fixed virtual texture layer reference names allowing invalid characters [1304146]
- Fixed issue with SRP Batcher compatibility [1310624]
- Fixed issue with Hybrid renderer compatibility [1296776]
- Fixed ParallaxOcclusionMapping node to clamp very large step counts that could crash GPUs (max set to 256). [1329025] (https://issuetracker.unity3d.com/issues/shadergraph-typing-infinity-into-the-steps-input-for-the-parallax-occlusion-mapping-node-crashes-unity)
- Fixed an issue where the shader variant limit exceeded message was not getting passed [1304168] (https://issuetracker.unity3d.com/product/unity/issues/guid/1304168)
- Fixed a bug in master node preview generation that failed compilation when a block was deleted [1319066] (https://issuetracker.unity3d.com/issues/shadergraph-deleting-stack-blocks-of-universal-rp-targeted-shadergraph-causes-the-main-preview-to-fail-to-compile)
- Fixed issue where vertex generation was incorrect when only custom blocks were present [1320695].
- Fixed a bug where property deduplication was failing and spamming errors [1317809] (https://issuetracker.unity3d.com/issues/console-error-when-adding-a-sample-texture-operator-when-a-sampler-state-property-is-present-in-blackboard)
- Fixed a bug where big input values to the SimpleNoise node caused precision issues, especially noticeable on Mali GPUs. [1322891] (https://issuetracker.unity3d.com/issues/urp-mali-missing-glitch-effect-on-mali-gpu-devices)
- Fixed a bug where synchronously compiling an unencountered shader variant for preview was causing long delays in graph updates [1323744]
- Fixed a regression where custom function node file-included functions could not access shadergraph properties [1322467]
- Fixed an issue where a requirement was placed on a fixed-function emission property [1319637]
- Fixed default shadergraph precision so it matches what is displayed in the graph settings UI (single) [1325934]
- Fixed an unhelpful error message when custom function nodes didn't have a valid file [1323493].
- Fixed an issue with how the transform node handled direction transforms from absolute world space in camera relative SRPs [1323726]
- Fixed a bug where changing a Target setting would switch the inspector view to the Node Settings tab if any nodes were selected.
- Fixed "Disconnect All" option being grayed out on stack blocks [1313201].
- Fixed how shadergraph's prompt for "unsaved changes" was handled to fix double messages and incorrect window sizes [1319623].
- Fixed an issue where users can't create multiple Boolean or Enum keywords on the blackboard. [1329021](https://issuetracker.unity3d.com/issues/shadergraph-cant-create-multiple-boolean-or-enum-keywords)
- Fixed an issue where generated property reference names could conflict with Shader Graph reserved keywords [1328762] (https://issuetracker.unity3d.com/product/unity/issues/guid/1328762/)
- Fixed a ShaderGraph issue where ObjectField focus and Node selections would both capture deletion commands [1313943].
- Fixed a ShaderGraph issue where the right click menu doesn't work when a stack block node is selected [1320212].
- Fixed a bug when a node was both vertex and fragment exclusive but could still be used causing a shader compiler error [1316128].
- Fixed a ShaderGraph issue where a warning about an uninitialized value was being displayed on newly created graphs [1331377].
- Fixed divide by zero warnings when using the Sample Gradient Node
- Fixed the default dimension (1) for vector material slots so that it is consistent with other nodes. (https://issuetracker.unity3d.com/product/unity/issues/guid/1328756/)
- Fixed reordering when renaming enum keywords. (https://issuetracker.unity3d.com/product/unity/issues/guid/1328761/)
- Fixed an issue where an integer property would be exposed in the material inspector as a float [1330302](https://issuetracker.unity3d.com/product/unity/issues/guid/1330302/)
- Fixed a bug in ShaderGraph where sticky notes couldn't be copied and pasted [1221042].
- Fixed an issue where upgrading from an older version of ShaderGraph would cause Enum keywords to be not exposed [1332510]
- Fixed an issue where a missing subgraph with a "Use Custom Binding" property would cause the parent graph to fail to load [1334621] (https://issuetracker.unity3d.com/issues/shadergraph-shadergraph-cannot-be-opened-if-containing-subgraph-with-custom-binding-that-has-been-deleted)
- Fixed a ShaderGraph issue where unused blocks get removed on edge replacement [1334341].
- Fixed an issue where the ShaderGraph transform node would generate incorrect results when transforming a direction from view space to object space [1333781] (https://issuetracker.unity3d.com/product/unity/issues/guid/1333781/)
- Fixed a ShaderGraph issue where keyword properties could get stuck highlighted when deleted [1333738].
- Fixed issue with ShaderGraph custom interpolator node dependency ordering [1332553].
- Fixed SubGraph SamplerState property defaults not being respected [1336119]
- Fixed an issue where nested subgraphs with identical SamplerState property settings could cause compile failures [1336089]
- Fixed an issue where SamplerState properties could not be renamed after creation [1336126]
- Fixed loading all materials from project when saving a ShaderGraph.
- Fixed issues with double prompts for "do you want to save" when closing Shader Graph windows [1316104].
- Fixed a ShaderGraph issue where resize handles on blackboard and graph inspector were too small [1329247] (https://issuetracker.unity3d.com/issues/shadergraph-resize-bounds-for-blackboard-and-graph-inspector-are-too-small)
- Fixed a ShaderGraph issue where a material inspector could contain an extra set of render queue, GPU instancing, and double-sided GI controls.
- Fixed a Shader Graph issue where property auto generated reference names were not consistent across all property types [1336937].
- Fixed a warning in ShaderGraph about BuiltIn Shader Library assembly having no scripts.
- Fixed ShaderGraph BuiltIn target not having collapsible foldouts in the material inspector [1339256].
- Fixed GPU instancing support in Shadergraph [1319655] (https://issuetracker.unity3d.com/issues/shader-graph-errors-are-thrown-when-a-propertys-shader-declaration-is-set-to-hybrid-per-instance-and-exposed-is-disabled).
- Fixed indent level in shader graph target foldout (case 1339025).
- Fixed ShaderGraph BuiltIn target shader GUI to allow the same render queue control available on URP with the changes for case 1335795.
- Fixed ShaderGraph BuiltIn target not to apply emission in the ForwardAdd pass to match surface shader results [1345574]. (https://issuetracker.unity3d.com/product/unity/issues/guid/1345574/)
- Fixed Procedural Virtual Texture compatibility with SRP Batcher [1329336] (https://issuetracker.unity3d.com/issues/procedural-virtual-texture-node-will-make-a-shadergraph-incompatible-with-srp-batcher)
- Fixed an issue where SubGraph keywords would not deduplicate before counting towards the permutation limit [1343528] (https://issuetracker.unity3d.com/issues/shader-graph-graph-is-generating-too-many-variants-error-is-thrown-when-using-subgraphs-with-keywords)
- Fixed an issue where an informational message could cause some UI controls on the graph inspector to be pushed outside the window [1343124] (https://issuetracker.unity3d.com/product/unity/issues/guid/1343124/)
- Fixed a ShaderGraph issue where selecting a keyword property in the blackboard would invalidate all previews, causing them to recompile [1347666] (https://issuetracker.unity3d.com/product/unity/issues/guid/1347666/)
- Fixed the incorrect value written to the VT feedback buffer when VT is not used.
- Fixed ShaderGraph isNaN node, which was always returning false on Vulkan and Metal platforms.
- Fixed ShaderGraph sub-graph stage limitations to be per slot instead of per sub-graph node [1337137].
- Disconnected nodes with errors in ShaderGraph no longer cause the imports to fail [1349311] (https://issuetracker.unity3d.com/issues/shadergraph-erroring-unconnected-node-causes-material-to-become-invalid-slash-pink)
- ShaderGraph SubGraphs now report node warnings in the same way ShaderGraphs do [1350282].
- Fixed ShaderGraph exception when trying to set a texture to "main texture" [1350573].
- Fixed a ShaderGraph issue where Float properties in Integer mode would not be cast properly in graph previews [1330302](https://fogbugz.unity3d.com/f/cases/1330302/)
- Fixed a ShaderGraph issue where hovering over a context block but not its node stack would not bring up the incorrect add menu [1351733](https://fogbugz.unity3d.com/f/cases/1351733/)
- Fixed the BuiltIn Target to perform shader variant stripping [1345580] (https://issuetracker.unity3d.com/product/unity/issues/guid/1345580/)
- Fixed incorrect warning while using VFXTarget
- Fixed a bug with Sprite Targets in ShaderGraph not rendering correctly in game view [1352225]
- Fixed compilation problems on preview shader when using hybrid renderer v2 and property desc override Hybrid Per Instance
- Fixed a serialization bug wrt PVT property flags when using subgraphs. This fixes SRP batcher compatibility.
- Fixed an incorrect direction transform from view to world space [1365186]
- Fixed the appearance (wrong text color, and not wrapped) of a warning in Node Settings [1365780]
- Fixed the ordering of inputs on a SubGraph node to match the properties on the blackboard of the subgraph itself [1366052]
- Fixed Parallax Occlusion Mapping node to handle non-uniformly scaled UVs such as HDRP/Lit POM [1347008].
- Fixed ShaderGraph HDRP master preview disappearing for a few seconds when graph is modified  [1330289] (https://issuetracker.unity3d.com/issues/shadergraph-hdrp-main-preview-is-invisible-until-moved)
- Fixed an issue where ShaderGraph "view shader" commands were opening in individual windows, and blocking Unity from closing [1367188]
- Fixed the node searcher results to prefer names over synonyms [1367706]

## [11.0.0] - 2020-10-21

### Added

### Changed

### Fixed
- Fixed an issue where nodes with ports on one side would appear incorrectly on creation [1262050]
- Fixed a broken link in the TOC to Main Preview
- Fixed an issue with the Gradient color picker displaying different values than the selected color.
- Fixed an issue where blackboard properties when dragged wouldn't scroll the list of properties to show the user more of the property list [1293632]
- Fixed an issue where, when blackboard properties were dragged and then the user hit the "Escape" key, the drag indicator would still be visible
- Fixed an issue where renaming blackboard properties through the Blackboard wouldn't actually change the underlying property name
- Fixed an issue where blackboard wasn't resizable from all directions like the Inspector and Main Preview
- Fixed an issue where deleting a property node while your mouse is over it leaves the property highlighted in the blackboard [1238635]
- Fixed an issue where Float/Vector1 properties did not have the ability to be edited using a slider in the Inspector like the other Vector types
- Fixed an issue with inactive node deletion throwing a superfluous exception.
- Fixed an issue where interpolators with preprocessors were being packed incorrectly.
- Fixed rounded rectangle shape not rendering correctly on some platforms.
- Fixed an issue where generated `BuildVertexDescriptionInputs()` produced an HLSL warning, "implicit truncation of vector type" [1299179](https://issuetracker.unity3d.com/product/unity/issues/guid/1299179/)
- Fixed an issue on upgrading graphs with inactive Master Nodes causing null ref errors. [1298867](https://issuetracker.unity3d.com/product/unity/issues/guid/1298867/)
- Fixed an issue with duplicating a node with the blackboard closed [1294430](https://issuetracker.unity3d.com/product/unity/issues/guid/1294430/)
- Fixed an issue where ShaderGraph stopped responding after selecting a node after opening the graph with the inspector window hidden [1304501](https://issuetracker.unity3d.com/issues/shadergraph-graph-is-unusable-if-opened-with-graph-inspector-disabled-throws-errors)
- Fixed the InputNodes tests that were never correct. These were incorrect tests, no nodes needed tochange.
- Fixed the ViewDirection Node in Tangent space's calculation to match how the transform node works [1296788]
- Fixed an issue where SampleRawCubemapNode were requiring the Normal in Object space instead of World space [1307962]
- Boolean keywords now have no longer require their reference name to end in _ON to show up in the Material inspector [1306820] (https://issuetracker.unity3d.com/product/unity/issues/guid/1306820/)
- Newly created properties and keywords will no longer use obfuscated GUID-based reference names in the shader code [1300484]
- Fixed ParallaxMapping node compile issue on GLES2
- Fixed a selection bug with block nodes after changing tabs [1312222]
- Fixed some shader graph compiler errors not being logged [1304162].
- Fixed a shader graph bug where the Hue node would have a large seam with negative values [1340849].
- Fixed an error when using camera direction with sample reflected cube map [1340538].
- Fixed ShaderGraph's FogNode returning an incorrect density when the fog setting was disabled [1347235].

## [10.3.0] - 2020-11-03

### Added
- Users can now manually control the preview mode of nodes in the graph, and subgraphs

### Changed
- Adjusted and expanded Swizzle Node article as reviewed by docs editorial.(DOC-2695)
- Adjusted docs for SampleTexture2D, SampleTexture2DLOD, SampleTexture2DArray, SampleTexture3D, SampleCubemap, SampleReflectedCubemap, TexelSize, NormalFromTexture, ParallaxMapping, ParallaxOcclusionMapping, Triplanar, Sub Graphs, and Custom Function Nodes to reflect changes to texture wire data structures. (DOC-2568)
- Texture and SamplerState types are now HLSL structures (defined in com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl).  CustomFunctionNode use of the old plain types is supported, but the user should upgrade to structures to avoid bugs.
- The shader graph inspector window will now switch to the "Node Settings" tab whenever a property/node/other selectable item in the graph is clicked on to save the user a click

### Fixed
- Fixed an issue where shaders could be generated with CR/LF ("\r\n") instead of just LF ("\n") line endings [1286430]
- Fixed Custom Function Node to display the name of the custom function. [1293575]
- Addressed C# warning 0649 generated by unassigned structure members
- Fixed using TexelSize or reading sampler states from Textures output from a Subgraph or Custom Function Node [1284036]
- Shaders using SamplerState types now compile with GLES2 (SamplerStates are ignored, falls back to Texture-associated sampler state) [1292031]
- Fixed an issue where the horizontal scrollbar at the bottom of the shader graph inspector window could not be used due to the resizing widget always taking priority over it
- Fixed an issue where the shader graph inspector window could be resized past the edges of the shader graph view
- Fixed an issue where resizing the shader graph inspector window sometimes had unexpected results
- Fixed Graph Inspector scaling that was allocating too much space to the labels [1268134]
- Fixed some issues with our Convert To Subgraph contextual menu to allow passthrough and fix inputs/outputs getting lost.
- Fixed issue where a NullReferenceException would be thrown on resetting reference name for a Shader Graph property
- Fixed an upgrade issue where old ShaderGraph files with a weird/bugged state would break on update to master stack [1255011]
- Fixed a bug where non-word characters in an enum keyword reference name would break the graph. [1270168](https://issuetracker.unity3d.com/product/unity/issues/guid/1270168)
- Fixed issue where a NullReferenceException would be thrown on resetting reference name for a Shader Graph property

## [10.2.0] - 2020-10-19

### Added

### Changed
- Renamed the existing Sample Cubemap Node to Sample Reflected Cubemap Node, and created a new Sample Cubemap Node that samples cubemaps with a direction.
- Removed unnecessary HDRP constant declarations used by Material inspector from the UnityPerMaterial cbuffer [1285701]
- Virtual Texture properties are now forced to be Exposed, as they do not work otherwise [1256374]

### Fixed
- Fixed an issue where old ShaderGraphs would import non-deterministically, changing their embedded property names each import [1283800]
- Using the TexelSize node on a ShaderGraph texture property is now SRP batchable [1284029]
- Fixed an issue where Mesh Deformation nodes did not have a category color. [1227081](https://issuetracker.unity3d.com/issues/shadergraph-color-mode-vertex-skinning-catagory-has-no-color-associated-with-it)
- Fixed SampleTexture2DLOD node to return opaque black on unsupported platforms [1241602]
- ShaderGraph now detects when a SubGraph is deleted while being used by a SubGraph node, and displays appropriate errors [1206438]
- Fixed an issue where the Main Preview window rendered too large on small monitors during first open. [1254392]
- Fixed an issue where Block nodes using Color slots would not be automatically removed from the Master Stack. [1259794]
- Fixed an issue where the Create Node menu would not close when pressing the Escape key. [1263667]
- Fixed an issue with the Preview Manager not updating correctly when deleting an edge that was created with a node (dragging off an existing node slot)
- Fixed an issue where ShaderGraph could not read matrices from a Material or MaterialPropertyBlock while rendering with SRP batcher [1256374]
- Fixed an issue where user setting a property to not Exposed, Hybrid-Instanced would result in a non-Hybrid Global property [1285700]
- Fixed an issue with Gradient when it is used as expose parameters. Generated code was failing [1285640 ]
- Fixed the subgraph slot sorting function [1286805]
- Fixed Parallax Occlusion Mapping not working in sub graphs. [1221317](https://issuetracker.unity3d.com/product/unity/issues/guid/1221317/)
- All textures in a ShaderGraph, even those not used, will now be pulled into an Exported Package [1283902]
- Fixed an issue where the presence of an HDRP DiffusionProfile property or node would cause the graph to fail to load when HDRP package was not present [1287904]
- Fixed an issue where unknown type Nodes (i.e. HDRP-only nodes used without HDRP package) could be copied, resulting in an unloadable graph [1288475]
- Fixed an issue where dropping HDRP-only properties from the blackboard field into the graph would soft-lock the graph [1288887]
- Fixed an issue using the sample gradient macros in custom function nodes, which was using a scalar value instead of a vector value for the gradients [1299830]

## [10.1.0] - 2020-10-12

### Added
- Added parallax mapping node and parallax occlusion mapping node.
- Added the possibility to have multiple POM node in a single graph.
- Added better error feedback when SampleVirtualTexture nodes run into issues with the VirtualTexture property inputs
- Added ability for Shader Graph to change node behavior without impacting existing graphs via the “Allow Deprecated Nodes”

### Changed
- Added method chaining support to shadergraph collection API.
- Optimized ShaderSubGraph import dependencies to minimize unnecessary reimports when using CustomFunctionNode
- Changed UI names from `Vector1` to `Float`
- Renamed `Float` precision to `Single`
- Cleaned up the UI to add/remove Targets
- The * in the ShaderGraph title bar now indicates that the graph has been modified when compared to the state it was loaded, instead of compared to what is on disk
- Cancelling a "Save changes on Close?" will now cancel the Close as well
- When attempting to Save and encountering a Read Only file or other exception, ShaderGraph will allow the user to retry as many times as they like

### Fixed
- Fixed a bug where ShaderGraph subgraph nodes would not update their slot names or order
- Fixed an issue where very old ShaderGraphs would fail to load because of uninitialized data [1269616](https://issuetracker.unity3d.com/issues/shadergraph-matrix-split-and-matrix-combine-shadergraphs-in-shadergraph-automated-tests-dont-open-throw-error)
- Fixed an issue where ShaderGraph previews didn't display correctly when setting a texture to "None" [1264932]
- Fixed an issue with the SampleVirtualTexture node in ShaderGraph, where toggling Automatic Streaming would cause the node to incorrectly display four output slots [1271618]
- Fixed an issue in ShaderGraph with integer-mode Vector1 properties throwing errors when the value is changed [1264930]
- Fixed a bug where ShaderGraph would not load graphs using Procedural VT nodes when the nodes were the project had them disabled [1271598]
- Fixed an issue where the ProceduralVT node was not updating any connected SampleVT nodes when the number of layers was changed [1274288]
- Fixed an issue with how unknown nodes were treated during validation
- Fixed an issue where ShaderGraph shaders did not reimport automatically when some of the included files changed [1269634]
- Fixed an issue where building a context menu on a dragging block node would leave it floating and undo/redo would result in a soft-lock
- Fixed an issue where ShaderGraph was logging error when edited in play mode [1274148].
- Fixed a bug where properties copied over with their graph inputs would not hook up correctly in a new graph [1274306]
- Fixed an issue where renaming a property in the blackboard at creation would trigger an error.
- Fixed an issue where ShaderGraph shaders did not reimport automatically when missing dependencies were reintroduced [1182895]
- Fixed an issue where ShaderGraph previews would not show error shaders when the active render pipeline is incompatible with the shader [1257015]
- ShaderGraph DDX, DDY, DDXY, and NormalFromHeight nodes do not allow themselves to be connected to vertex shader, as the derivative instructions can't be used [1209087]
- When ShaderGraph detects no active SRP, it will still continue to render the master preview, but it will use the error shader [1264642]
- VirtualTexture is no longer allowed as a SubGraph output (it is not supported by current system) [1254483]
- ShaderGraph Custom Function Node will now correctly convert function and slot names to valid HLSL identifiers [1258832]
- Fixed an issue where ShaderGraph Custom Function Node would reorder slots when you modified them [1280106]
- Fixed Undo handling when adding or removing Targets from a ShaderGraph [1257028]
- Fixed an issue with detection of circular subgraph dependencies [1269841]
- Fixed an issue where subgraph nodes were constantly changing their serialized data [1281975]
- Modifying a subgraph will no longer cause ShaderGraphs that use them to "reload from disk?" [1198885]
- Fixed issues with ShaderGraph title bar not correctly displaying the modified status * [1282031]
- Fixed issues where ShaderGraph could discard modified data without user approval when closed [1170503]
- Fixed an issue where ShaderGraph file dependency gathering would fail to include any files that didn't exist
- Fixed issues with ShaderGraph detection and handling of deleted graph files
- Fixed an issue where the ShaderGraph was corrupting the translation cache
- Fixed an issue where ShaderGraph would not prompt the user to save unsaved changes after an assembly reload
- Fixed an issue with Position Node not automatically upgrading
- Fixed an issue where failing SubGraphs would block saving graph files using them (recursion check would throw exceptions) [1283425]
- Fixed an issue where choosing "None" as the default texture for a texture property would not correctly preview the correct default color [1283782]
- Fixed some bugs with Color Nodes and properties that would cause incorrect collorspace conversions

## [10.0.0] - 2019-06-10
### Added
- Added the Internal Inspector which allows the user to view data contained in selected nodes and properties in a new floating graph sub-window. Also added support for custom property drawers to let you visualize any data type you like and expose it to the inspector.
- Added samples for Procedural Patterns to the package.
- You can now use the right-click context menu to delete Sticky Notes.
- You can now save your graph as a new Asset.
- Added support for vertex skinning when you use the DOTS animation package.
- You can now use the right-click context menu to set the precision on multiple selected nodes.
- You can now select unused nodes in your graph.
- When you start the Editor, Shader Graph now displays Properties in the Blackboard as collapsed.
- Updated the zoom level to let you zoom in further.
- Blackboard properties now have a __Duplicate__ menu option. When you duplicate properties, Shader Graph maintains the order, and inserts duplicates below the current selection.
- When you convert a node to a Sub Graph, the dialog now opens up in the directory of the original graph that contained the node. If the new Sub Graph is outside this directory, it also remembers that path for the next dialog to ease folder navigation.
- If Unity Editor Analytics are enabled, Shader Graph collects anonymous data about which nodes you use in your graphs. This helps the Shader Graph team focus our efforts on the most common graph scenarios, and better understand the needs of our customers. We don't track edge data and cannot recreate your graphs in any form.
- The Create Node Menu now has a tree view and support for fuzzy field searching.
- When a Shader Graph or Sub Graph Asset associated with a open window has been deleted, Unity now displays a dialog that asks whether you would like to save the graph as a new Asset or close the window.
- Added a drop-down menu to the PBR Master Node that lets you select the final coordinate space of normals delivered from the fragment function.
- Added support for users to drag and drop Blackboard Properties from one graph to another.
- Breaking out GraphData validation into clearer steps.
- Added AlphaToMask render state.
- Added a field to the Master Nodes that overrides the generated shader's ShaderGUI, which determines how a Material that uses a Shader Graph looks.
- Added Redirect Nodes. You can now double-click an edge to add a control point that allows you to route edges around other nodes and connect multiple output edges.
- Added `Compute Deformation` Node to read deformed vertex data from Dots Deformations.
- Added new graph nodes that allow sampling Virtual Textures
- Shader Graph now uses a new file format that is much friendlier towards version control systems and humans. Existing Shader Graphs and will use the new format next time they are saved.
- Added 'Allow Material Override' option to the built-in target for shader graph.

### Changed
- Changed the `Branch` node so that it uses a ternary operator (`Out = bool ? a : B`) instead of a linear interpolate function.
- Copied nodes are now pasted at the cursor location instead of slightly offset from their original location.
- Error messages reported on Sub Graph output nodes for invalid previews now present clearer information, with documentation support.
- Updated legacy COLOR output semantic to SV_Target in pixel shader for compatibility with DXC.
- Updated the functions in the `Normal From Height` node to avoid NaN outputs.
- Changed the Voronoi Node algorithm to increase the useful range of the input values and to always use float values internally to avoid clipping.
- Changed the `Reference Suffix` of Keyword Enum entries so that you cannot edit them, which ensures that material keywords compile properly.
- Updated the dependent version of `Searcher` to 4.2.0.
- Added support for `Linear Blend Skinning` Node to Universal Render Pipeline.
- Moved all code to be under Unity specific namespaces.
- Changed ShaderGraphImporter and ShaderSubgraphImporter so that graphs are imported before Models.
- Remove VFXTarget if VisualEffect Graph package isn't included.
- VFXTarget doesn't overwrite the shader export anymore, VFXTarget can be active with another target.

### Fixed
- Edges no longer produce errors when you save a Shader Graph.
- Shader Graph no longer references the `NUnit` package.
- Fixed a shader compatibility issue in the SRP Batcher when you use a hybrid instancing custom variable.
- Fixed an issue where Unity would crash when you imported a Shader Graph Asset with invalid formatting.
- Fixed an issue with the animated preview when there is no Camera with animated Materials in the Editor.
- Triplanar nodes no longer use Camera-relative world space by default in HDRP.
- Errors no longer occur when you activate `Enable GPU Instancing` on Shader Graph Materials. [1184870](https://issuetracker.unity3d.com/issues/universalrp-shader-compilation-error-when-using-gpu-instancing)
- Errors no longer occur when there are multiple tangent transform nodes on a graph. [1185752](https://issuetracker.unity3d.com/issues/shadergraph-fails-to-compile-with-redefinition-of-transposetangent-when-multiple-tangent-transform-nodes-are-plugged-in)
- The Main Preview for Sprite Lit and Sprite Unlit master nodes now displays the correct color. [1184656](https://issuetracker.unity3d.com/issues/shadergraph-preview-for-lit-and-unlit-master-node-wrong-color-when-color-is-set-directly-on-master-node)
- Shader Graph shaders in `Always Include Shaders` no longer crash builds. [1191757](https://issuetracker.unity3d.com/issues/lwrp-build-crashes-when-built-with-shadergraph-file-added-to-always-include-shaders-list)
- The `Transform` node now correctly transforms Absolute World to Object.
- Errors no longer occur when you change the precision of Sub Graphs. [1158413](https://issuetracker.unity3d.com/issues/shadergraph-changing-precision-of-sg-with-subgraphs-that-still-use-the-other-precision-breaks-the-generated-shader)
- Fixed an error where the UV channel drop-down menu on nodes had clipped text. [1188710](https://issuetracker.unity3d.com/issues/shader-graph-all-uv-dropdown-value-is-clipped-under-shader-graph)
- Added StencilOverride support.
- Sticky Notes can now be grouped properly.
- Fixed an issue where nodes couldn't be copied from a group.
- Fixed a bug that occurred when you duplicated multiple Blackboard properties or keywords simultaneously, where Shader Graph stopped working, potentially causing data loss.
- Fixed a bug where you couldn't reorder Blackboard properties.
- Shader Graph now properly duplicates the __Exposed__ status for Shader properties and keywords.
- Fixed a bug where the __Save Graph As__ dialog for a Shader or Sub Graph sometimes appeared in the wrong Project when you had multiple Unity Projects open simultaneously.
- Fixed an issue where adding the first output to a Sub Graph without any outputs prior caused Shader Graphs containing the Sub Graph to break.
- Fixed an issue where Shader Graph shaders using the `CameraNode` failed to build on PS4 with "incompatible argument list for call to 'mul'".
- Fixed a bug that caused problems with Blackboard property ordering.
- Fixed a bug where the redo functionality in Shader Graph often didn't work.
- Fixed a bug where using the Save As command on a Sub Graph raised an exception.
- Fixed a bug where the input fields sometimes didn't render properly. [1176268](https://issuetracker.unity3d.com/issues/shadergraph-input-fields-get-cut-off-after-minimizing-and-maximizing-become-unusable)
- Fixed a bug where the Gradient property didn't work with all system locales. [1140924](https://issuetracker.unity3d.com/issues/shader-graph-shader-doesnt-compile-when-using-a-gradient-property-and-a-regional-format-with-comma-decimal-separator-is-used)
- Fixed a bug where Properties in the Blackboard could have duplicate names.
- Fixed a bug where you could drag the Blackboard into a graph even when you disabled the Blackboard.
- Fixed a bug where the `Vertex Normal` slot on master nodes needed vertex normal data input to compile. [1193348](https://issuetracker.unity3d.com/issues/hdrp-unlit-shader-plugging-anything-into-the-vertex-normal-input-causes-shader-to-fail-to-compile)
- Fixed a bug where `GetWorldSpaceNormalizeViewDir()` could cause undeclared indentifier errors. [1190606](https://issuetracker.unity3d.com/issues/view-dir-node-plugged-into-vertex-position-creates-error-undeclared-identifier-getworldspacenormalizeviewdir)
- Fixed a bug where Emission on PBR Shader Graphs in the Universal RP would not bake to lightmaps. [1190225](https://issuetracker.unity3d.com/issues/emissive-custom-pbr-shadergraph-material-only-works-for-primitive-unity-objects)
- Fixed a bug where Shader Graph shaders were writing to `POSITION` instead of `SV_POSITION`, which caused PS4 builds to fail.
- Fixed a bug where `Object to Tangent` transforms in the `Transform` node used the wrong matrix. [1162203](https://issuetracker.unity3d.com/issues/shadergraph-transform-node-from-object-to-tangent-space-uses-the-wrong-matrix)
- Fixed an issue where boolean keywords in a Shader Graph caused HDRP Material features to fail. [1204827](https://issuetracker.unity3d.com/issues/hdrp-shadergraph-adding-a-boolean-keyword-to-an-hdrp-lit-shader-makes-material-features-not-work)
- Fixed a bug where Object space normals scaled with Object Scale.
- Documentation links on nodes now point to the correct URLs and package versions.
- Fixed an issue where Sub Graphs sometimes had duplicate names when you converted nodes into Sub Graphs.
- Fixed an issue where the number of ports on Keyword nodes didn't update when you added or removed Enum Keyword entries.
- Fixed an issue where colors in graphs didn't update when you changed a Blackboard Property's precision while the Color Mode is set to Precision.
- Fixed a bug where custom mesh in the Master Preview didn't work.
- Fixed a number of memory leaks that caused Shader Graph assets to stay in memory after closing the Shader Graph window.
- You can now smoothly edit controls on the `Dielectric Specular` node.
- Fixed Blackboard Properties to support scientific notation.
- Fixed a bug where warnings in the Shader Graph or Sub Graph were treated as errors.
- Fixed a bug where the error `Output value 'vert' is not initialized` displayed on all PBR graphs in Universal. [1210710](https://issuetracker.unity3d.com/issues/output-value-vert-is-not-completely-initialized-error-is-thrown-when-pbr-graph-is-created-using-urp)
- Fixed a bug where PBR and Unlit master nodes in Universal had Alpha Clipping enabled by default.
- Fixed an issue in where analytics wasn't always working.
- Fixed a bug where if a user had a Blackboard Property Reference start with a digit the generated shader would be broken.
- Avoid unintended behavior by removing the ability to create presets from Shader Graph (and Sub Graph) assets. [1220914](https://issuetracker.unity3d.com/issues/shadergraph-preset-unable-to-open-editor-when-clicking-on-open-shader-editor-in-the-shadersubgraphimporter)
- Fixed a bug where undo would make the Master Preview visible regardless of its toggle status.
- Fixed a bug where any change to the PBR master node settings would lose connection to the normal slot.
- Fixed a bug where the user couldn't open up HDRP Master Node Shader Graphs without the Render Pipeline set to HDRP.
- Fixed a bug where adding a HDRP Master Node to a Shader Graph would softlock the Shader Graph.
- Fixed a bug where shaders fail to compile due to `#pragma target` generation when your system locale uses commas instead of periods.
- Fixed a compilation error when using Hybrid Renderer due to incorrect positioning of macros.
- Fixed a bug where the `Create Node Menu` lagged on load. Entries are now only generated when property, keyword, or subgraph changes are detected. [1209567](https://issuetracker.unity3d.com/issues/shadergraph-opening-node-search-window-is-unnecessarily-slow).
- Fixed a bug with the `Transform` node where converting from `Absolute World` space in a sub graph causes invalid subscript errors. [1190813](https://issuetracker.unity3d.com/issues/shadergraph-invalid-subscript-errors-are-thrown-when-connecting-a-subgraph-with-transform-node-with-unlit-master-node)
- Fixed a bug where depndencies were not getting included when exporting a shadergraph and subgraphs
- Fixed a bug where adding a " to a property display name would cause shader compilation errors and show all nodes as broken
- Fixed a bug where the `Position` node would change coordinate spaces from `World` to `Absolute World` when shaders recompile. [1184617](https://issuetracker.unity3d.com/product/unity/issues/guid/1184617/)
- Fixed a bug where instanced shaders wouldn't compile on PS4.
- Fixed a bug where switching a Color Nodes' Mode between Default and HDR would cause the Color to be altered incorrectly.
- Fixed a bug where nodes dealing with matricies would sometimes display a preview, sometimes not.
- Optimized loading a large Shader Graph. [1209047](https://issuetracker.unity3d.com/issues/shader-graph-unresponsive-editor-when-using-large-graphs)
- Fixed NaN issue in triplanar SG node when blend goes to 0.
- Fixed a recurring bug where node inputs would get misaligned from their ports. [1224480]
- Fixed an issue where Blackboard properties would not duplicate with `Precision` or `Hybrid Instancing` options.
- Fixed an issue where `Texture` properties on the Blackboard would not duplicate with the same `Mode` settings.
- Fixed an issue where `Keywords` on the Blackboard would not duplicate with the same `Default` value.
- Shader Graph now requests preview shader compilation asynchronously. [1209047](https://issuetracker.unity3d.com/issues/shader-graph-unresponsive-editor-when-using-large-graphs)
- Fixed an issue where Shader Graph would not compile master previews after an assembly reload.
- Fixed issue where `Linear Blend Skinning` node could not be converted to Sub Graph [1227087](https://issuetracker.unity3d.com/issues/shadergraph-linear-blend-skinning-node-reports-an-error-and-prevents-shader-compilation-when-used-within-a-sub-graph)
- Fixed a compilation error in preview shaders for nodes requiring view direction.
- Fixed undo not being recorded properly for setting active master node, graph precision, and node defaults.
- Fixed an issue where Custum Function nodes and Sub Graph Output nodes could no longer rename slots.
- Fixed a bug where searcher entries would not repopulate correctly after an undo was perfromed (https://fogbugz.unity3d.com/f/cases/1241018/)
- Fixed a bug where Redirect Nodes did not work as inputs to Custom Function Nodes. [1235999](https://issuetracker.unity3d.com/product/unity/issues/guid/1235999/)
- Fixed a bug where changeing the default value on a keyword would reset the node input type to vec4 (https://fogbugz.unity3d.com/f/cases/1216760/)
- Fixed a soft lock when you open a graph when the blackboard hidden.
- Fixed an issue where keyboard navigation in the Create Node menu no longer worked. [1253544]
- Preview correctly shows unassigned VT texture result, no longer ignores null textures
- Don't allow duplicate VT layer names when renaming layers
- Moved VT layer TextureType to the VTProperty from the SampleVT node
- Fixed the squished UI of VT property layers
- Disallow Save As and Convert to Subgraph that would create recursive dependencies
- Fixed an issue where the user would not get a save prompt on application close [1262044](https://issuetracker.unity3d.com/product/unity/issues/guid/1262044/)
- Fixed bug where output port type would not visually update when input type changed (for example from Vec1 to Vec3) [1259501](https://issuetracker.unity3d.com/product/unity/issues/guid/1259501/)
- Fixed an issue with how we collected/filtered nodes for targets. Applied the work to the SearchWindowProvider as well
- Fixed a bug where the object selector for Custom Function Nodes did not update correctly. [1176129](https://issuetracker.unity3d.com/product/unity/issues/guid/1176129/)
- Fixed a bug where whitespaces were allowed in keyword reference names
- Fixed a bug where the Create Node menu would override the Object Field selection window. [1176125](https://issuetracker.unity3d.com/issues/shader-graph-object-input-field-with-space-bar-shortcut-opens-shader-graph-search-window-and-object-select-window)
- Fixed a bug where the Main Preview window was no longer a square aspect ratio. [1257053](https://issuetracker.unity3d.com/product/unity/issues/guid/1257053/)
- Fixed a bug where the size of the Graph Inspector would not save properly. [1257084](https://issuetracker.unity3d.com/product/unity/issues/guid/1257084/)
- Replace toggle by an enumField for lit/unlit with VFXTarget
- Alpha Clipping option in Graph inspector now correctly hides and indents dependent options. (https://fogbugz.unity3d.com/f/cases/1257041/)
- Fixed a bug where changing the name of a property did not update nodes on the graph. [1249164](https://issuetracker.unity3d.com/product/unity/issues/guid/1249164/)
- Fixed a crash issue when ShaderGraph included in a project along with DOTS assemblies
- Added missing SampleVirtualTextureNode address mode control in ShaderGraph
- Fixed a badly named control on SampleVirtualTextureNode in ShaderGraph
- Fixed an issue where multiple SampleVirtualTextureNodes created functions with names that may collide in ShaderGraph
- Made sub graph importer deterministic to avoid cascading shader recompiles when no change was present.
- Adjusted style sheet for Blackboard to prevent ui conflicts.
- Fixed a bug where the SampleVirtualTexture node would delete slots when changing its LOD mode
- Use preview of the other target if VFXTarget is active.

## [7.1.1] - 2019-09-05
### Added
- You can now define shader keywords on the Blackboard. Use these keywords on the graph to create static branches in the generated shader.
- The tab now shows whether you are working in a Sub Graph or a Shader Graph file.
- The Shader Graph importer now bakes the output node type name into a meta-data object.

### Fixed
- The Shader Graph preview no longer breaks when you create new PBR Graphs.
- Fixed an issue where deleting a group and a property at the same time would cause an error.
- Fixed the epsilon that the Hue Node uses to avoid NaN on platforms that support half precision.
- Emission nodes no longer produce errors when you use them in Sub Graphs.
- Exposure nodes no longer produce errors when you use them in Sub Graphs.
- Unlit master nodes no longer define unnecessary properties in the Universal Render Pipeline.
- Errors no longer occur when you convert a selection to a Sub Graph.
- Color nodes now handle Gamma and Linear conversions correctly.
- Sub Graph Output nodes now link to the correct documentation page.
- When you use Keywords, PBR and Unlit master nodes no longer produce errors.
- PBR master nodes now calculate Global Illumination (GI) correctly.
- PBR master nodes now apply surface normals.
- PBR master nodes now apply fog.
- The Editor now displays correct errors for missing or deleted Sub Graph Assets.
- You can no longer drag and drop recursive nodes onto Sub Graph Assets.

## [7.0.1] - 2019-07-25
### Changed
- New Shader Graph windows are now docked to either existing Shader Graph windows, or to the Scene View.

### Fixed
- Fixed various dependency tracking issues with Sub Graphs and HLSL files from Custom Function Nodes.
- Fixed an error that previously occurred when you used `Sampler State` input ports on Sub Graphs.
- `Normal Reconstruct Z` node is now compatible with both fragment and vertex stages.
- `Position` node now draws the correct label for **Absolute World**.
- Node previews now inherit preview type correctly.
- Normal maps now unpack correctly for mobile platforms.
- Fixed an error that previously occurred when you used the Gradient Sample node and your system locale uses commas instead of periods.
- Fixed an issue where you couldn't group several nodes.

## [7.0.0] - 2019-07-10
### Added
- You can now use the `SHADERGRAPH_PREVIEW` keyword in `Custom Function Node` to generate different code for preview Shaders.
- Color Mode improves node visibility by coloring the title bar by Category, Precision, or custom colors.
- You can now set the precision of a Shader Graph and individual nodes.
- Added the `_TimeParameters` variable which contains `Time`, `Sin(Time)`, and `Cosine(Time)`
- _Absolute World_ space on `Position Node` now provides absolute world space coordinates regardless of the active render pipeline.
- You can now add sticky notes to graphs.

### Changed
- The `Custom Function Node` now uses an object field to reference its source when using `File` mode.
- To enable master nodes to generate correct motion vectors for time-based vertex modification, time is now implemented as an input to the graph rather than as a global uniform.
- **World** space on `Position Node` now uses the default world space coordinates of the active render pipeline.

### Fixed
- Fixed an error in `Custom Function Node` port naming.
- `Sampler State` properties and nodes now serialize correctly.
- Labels in the Custom Port menu now use the correct coloring when using the Personal skin.
- Fixed an error that occured when creating a Sub Graph from a selection containing a Group Node.
- When you change a Sub Graph, Shader Graph windows now correctly reload.
- When you save a Shader Graph, all other Shader Graph windows no longer re-compile their preview Shaders.
- Shader Graph UI now draws with correct styling for 2019.3.
- When deleting edge connections to nodes with a preview error, input ports no longer draw in the wrong position.
- Fixed an error involving deprecated components from VisualElements.
- When you convert nodes to a Sub Graph, the nodes are now placed correctly in the Sub Graph.
- The `Bitangent Vector Node` now generates all necessary shader requirements.

## [6.7.0-preview] - 2019-05-16
### Added
- Added a hidden path namespace for Sub Graphs to prevent certain Sub Graphs from populating the Create Node menu.

### Changed
- Anti-aliasing (4x) is now enabled on Shader Graph windows.

### Fixed
- When you click on the gear icon, Shader Graph now focuses on the selected node, and brings the settings menu to front view.
- Sub Graph Output and Custom Function Node now validate slot names, and display an appropriate error badge when needed.
- Remaining outdated documentation has been removed.
- When you perform an undo or redo to an inactive Shader Graph window, the window no longer breaks.
- When you rapidly perform an undo or redo, Shader Graph windows no longer break.
- Sub Graphs that contain references to non-existing Sub Graphs no longer break the Sub Graph Importer.
- You can now reference sub-assets such as Textures.
- You can now reference Scene Color and Scene Depth correctly from within a Sub Graph.
- When you create a new empty Sub Graph, it no longer shows a warning about a missing output.
- When you create outputs that start with a digit, Shader generation no longer fails.
- You can no longer add nodes that are not allowed into Sub Graphs.
- A graph must now always contain at least one Master Node.
- Duplicate output names are now allowed.
- Fixed an issue where the main preview was always redrawing.
- When you set a Master Node as active, the Main Preview now shows the correct result.
- When you save a graph that contains a Sub Graph node, the Shader Graph window no longer freezes.
- Fixed an error that occured when using multiple Sampler State nodes with different parameters.
- Fixed an issue causing default inputs to be misaligned in certain cases.
- You can no longer directly connect slots with invalid types. When the graph detects that situation, it now doesn't break and gives an error instead.

## [6.6.0] - 2019-04-01
### Added
- You can now add Matrix, Sampler State and Gradient properties to the Blackboard.
- Added Custom Function node. Use this node to define a custom HLSL function either via string directly in the graph, or via a path to an HLSL file.
- You can now group nodes by pressing Ctrl + G.
- Added "Delete Group and Contents" and removed "Ungroup All Nodes" from the context menu for groups.
- You can now use Sub Graphs in other Sub Graphs.
- Preview shaders now compile in the background, and only redraw when necessary.

### Changed
- Removed Blackboard fields, which had no effect on Sub Graph input ports, from the Sub Graph Blackboard.
- Subgraph Output node is now called Outputs.
- Subgraph Output node now supports renaming of ports.
- Subgraph Output node now supports all port types.
- Subgraph Output node now supports reordering ports.
- When you convert nodes to a Sub Graph, Shader Graph generates properties and output ports in the Sub Graph, and now by default, names those resulting properties and output ports based on their types.
- When you delete a group, Shader Graph now deletes the Group UI, but doesn't delete the nodes inside.

### Fixed
- You can now undo edits to Vector port default input fields.
- You can now undo edits to Gradient port default input fields.
- Boolean port input fields now display correct values when you undo changes.
- Vector type properties now behave as expected when you undo changes.
- Fixed an error that previously occurred when you opened saved Shader Graphs containing one or more Voronoi nodes.
- You can now drag normal map type textures on to a Shader Graph to create Sample Texture 2D nodes with the correct type set.
- Fixed the Multiply node so default input values are applied correctly.
- Added padding on input values for Blend node to prevent NaN outputs.
- Fixed an issue where `IsFaceSign` would not compile within Sub Graph Nodes.
- Null reference errors no longer occur when you remove ports with connected edges.
- Default input fields now correctly hide and show when connections change.

## [6.5.0] - 2019-03-07

### Fixed
- Fixed master preview for HDRP master nodes when alpha clip is enabled.

## [6.4.0] - 2019-02-21
### Fixed
- Fixed the Transform node, so going from Tangent Space to any other space now works as expected.

## [6.3.0] - 2019-02-18
### Fixed
- Fixed an issue where the Normal Reconstruct Z Node sometimes caused Not a Number (NaN) errors when using negative values.

## [6.2.0] - 2019-02-15
### Fixed
- Fixed the property blackboard so it no longer goes missing or turns very small.

### Changed
- Code refactor: all macros with ARGS have been swapped with macros with PARAM. This is because the ARGS macros were incorrectly named.

## [6.1.0] - 2019-02-13

## [6.0.0] - 2019-02-23
### Added
- When you hover your cursor over a property in the blackboard, this now highlights the corresponding property elements in your Shader Graph. Similarly, if you hover over a property in the Shader Graph itself, this highlights the corresponding property in the blackboard.
- Property nodes in your Shader Graph now have a similar look and styling as the properties in the blackboard.

### Changed
- Errors in the compiled shader are now displayed as badges on the appropriate node.
- In the `Scene Depth` node you can now choose the depth sampling mode: `Linear01`, `Raw` or `Eye`.

### Fixed
- When you convert an inline node to a `Property` node, this no longer allows duplicate property names.
- When you move a node, you'll now be asked to save the Graph file.
- You can now Undo edits to Property parameters on the Blackboard.
- You can now Undo conversions between `Property` nodes and inline nodes.
- You can now Undo moving a node.
- You can no longer select the `Texture2D` Property type `Mode`, if the Property is not exposed.
- The `Vector1` Property type now handles default values more intuitively when switching `Mode` dropdown.
- The `Color` node control is now a consistent width.
- Function declarations no longer contain double delimiters.
- The `Slider` node control now functions correctly.
- Fixed an issue where the Editor automatically re-imported Shader Graphs when there were changes to the asset database.
- Reverted the visual styling of various graph elements to their previous correct states.
- Previews now repaint correctly when Unity does not have focus.
- Code generation now works correctly for exposed Vector1 shader properties where the decimal separator is not a dot.
- The `Rotate About Axis` node's Modes now use the correct function versions.
- Shader Graph now preserves grouping when you convert nodes between property and inline.
- The `Flip` node now greys out labels for inactive controls.
- The `Boolean` property type now uses the `ToggleUI` property attribute, so as to not generate keywords.
- The `Normal Unpack` node no longer generates errors in Object space.
- The `Split` node now uses values from its default Port input fields.
- The `Channel Mask` node now allows multiple node instances, and no longer generates any errors.
- Serialized the Alpha control value on the `Flip` node.
- The `Is Infinite` and `Is NaN` nodes now use `Vector 1` input ports, but the output remains the same.
- You can no longer convert a node inside a `Sub Graph` into a `Sub Graph`, which previously caused errors.
- The `Transformation Matrix` node's Inverse Projection and Inverse View Projection modes no longer produce errors.
- The term `Shader Graph` is now captilized correctly in the Save Graph prompt.

## [5.2.0] - 2018-11-27
### Added
- Shader Graph now has __Group Node__, where you can group together several nodes. You can use this to keep your Graphs organized and nice.

### Fixed
- The expanded state of blackboard properties are now remembered during a Unity session.

## [5.1.0] - 2018-11-19
### Added
- You can now show and hide the Main Preview and the Blackboard from the toolbar.

### Changed
- The Shader Graph package is no longer in preview.
- Moved `NormalBlendRNM` node to a dropdown option on `Normal Blend` node.
- `Sample Cubemap` node now has a `SamplerState` slot.
- New Sub Graph assets now default to the "Sub Graphs" path in the Create Node menu.
- New Shader Graph assets now default to the "Shader Graphs" path in the Shader menu.
- The `Light Probe` node is now a `Baked GI` node. When you use LWRP with lightmaps, this node now returns the correct lightmap data. This node is supported in HDRP.
- `Reflection Probe` nodes now only work with LWRP. This solves compilation errors in HDRP.
- `Ambient` nodes now only work with LWRP. This solves compilation errors in HDRP.
- `Fog` nodes now only work with LWRP. This solves compilation errors in HDRP.
- In HDRP, the `Position` port for the `Object` node now returns the absolute world position.
- The `Baked GI`, `Reflection Probe`, and `Ambient` nodes are now in the `Input/Lighting` category.
- The master node no longer has its own preview, because it was redundant. You can see the results for the master node in the Main Preview.

### Fixed
- Shadow projection is now correct when using the `Unlit` master node with HD Render Pipeline.
- Removed all direct references to matrices
- `Matrix Construction` nodes with different `Mode` values now evaluate correctly.
- `Is Front Face` node now works correctly when connected to `Alpha` and `AlphaThreshold` slots on the `PBR` master node.
- Corrected some instances of incorrect port dimensions on several nodes.
- `Scene Depth` and `Scene Color` nodes now work in single pass stereo in Lightweight Render Pipeline.
- `Channel Mask` node controls are now aligned correctly.
- In Lightweight Render Pipeline, Pre-multiply surface type now matches the Lit shader.
- Non-exposed properties in the blackboard no longer have a green dot next to them.
- Default reference name for shader properties are now serialized. You cannot change them after initial creation.
- When you save Shader Graph and Sub Graph files, they're now automatically checked out on version control.
- Shader Graph no longer throws an exception when you double-click a folder in the Project window.
- Gradient Node no longer throws an error when you undo a deletion.

## [5.0.0-preview] - 2018-09-28

## [4.0.0-preview] - 2018-09-28
### Added
- Shader Graph now supports the High Definition Render Pipeline with both PBR and Unlit Master nodes. Shaders built with Shader Graph work with both the Lightweight and HD render pipelines.
- You can now modify vertex position via the Position slot on the PBR and Unlit Master nodes. By default, the input to this node is object space position. Custom inputs to this slot should specify the absolute local position of a given vertex. Certain nodes (such as Procedural Shapes) are not viable in the vertex shader. Such nodes are incompatible with this slot.
- You can now edit the Reference name for a property. To do so, select the property and type a new name next to Reference. If you want to reset to the default name, right-click Reference, and select Reset reference.
- In the expanded property window, you can now toggle whether the property is exposed.
- You can now change the path of Shader Graphs and Sub Graphs. When you change the path of a Shader Graph, this modifies the location it has in the shader selection list. When you change the path of Sub Graph, it will have a different location in the node creation menu.
- Added `Is Front Face` node. With this node, you can change graph output depending on the face sign of a given fragment. If the current fragment is part of a front face, the node returns true. For a back face, the node returns false. Note: This functionality requires that you have enabled **two sided** on the Master node.
- Gradient functionality is now available via two new nodes: Sample Gradient and Gradient Asset. The Sample Gradient node samples a gradient given a Time parameter. You can define this gradient on the Gradient slot control view. The Gradient Asset node defines a gradient that can be sampled by multiple Sample Gradient nodes using different Time parameters.
- Math nodes now have a Waves category. The category has four different nodes: Triangle wave, Sawtooth wave, Square wave, and Noise Sine wave. The Triangle, Sawtooth, and Square wave nodes output a waveform with a range of -1 to 1 over a period of 1. The Noise Sine wave outputs a standard Sine wave with a range of -1 to 1 over a period of 2 * pi. For variance, random noise is added to the amplitude of the Sine wave, within a determined range.
- Added `Sphere Mask` node for which you can indicate the starting coordinate and center point. The sphere mask uses these with the **Radius** and **Hardness** parameters. Sphere mask functionality works in both 2D and 3D spaces, and is based on the vector coordinates in the **Coords and Center** input.
- Added support for Texture 3D and Texture 2D Array via two new property types and four new nodes.
- A new node `Texture 2D LOD` has been added for LOD functionality on a Texture 2D Sample. Sample Texture 2D LOD uses the exact same input and output slots as Sample Texture 2D, but also includes an input for level of detail adjustments via a Vector1 slot.
- Added `Texel Size` node, which allows you to get the special texture properties of a Texture 2D Asset via the `{texturename}_TexelSize` variable. Based on input from the Texture 2D Asset, the node outputs the width and height of the texel size in Vector1 format.
- Added `Rotate About Axis` node. This allows you to rotate a 3D vector space around an axis. For the rotation, you can specify an amount of degrees or a radian value.
- Unpacking normal maps in object space.
- Unpacking derivative maps option on sample texture nodes.
- Added Uint type for instancing support.
- Added HDR option for color material slots.
- Added definitions used by new HD Lit Master node.
- Added a popup control for a string list.
- Added conversion type (position/direction) to TransformNode.
- In your preview for nodes that are not master nodes, pixels now display as pink if they are not finite.

### Changed
- The settings for master nodes now live in a small window that you can toggle on and off. Here, you can change various rendering settings for your shader.
- There are two Normal Derive Nodes: `Normal From Height` and `Normal Reconstruct Z`.
  `Normal From Height` uses Vector1 input to derive a normal map.
  `Normal Reconstruct Z` uses the X and Y components in Vector2 input to derive the proper Z value for a normal map.
- The Texture type default input now accepts render textures.
- HD PBR subshader no longer duplicates surface description code into vertex shader.
- If the current render pipeline is not compatible, master nodes now display an error badge.
- The preview shader now only considers the current render pipeline. Because of this there is less code to compile, so the preview shader compiles faster.
- When you rename a shader graph or sub shader graph locally on your disk, the title of the Shader Graph window, black board, and preview also updates.
- Removed legacy matrices from Transfomation Matrix node.
- Texture 2D Array and Texture 3D nodes can no longer be used in the vertex shader.
- `Normal Create` node has been renamed to `Normal From Texture`.
- When you close the Shader Graph after you have modified a file, the prompt about saving your changes now shows the file name as well.
- `Blend` node now supports Overwrite mode.
- `Simple Noise` node no longer has a loop.
- The `Polygon` node now calculates radius based on apothem.
- `Normal Strength` node now calculates Z value more accurately.
- You can now connect Sub Graphs to vertex shader slots. If a node in the Sub Graph specifies a shader stage, that specific Sub Graph node is locked to that stage. When an instance of a Sub Graph node is connected to a slot that specifies a shader stage, all slots on that instance are locked to the stage.
- Separated material options and tags.
- Master node settings are now recreated when a topological modification occurs.

### Fixed
- Vector 1 nodes now evaluate correctly. ([#334](https://github.com/Unity-Technologies/ShaderGraph/issues/334) and [#337](https://github.com/Unity-Technologies/ShaderGraph/issues/337))
- Properties can now be copied and pasted.
- Pasting a property node into another graph will now convert it to a concrete node. ([#300](https://github.com/Unity-Technologies/ShaderGraph/issues/300) and [#307](https://github.com/Unity-Technologies/ShaderGraph/pull/307))
- Nodes that are copied from one graph to another now spawn in the center of the current view. ([#333](https://github.com/Unity-Technologies/ShaderGraph/issues/333))
- When you edit sub graph paths, the search window no longer yields a null reference exception.
- The blackboard is now within view when deserialized.
- Your system locale can no longer cause incorrect commands due to full stops being converted to commas.
- Deserialization of subgraphs now works correctly.
- Sub graphs are now suffixed with (sub), so you can tell them apart from other nodes.
- Boolean and Texture type properties now function correctly in sub-graphs.
- The preview of a node does not obstruct the selection outliner anymore.
- The Dielectric Specular node no longer resets its control values.
- You can now copy, paste, and duplicate sub-graph nodes with vector type input ports.
- The Lightweight PBR subshader now normalizes normal, tangent, and view direction correctly.
- Shader graphs using alpha clip now generate correct depth and shadow passes.
- `Normal Create` node has been renamed to `Normal From Texture`.
- The preview of nodes now updates correctly.
- Your system locale can no longer cause incorrect commands due to full stops being converted to commas.
- `Show Generated Code` no longer throws an "Argument cannot be null" error.
- Sub Graphs now use the correct generation mode when they generate preview shaders.
- The `CodeFunctionNode` API now generates correct function headers when you use `DynamicMatrix` type slots.
- Texture type input slots now set correct default values for 'Normal' texture type.
- SpaceMaterialSlot now reads correct slot.
- Slider node control now functions correctly.
- Shader Graphs no longer display an error message intended for Sub Graphs when you delete properties.
- The Shader Graph and Sub Shader Graph file extensions are no longer case-sensitive.
- The dynamic value slot type now uses the correct decimal separator during HLSL generation.
- Fixed an issue where Show Generated Code could fail when external editor was not set.
- In the High Definition Render Pipeline, Shader Graph now supports 4-channel UVs.
- The Lightweight PBR subshader now generates the correct meta pass.
- Both PBR subshaders can now generate indirect light from emission.
- Shader graphs now support the SRP batcher.
- Fixed an issue where floatfield would be parsed according to OS locale settings with .NET 4.6
