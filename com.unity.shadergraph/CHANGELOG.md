# Changelog
All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [7.4.3] - 2020-08-06

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [7.4.2] - 2020-08-04

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [7.4.1] - 2020-06-03

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [7.4.0] - 2020-05-22

### Fixed
- Fixed a bug where the `Position` node would change coordinate spaces from `World` to `Absolute World` when shaders recompile. [1184617](https://issuetracker.unity3d.com/product/unity/issues/guid/1184617/)
 - Fixed a bug where `Scene Depth` nodes would stop working after adding a keyword on the blackboard. [1203333](https://issuetracker.unity3d.com/product/unity/issues/guid/1203333/)
 - Fixed undo not being recorded properly for setting active master node, graph precision, and node defaults.

## [7.3.0] - 2020-03-11

### Added
- If Unity Editor Analytics are enabled, Shader Graph collects anonymous data about which nodes you use in your graphs. This helps the Shader Graph team focus our efforts on the most common graph scenarios, and better understand the needs of our customers. We don't track edge data and cannot recreate your graphs in any form.

### Changed
- Changed the `Branch` node so that it uses a ternary operator (`Out = bool ? a : B`) instead of a linear interpolate function.

### Added
- Added a field to the Master Nodes that overrides the generated shader's ShaderGUI.

### Fixed
- Fixed a bug where PBR and Unlit master nodes in the Universal RP had Alpha Clipping enabled by default.
- Fixed an issue where Sub Graphs sometimes had duplicate names when you converted nodes into Sub Graphs. 
- Fixed an issue where the number of ports on Keyword nodes didn't update when you added or removed Enum Keyword entries.
- Fixed an issue where colors in graphs didn't update when you changed a Blackboard Property's precision while the Color Mode is set to Precision.
- Fixed a bug where if a user had a Blackboard Property Reference start with a digit the generated shader would be broken.
- Fixed a bug where undo would make the Master Preview visible regardless of its toggle status.
- Fixed a bug where any change to the PBR master node settings would lose connection to the normal slot.
- Fixed a bug where the user couldn't open up HDRP Master Node Shader Graphs without the Render Pipeline set to HDRP.
- Fixed a bug where adding a HDRP Master Node to a Shader Graph would softlock the Shader Graph.
- Fixed a bug where fog density node always returns 0 in the shader preview window when connected to an Unlit Master node. 
- Fixed a bug with the `Transform` node where converting from `Absolute World` space in a sub graph causes invalid subscript errors. [1190813](https://issuetracker.unity3d.com/issues/shadergraph-invalid-subscript-errors-are-thrown-when-connecting-a-subgraph-with-transform-node-with-unlit-master-node)
- Optimized loading a large Shader Graph. [1209047](https://issuetracker.unity3d.com/issues/shader-graph-unresponsive-editor-when-using-large-graphs)
- Fixed NaN issue in triplanar SG node when blend goes to 0.

## [7.2.0] - 2020-02-10

### Fixed
- Fixed an issue where Shader Graph shaders using the `CameraNode` failed to build on PS4 with "incompatible argument list for call to 'mul'".
- Fixed a bug where the redo functionality in Shader Graph often didn't work.
- Fixed a bug where the input fields sometimes didn't render properly. [1176268](https://issuetracker.unity3d.com/issues/shadergraph-input-fields-get-cut-off-after-minimizing-and-maximizing-become-unusable)
- Fixed a bug where the Gradient property didn't work with all system locales. [1140924](https://issuetracker.unity3d.com/issues/shader-graph-shader-doesnt-compile-when-using-a-gradient-property-and-a-regional-format-with-comma-decimal-separator-is-used)
- Fixed a bug where Properties in the Blackboard could have duplicate names.
- Fixed a bug where the `Vertex Normal` slot on master nodes needed vertex normal data input to compile. [1193348](https://issuetracker.unity3d.com/issues/hdrp-unlit-shader-plugging-anything-into-the-vertex-normal-input-causes-shader-to-fail-to-compile)
- Fixed a bug where `GetWorldSpaceNormalizeViewDir()` could cause undeclared indentifier errors. [1190606](https://issuetracker.unity3d.com/issues/view-dir-node-plugged-into-vertex-position-creates-error-undeclared-identifier-getworldspacenormalizeviewdir)
- Fixed a bug where Emission on PBR Shader Graphs in the Universal RP would not bake to lightmaps. [1190225](https://issuetracker.unity3d.com/issues/emissive-custom-pbr-shadergraph-material-only-works-for-primitive-unity-objects)
- Fixed a bug where Shader Graph shaders were writing to `POSITION` instead of `SV_POSITION`, which caused PS4 builds to fail.
- Fixed a bug where the error `Output value 'vert' is not initialized` displayed on all PBR graphs in Universal. [1210710](https://issuetracker.unity3d.com/issues/output-value-vert-is-not-completely-initialized-error-is-thrown-when-pbr-graph-is-created-using-urp)
- Documentation links on nodes now point to the correct URLs and package versions.
- Fixed a bug where parts of the Blackboard remain visible even after disabling its visibility.
- Fixed a number of memory leaks causing Shader Graph assets to stay in memory after closing the Shader Graph window.

## [7.1.8] - 2020-01-20
### Fixed
- Fixed a number of memory leaks that caused Shader Graph assets to stay in memory after closing the Shader Graph window.

## [7.1.7] - 2019-12-11
### Added
- Added StencilOverride support.

### Fixed
- You can now smoothly edit controls on the `Dielectric Specular` node.
- Fixed shader compile errors when trying to use tessellation shaders with PlayStation VR on PS4.

## [7.1.6] - 2019-11-22
### Fixed
- Errors no longer occur when you change the precision of Sub Graphs. [1158413](https://issuetracker.unity3d.com/issues/shadergraph-changing-precision-of-sg-with-subgraphs-that-still-use-the-other-precision-breaks-the-generated-shader)
- Fixed an error where the UV channel drop-down menu on nodes had clipped text. [1188710](https://issuetracker.unity3d.com/issues/shader-graph-all-uv-dropdown-value-is-clipped-under-shader-graph)
- Fixed an issue where adding the first output to a Sub Graph without any outputs prior caused Shader Graphs containing the Sub Graph to break.

## [7.1.5] - 2019-11-15

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [7.1.4] - 2019-11-13

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [7.1.3] - 2019-11-04

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [7.1.2] - 2019-09-19
### Added
- Added samples for Procedural Patterns to the package.
- You can now use the right-click context menu to delete Sticky Notes.
- Added support for vertex skinning when you use the DOTS animation package.

### Fixed 
- The Editor now displays correct errors for missing or deleted Sub Graph Assets.
- You can no longer drag and drop recursive nodes onto Sub Graph Assets.
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
