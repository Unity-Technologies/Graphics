# Changelog
All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [6.9.2] - 2019-10-04
### Fixed
- The Shader Graph window no longer freezes when you rename properties.

## [6.9.1] - 2019-07-29
### Fixed
- You can no longer directly connect slots with invalid types. When the graph detects that situation, it now doesn't break and gives an error instead.
- Fixed an error that previously occurred when you used `Sampler State` input ports on Sub Graphs.
- Fixed various dependency tracking issues with Sub Graphs and HLSL files from Custom Function Nodes.
- Added support of Alembic velocity to various shaders
- Renaming Properties no longer freezes the Shader Graph window.
- Fixed an error that previously occurred when you used the Gradient Sample node and your system locale uses commas instead of periods.
- Fixed an issue with the animated preview when there is no Camera with animated Materials in the Editor.

## [6.9.0] - 2019-07-05

### Version Updated
The version number for this package has increased due to a version update of a related graphics package.

### Fixed
- The `Bitangent Vector Node` now generates all necessary shader requirements.

## [6.8.0] - 2019-06-12
### Added
- Color Mode improves node visibility by coloring the title bar by Category, Precision, or custom colors.
- You can now set the precision of a Shader Graph and individual nodes.
- Added _TimeParameters variable containing Time, Sin(Time), Cosine(Time)

## [6.7.0] - 2019-05-21
### Added
- You can now use the `SHADERGRAPH_PREVIEW` keyword in `Custom Function Node` to generate different code for preview Shaders.

### Changed
- The `Custom Function Node` now uses an object field to reference its source when using `File` mode.
- To enable master nodes to generate correct motion vectors for time-based vertex modification, time is now implemented as an input to the graph rather than as a global uniform.

### Fixed
- `Sampler State` properties and nodes now serialize correctly.
- Labels in the Custom Port menu now use the correct coloring when using the Personal skin.
- Fixed an error that occured when using multiple Sampler State nodes with different parameters.
- When you convert nodes to a Sub Graph, the nodes are now placed correctly in the Sub Graph.
- When you perform an undo or redo to an inactive Shader Graph window, the window no longer breaks.
- When you rapidly perform an undo or redo, Shader Graph windows no longer break.
- Sub Graphs that contain references to non-existing Sub Graphs no longer break the Sub Graph Importer.
- You can now reference sub-assets such as Textures.
- Remaining outdated documentation has been removed. 
- You can no longer duplicate the Sub Graph Outputs node.
- When you create a new empty Sub Graph, it no longer shows a warning about a missing output.
- When you create outputs that start with a digit, Shader generation no longer fails.
- You can no longer add nodes that are not allowed into Sub Graphs.
- A graph must now always contain at least one Master Node.
- Duplicate output names are now allowed.
- Fixed an issue where the main preview was always redrawing.
- When you set a Master Node as active, the Main Preview now shows the correct result.
- When you click on the gear icon, Shader Graph now focuses on the selected node, and brings the settings menu to front view.
- Sub Graph Output and Custom Function Node now validate slot names, and display an appropriate error badge when needed.
- You can now reference Scene Color and Scene Depth correctly from within a Sub Graph.

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
