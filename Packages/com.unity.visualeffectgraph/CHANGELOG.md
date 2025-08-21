# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [17.0.3] - 2025-02-13

This version is compatible with Unity 6000.2.0a5.

### Changed
- Improved shader source generation performance.
- Improved VFX compilation time by avoiding redundant instanciation of implicit blocks.
- Added a missing button in the VFX template window to quickly install learning templates.
- Optimized the particle attribute layout for a smaller memory footprint.
- Reduced the main thread cost of `VFX.Update` by moving some transform-related operations to other threads.
- Performance optimization on the attributes manager.
- Enable threaded `RenderQueue` extraction for VFX renderers.
- Modified the code generation process to skip creating DXR-related code when ray tracing is not enabled in the output.
- Improved shader generation time by implementing a local include template cache.
- Enabled instancing support for VFX using GPU events.
- Added a missing button in the VFX template window to quickly install learning templates.

### Fixed
- Fixed unexpected compilation warning.
- Fixed incorrect capitalization Open VFX in the Visual Effect Graph package in the Package Manager.
- Fixed invalid cast exception when clicking on template window headers
- Fixed errors when deleting Integration Update Rotation and Trigger blocks.
- Fixed undoing slider value change would not be reflected in the float field next to it
- Fix VFX ray tracing shader errors when using flipbook or when not using the color attribute.
- Fixed VFX Graph template window was empty when the Terrain Tool package is installed
- Fix missing dirty calling SetTexture
- This PR fix resolves minor issues related to VFX graph content sample package.
- This PR fix resolves minor issues related to VFX graph content sample package.
- This PR fix resolves minor issues related to VFX graph content sample package.
- Fix VFX command culling when using Custom Passes in HDRP.
- Fix occasional crashes when modifying exposed properties when in paused play mode
- Unexpected visible particle if set alive is force to true in SG Opaque Output
- Fix behavior of TriggerOnDie block with implicit age reaping
- Don't allocate VFX system data in player with no renderer
- Incorrect sanitization of SetCustomAttribute when Random was different than Random.Off
- Missing delayed field for Sample Water Surface Operator.
- Unexpected log "Expression graph was marked as dirty after compiling context for UI" while using Custom HLSL based operators.
- Using the same name as a built-in attribute in a custom HLSL function's parameter would lead to a compilation error.
- Creating a Custom HLSL operator with two outputs could prevent the generated shader from compiling
- Fixed VFX particles GBuffer pass with URP Render Graph.
- Particle outputs connected to particle strip systems don't render last particle.
- Fixed an issue when importing old VFX asset in Unity6 using custom attribute with same name as built-in attribute.
- Fixed an issue subgraph blocks did not accept correct types of block based on their suitable context.
- Fixed an issue where CustomRenderTexture could not be used in VFX Graph object fields.
- Fixed a small cursor offset when drawing a rectangle selection.
- Fixed usage of FogNode always returning 1.0 in URP.
- Fixed an argument exception that used Arc Transform in blackboard.
- Fixed arc Shape properties in blackboard not applied in VFXGraph.
- Fixed an issue where reordering properties inside a category were not possible.  Also reordering a category could not work if there was properties at the root (with no category)
- Fixed port's label was not be visible when node is collapsed.
- Fixed an exception that could prevent opening a VFX in one specific case.
- Fixed a potential crash that could occur when adding the render node of sleeping systems.
- Fixed an issue where CustomHLSL was incorrectly marking parent assets as dirty.
- Fixed emissive decal when using color attribute for emissive.
- Fixed sprites in Texture Sheet Animation module in HDRP.
- Fixed an issue where direct material modification in VFXRenderer could lead to crashes.
- Fixed a potential memory-intensive strip buffer initialization by moving it to a compute dispatch.

- Fixed `NullReferenceExpection` happening with disconnected output contexts.
- Fix VFX ray tracing shader errors when using flipbook or when not using the color attribute.
- This PR fix resolves minor issues related to VFX graph content sample package.
- This PR fix resolves minor issues related to VFX graph content sample package.
- This PR fix resolves minor issues related to VFX graph content sample package.
- This PR fix resolves minor issues related to VFX graph content sample package.
- This PR fix resolves minor issues related to VFX graph content sample package.
- This PR fix resolves minor issues related to VFX graph content sample package.
- Fixed an issue where the VFX Graph template window appeared empty when the Terrain Tools package was installed.
- Fixed occasional crashes when modifying exposed properties when in paused play mode.
- Fixed errors that occurred when deleting Integration Update Rotation and Trigger blocks in the VFX Graph.
- Fixed HLSL blocks so that they accept parameters that don't have an `in` attribute.
- Fixed incorrect error message on Custom HLSL.
- Improved CustomHLSL to consider custom HLSL and includes workflow.
- Added support for more HLSL function prototype declaration.
- Resolved a corner case issue where the presence of multiple instances of the same buffer led to a compilation failure.
- A compilation issue occured when declaring a gradient in ShaderGraph blackboard.
- Fixed NullReferenceException when enabling Decal Layers in HDRP.
- Fixed exposed properties reset when editing multiple VFX at the same time on inspector.
- Fixed incorrect source spawnCount.
- Fixed exception when a category color is reset in the node search.
- Fixed a rare crash when destroying a VFX instance during rendering.
- Updated the starter template Description and some default VFX resources.
- Force culling when VFX rendering is disabled.
- VFX Graph VFXOutputEventHandlers Sample now compatible with Cinemachine 3.x.
- Disabled compile menu when authoring subgraphs.
- Fixed the ability to add blocks to subgraph context.
- Fixed some UI elements could overflow their reserved space.
- Fixed "int" type could not be parsed when the access modifier is not specified.
- Fixed unexpected CustomHLSL includes in neighbors contexts.
- Fixed potential exception message in the console when opening any VFX Graph.
- Fixed potential crash when using the Noise Module in a particle system.
- Fixed output properties in subgraphs had misplaced wire connector.
- Fixed a leak while spamming ReInit.
- Fixed compilation error when using the Six-way Lit Output with Adaptive Probe Volumes.
- Custom HLSL can be missing when connected to several contexts.
- Improved how the sleep state is updated for particle systems receiving GPU events.
- Wrong mesh rendered with instancing, when using multi mesh and exposed submesh mask.
- Fixed potential crash and correctness when using a system with multiple Volumetric Fog Outputs.
- Fixed SpawnIndex attribute when using instancing.
- Fixed an exception when trying to create curl noise sub-variant nodes.
- Fixed variadic attributes to not be allowed to be used in custom HLSL code.
- Fixed random texture rendered with instancing when using exposed texture set to None.
- Fixed a crash that occurred when visualizing a VFX preview with raytracing enabled.
- Fixed a potential division by zero in RayBoxIntersection code.
- Fixed an issue where copying or pasting in a different asset in a context with a block that used a custom attribute would lose the custom attribute type and fallback to float.


- Fixed missing drag area to change a value for inline float, uint and int operators.
- Fixed several UX issues in the VFX Graph blackboard.
- Fixed copy/pasting a selection from a graph to another when the selection contains multiple times the same property node.
- Fixed compatibility between Flipbook and Vector2 field types.
- Added error feedback in case of incorrect setup of the Position Sequential Circle block.
- Read unexposed shader global properties when using a Shader Graph output.
- Fixed ParticleIndexInStrip, StripIndex, and ParticleCountinStrip attributes when used in quad or mesh outputs (previously all returning 0).
- Fixed rendering unwanted particles when rendering particle strip systems as particles (previously rendering entire capacity).
- Fixed strips with immortal particles disappearing with instancing on.
- Fixed an issue where Convert Output to Output Particle ShaderGraph Octagon or Triangle generates an exception.
- Fixed corrupted graph when a custom type was missing.
- Fixed node search expand/collapse button, which could be blurry, depending on screen DPI setting.
- Fixed sticky note resizing could be broken.
- Fixed a crash that would uccur during the update of a Visual Effect when deleting a used Texture.
- Fixed an issue where strip tangent was not computed correctly when using Shader Graph output.
- Fixed two different HLSL parsing issues with VFX Graph custom HLSL.
- Fixed an issue where Tooltips were not displaying. 
- Fixed capacity field in the Particle System Info panel not being refreshed when modifying system capacity.
- Fixed overdraw debug mode of unlit particles in URP.
- Improved Water integration to prevent an unexpected error from dispatch.
- Incorrect sanitization of SetCustomAttribute when Random was different than Random.Off.
- Missing delayed field for Sample Water Surface Operator.
- Unexpected log "Expression graph was marked as dirty after compiling context for UI" while using Custom HLSL based operators.
- Particle outputs connected to particle strip systems don't render last particle.
- Fixed an issue when using the same name as a built-in attribute in a custom HLSL function's parameter would lead to a compilation error.
- Fixed an issue when creating a Custom HLSL operator with two outputs could prevent the generated shader from compiling.
- Fixed an issue with VFX particles GBuffer pass with URP Render Graph.
- Fixed an issue when importing old VFX asset in Unity6 using custom attribute with same name as built-in attribute.
- Subgraph blocks now accept correct types of block based on their suitable context.
- Fixed port's label was not be visible when node is collapsed.
- Fixed an exception that could prevent opening a VFX in one specific case.
- Fixed CustomRenderTexture could not be used in VFX Graph object fields.
- Fixed reordering properties inside a category was not possible anymore.
Also reordering a category could not work if there was properties at the root (with no category)
- Fixed a small cursor offset when drawing a rectangle selection
- This PR fix resolves minor issues related to VFX graph content sample package.
- This PR fix resolves minor issues related to VFX graph content sample package.
- This PR fix resolves minor issues related to VFX graph content sample package.
- Fix emissive decal when using color attribute for emissive.
- Fix NullReferenceExpection happening with disconnected output contexts.
- Fix occasional crashes when modifying exposed properties when in paused play mode
- Fixed VFX Graph template window was empty when the Terrain Tool package is installed

## [17.0.2] - 2024-04-02

This version is compatible with Unity 6000.0.0b15.

### Changed
- Improved compilation times with VFX Graph using Subgraphs.
- Improved the performance of VFX.ProcessCommandList by skipping the use of a RenderingCommandBuffer.
- Added selective VFX Graph buffers to lower the amount of buffer used in the shaders and increased platform reach.
- The Construct Matrix can now select between row and column. Added the Split Matrix operator.

### Fixed
- Instancing when gradient selection was based on a branch was wrong.
- Fixed very very long system name could lead to freeze the Editor.
- Removed SetDirty calls that triggered assertions in debug mode.
- Switch property binder from ExecuteInEditMode in ExecuteAlways.
- Fixed a PCache exporter issue to insure color in linear space.
- Fixed shadows being cast by Mesh Output when "cast shadows" was disabled (URP only).
- Fixed an issue where multithreaded camera expression were not allowed.
- Fixed a build failure on HDRP Linux using Sphere Output.
- Fixed missing particles with strip systems using instancing.
- Fixed some sanitation failures with new merged Position and Collision blocks.
- Unexpected material listing in animation window.
- Disable MeshToSDFBaker shaders on GLES3 to avoid warnings.
- Fixed incompatibility issue with HLSL 2021.
- Fixed an exception that could be raised when deleting all graph nodes in some specific cases.
- Fixed normal handling of odd-negative scales.
- Fixed an issue by cleaning data and upgraded VFX assets to prevent unwanted warnings in the console.
- Fixed Screen Space Size block in Shader Graph outputs.
- Fixed an editor crash when deleting objects (textures, meshes) that are used by active VFX graph.
- Fixed an issue that reduced FloatField to a height of 1px.
- Added tooltips to the VFX Control panel.
- Fixed an issue where VFX graph rendered the wrong mesh when using different exposed meshes with instancing enabled.
- When trying to connect incompatible types, the error popup was left over if the action was canceled with Escape key
- Fixed an issue that caused missing recompilation triggers that occurred when changing some operator settings
- Fixed an issue that caused an unexpected long enter in Play mode, and removed timeout exception.
- Removed the eye dropper from Node Search details panel.
- Fixed activation slot was hidden when a block was collapsed
- Removed multiple unexpected constraints on CustomHLSL functions
- Fix Construct Matrix operator serialization issue
- Correctly handle includes in CustomHLSL operator
- Initial Position Oriented Box with zero scale is restored.
- Position On Signed Distance field was failing to compile without direction.

## [17.0.1] - 2023-12-21

This version is compatible with Unity 2023.3.0b2.

### Changed

- Improved and optimized both undo and redo.
- Improved AddComponent performance for VFX by precaching script pointers in common case operation.
- Improved the collision system so it is more stable, robust, and energy conservative.
- Improved Position, Collision, and Kill blocks.
- Improved error feedback and added more error feedback.

### Fixed

- The behavior of the VFX graph toolbar button to open the template window has slightly changed
- Fixed unexpected inspector in case of Sprite Custom/Lit/Unlit.
- Fixed decal normal map handling of non-uniform scale.
- Fixed an issue where spawner callbacks were only working on the first instance of an instanced effect
- Understeministic skin mesh sampling when previous and current were fetch within the same VFX
- Updated of curve & gradient were missing when edited directly in VFX View Window.
- Fixed undoing port value change that did not restore correct value.
- Fixed undo/redo did not work anymore with selection.
- Fixed property not visible in blackboard with creating using ALT+Drag shortcut.
- Improved error feedback message when a shader graph is missing and no path is found.
- Fixed an issue where the old style dropdown in Add, Divide (and many others) style had not been updated to new design.
- Fixed custom attribute broke compilation if name was starting with a capital letter.
- Fixed shader compilation error when using a Custom HLSL node.
- Avoid names which doesn't fit in node search window.
- Logical operators And (logical) and And (Bitwise) could be mixed up in the node search window when search for "and" (same for Or operator).
- Blackboard menu entries are better human readable.
- Fixed issue with null value in slots preventing it to be changed.
- Fixed error feedback context menu could not be displayed anymore.
- Fixed RenderTexture could not be used anymore in VFXGraph.
- We could not change the selected gizmo for nodes with multiple gizmos (for instance: Set Position Sequential Line).
- VFX Graph: Fixed gizmo overlay's drop down was cut at the bottom.
- Unexpected JSON error while using ShaderGraph.
- ShaderGraph keyword are now correctly supported in VFX Graph Output.
- Fixed space conversion error when copy-pasting a system.
- Trigger over distance now takes into account change in position (not just velocity).
- Visual Effects back in Scene FX window.
- Fixed strip output could not be created anymore.
- Indeterminate state object fields were hidden instead of greyed out.
- Fixed copy/paste a parameter node was also duplicating the parameter itself.
- Fixed editor freeze when selecting all properties/custom attributes from the blackboard
- Custom HLSL used in ShaderGraph Output
- Sample of Camera Buffer isn't available in compute passes
- Remove warning from VFX init for strips (GetParticleIndex)
- Fixed unexpected warning message in the console
- Fixed node search window could not be opened through the context menu

## [17.0.0] - 2023-09-26

This version is compatible with Unity 2023.3.0a8.

### Fixed

- Fixed an issue where the "materials" and "sharedMaterials" properties returned all materials instead of returning only the active materials.
- Fixed an issue where an event at frame zero in Timeline were not handled correctly.
- Fixed an issue with the wrong size used for updates in strips with immortal particles.
- Fixed Six-way Shader Graph sub target when using more than one SRP in a project.
- Unexpected generated shader with invalid ShaderGraph.
- Keep the built-in templates category always visible when there's no user defined category.
- Template items could have uneven width when the left panel is too small.
- The template item hit box was slightly bigger than its visual representation, now it perfectly match.
- The right panel (details panel) could be resize to as small as zero width, leading to messy layout. Now it has a minimum width of 200px (like the left panel).
- Fixed a performance issue with parameters gizmos.
- Fixed missing gizmo for Position exposed property
- Fixed error log raised by VFX analytics while building a project.
- Fixed unexpected behavior while switching to AfterPostProcess with ShaderGraph.
- Fixed gizmo overlay's drop down was cut at the bottom

## [16.0.3] - 2023-07-04

This version is compatible with Unity 2023.3.0a1.

### Fixed

- Initialize VFX material indices to make all materials valid if used on Awake
- Fix HDRP Decal Output when system is in world space
- Fixed nested curly braces not supported in custom hlsl code
- Fix VFX camera command culling failling when all effects are out of frustum

## [16.0.2] - 2023-06-28

This version is compatible with Unity 2023.2.0a22.

### Fixed

- Fixed a loss of Material Settings when switching between two SRP.
- Fixed error when trying to open a VFX asset without using an SRP. Note that this does not make VFX Graph supported on BiRP.
- Fixed a case where more than one **No Asset** window could be opened.
- Fixed the unexpected listing of a Scene object in the object picker from VFX Graph.
- Fixed immortal particles so they work properly when instancing is enabled.
- Fixed an exception while removing a clip event in the Timeline Inspector window.
- Fixed flickering and glitches when using Volumetric Fog Output on Metal devices.
- Removed lock capabilities while editing material stored in VisualEffectAsset.
- Fixes ray tracing shader passes when using Shader Graph and Ray Tracing
- Fix crash when changing to custom batch capacity in computers with large GPU memory
- Prevent unexpected border highlight after clicking on VFX toolbar button
- Fixed several small issues related to the new VFX Template window
- Crash when converting to subgraph block
- Exception while convert to subgraph with Range of Parameter
- Subgraph creation doesn't keep activation slots link
- Crash while sampling combined or deleted mesh with SampleMesh

## [16.0.1] - 2023-05-23

This version is compatible with Unity 2023.2.0a17.

### Changed

- Optimized `VFX.Update` per component overhead.
- Enabled VFX systems receiving GPU events to now enter sleep state.

### Fixed

- Fixed the broken documentation link for VFX Graph nodes (when documentation is available).
- Fixed Motion Vector so it is now correct when transform isn't changed every frame.
- Improved error feedback in case of missing reference in custom spawner.
- Removed Loop And Delay block listing in favor of Spawn Context Settings (which can be accessed through the Inspector).
- Fixed shader errors when building with sphere or cube outputs.
- Fixed shader graph with VFX compatibility were not reimported if imported before VFX package is installed.
- Fixed a crash when logging an error messages for unexpected buffers.
- Fixed Opacity Channel "Metallic Map Blue" for VFX URP Decals.
- Fixed an unexpected asset database error while importing VFX and ShaderGraph dependency.
- Fixed the wrong evaluation of time in VFX Control Track while using Playables API.
- Enabled the lighting debug to provide the ambient occlusion contribution on Unlit.
- Enabled integrating the debug view for VertexDensity and QuadOverdraw.
- Fixed data serialization that could lead to corrupted state.
- Fixed a memory leak in the Unity Editor with VFX Graph objects.
- Fix OutputUpdate warnings about spaces after end of line
- Removed an error message when a point cache asset is missing, and replaced it with error feedback.
- Fixed flickering with Volumetric Fog Output.
- Fix strips tangents and buffer type with Shader Graph
- Fix potential infinite loop when opening VFX Graph due to space issue

## [16.0.0] - 2023-03-22

This version is compatible with Unity 2023.2.0a9.

### Changed

- Reduced the import cost of VFX Graph objects, especially when importing many objects at once.

### Fixed

- Added extra memory to allow external threads to steal VFX update jobs.
- Fixed the range not being applied in the UI when setting up a value out of the allowed range.
- Fixed minor issues with Cube and Sphere particle outputs.
- Fixed a crash when loading a subscene with VFX in DOTS.
- Enabled correct generation of the interpolator modifiers for packed structure in HDRP Shader Graph.
- Enabled minimizing the generated interpolator count with VFX Shader Graph to improve its performance and avoid reaching the limit.
- Fixed mesh LOD flickering when using TAA.
- Fixed mismatching LOD between eyes in multi-pass VR.
- Restored missing tooltips.
- Re-enabled Volumetric Test in XR.
- Fixed the `Dispose()` method of `MeshToSDFBaker` leading to memory leaks
- Fixed an unexpected motion vector when adding precompute velocity that was enabled in Shader Graph.
- Fixed unexpected per frame garbage while using Timeline.
- Fixed a crash when removing VFXRenderer from a disabled GameObject.
- Enabled the exposure weight slider to be hidden when a shadergraph was assigned to an output context.
- Fixed an error in the console when clicking on the [+] button in the blackboard in the "No Asset" window.
- Fixed errors in the console when undoing changes from gizmo in some specific conditions.
- Fixed panning and zooming a VFX Graph was synchronized between all opened tabs. Also when multiple VFX Graph tabs are opened they are now properly restored after Unity is restarted.
- Enabled the option to filter out DXR and META passes from SG generated shaders.
- Forced positive color values in the graph UI.
- Fixed incorrect MotionVectors when using multiple camera or multi pass stereo.
- Fixed incorrect MotionVectors in XR with Stereo Instancing.
- Enabled taking user's preference for the Search Window mode into account for object fields in VFX Graph (classic / advanced).
- Enabled hiding **Sorting mode** and **Revert sorting** when the blend mode is set to Opaque.
- Enabled hiding the log message asking to check the asset for version control in an empty VFX window, when resetting Editor Layout.
- Enabled keeping the bottom margin on blocks when collapsed.

## [15.0.3] - 2022-12-02

This version is compatible with Unity 2023.2.0a1.

### Fixed

- Fixed wrong particle count if read before first readback.
- Fixed subgraph edition causing error **An infinite import loop has been detected.** while saving.
- Enabled renamed blackboard categories that have been duplicated to stay on screen.
- Removed blackboard category with only spaces in the name.
- Fixed the VFX compute shader so it now compiles when the name of a custom attribute contains a space.
- Fixed a rare issue with VFXCullResults.
- Fixed the play / pause button in the VFX Graph control panel so it now switches the icon depending on the current state.
- Removed exception when more than 5 flow inputs are exposed in subgraph.
- Fixed an issue with the out of range exception on GPU when multiple spawn context are plugged to the same initialize system.
- Prevented overflow on baked curve and gradient.
- Enabled easier usage of the toggle Support VFX Graph instead to avoid confusing creation of VFX Target.
- Fixed a crash when drag & dropping a VFX on another VFX with a circular dependency.

## [15.0.2] - 2022-11-04

This version is compatible with Unity 2023.1.0a23.

### Changed

- Reduced the time taken by VFXGraph.CheckCompilationVersion that would previously potentially query all assets on every domain reload.

### Fixed

- Fixed position where VFX are created when VFX asset is dragged to Scene View.
- Fixed an issue where the output mesh with default shader was incorrectly sorted before the HDRP fog by Replacing default mesh output shader to be SRP compatible.
- Fixed an unexpected compilation failure with URP Lit Output.
- Fixed an issue to avoid unnessary allocations in the SDF Baker by using Mesh Buffer API.
- Fixed an issue that the VFX Graph documentation link was always pointing to the latest LTS version instead of current package version.
- Fixed an issue that read alive from source attribute was always returning true.
- Fixed single pass stereo rendering issue with SG integration due to uncorrectly setup instanceID.
- Fixed an issue that VisualEffect spawned behind the camera were always updated until visible and culled.
- Fixed Bounds helper compilation error.
- Forbid drag and drop of material from project browser to VFX component in scene.
- Fixed compilation time increase due to DXR passes.
- Fixed unexpected unrecognized identifier 'GraphValues' while using SG.
- Fixed an issue that values modified in spawner or init context automatically trigger a reinit of the attached VFX component.
- Fixed compilation error when using sorting in World space.
- Fixed an issue that vertex Color was black while using new shader graph integration on planar primitive output.
- Enabled specifying the maximum point count in Attribute from Map blocks.
- Fixed exceptions for SystemNames when leaving play mode if a new system had been added without saving.
- Added a Visual Effect Graph to a scene that did not take the default parent into account.
- Improved dragging and dropping of blocks when you want to change their order or move them to another context.
- Re-enabled multithreaded VFX Update on console platforms.
- Fixed build errors with VFX-DXR.
- Fixed the **Preserve Specular Lighting** mode on non-Shader Graph lit outputs.
- Updated non-deterministic test in VFX_HDRP: InstancingBatch.
- Fixed issues with light probes and instancing.
- Fixed compilation errors with large graphs.
- Fixed unpredictable behavior in spawners using instancing with more than one instance.
- Fixed an exception while using Unlit ShaderGraph with VFX.
- Fixed internal issue with Editor test and inspector rendering.
- Improved shader input properties synchronization with VFX Graph output context when the shader is deleted or set to None.
- Fixed some VFX Graphs that were not compiled until the asset was opened.
- Fixed VFX Graph so that when a ShaderGraph exposed property is renamed, and the shader graph is saved, the corresponding VFXGraph output context property is now renamed properly.
- Fixed undo so it now works with shader property in the Mesh Output context.
- Fixed ShaderGraph so that changes are now saved in the Mesh Output shader property when saving.
- Updated non-deterministic InstancingBatch test in VFX_URP.
- Removed unexpected GC.Alloc while accessing to `state.vfxEventAttribute` in [VFXSpawnerCallbacks](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerCallbacks.OnUpdate.html).
- Fixed the Property Binder so it now takes the space property into account.

## [15.0.1] - 2022-08-04

This version is compatible with Unity 2023.1.0a19.

### Changed

- Reduced time taken by code generation when a VFX asset is imported.

### Fixed

- Fixed an issue when motion vector is applied on line using `targetOffset`, the VFXLoadParameter was missing.
- Fixed NRE when the Vector2 is configured as a range, it was preventing Decal output context creation.
- Fixed and unexpected lossy scale evaluation issue on GPU verses CPU where it's correct.
- Fixed an issue where Position ArcSphere was failing with BlendDirection.
- Fixed an isse where the mirrored curve presets to match Shuriken curve editor was missing.
- Fixed an issue were Alpha Clipping have unexpected behavior in editor when used in MaterialOverride with SG integration, .
- Error thrown when entering a subgraph that is already opened.
- Unexpected memory allocation in inspector preview when interacting with mouse while in pause.
- Make collision with SDF more robust to bad inputs.
- Fixed an issue where VFX shadows were rendering when VFX was disabled in Scene View visibility menu.

## [15.0.0] - 2022-06-13

This version is compatible with Unity 2023.1.0a6.

### Changed

- Sticky notes are no longer lost when you convert to block subgraph.
- Made the input property label colors consistent.

### Fixed

- Fixed unexpected assert when capacity is really high.
- Fixed delayed property changes so they apply when you save.
- Fixed resetting of needsComputeBounds.
- Fixed so that output order changes in the inspector take effect even if the asset is not opened in VFX Graph editor.
- Fixed the timeline behavior when wrapmode is set to loop in director.
- Unexpected lossy scale evaluation on GPU.
- Fixed an issue with Motion Vector target offset with Line Output.
- Fixed an issue that caused an unexpected compilation failure with URP Lit Output.
- Fixed an unexpected memory allocation in inspector preview when interacting with mouse while in pause.
- Stop rendering VFX shadows when VFX are disabled in Scene View visibility menu.
- Updated non-deterministic test: InstancingBatch.
- Fixed robustness issues with Collision with SDF.
- Exceptions about SystemNames were raised when leaving play mode if a new system had been added without saving.
- Fixed crash when loading a subscene with VFX in DOTS
- Added a Visual Effect Graph to the scene did not take the default parent into account.

## [14.0.3] - 2021-05-09

### Added

- New Timeline Integration which supports scrubbing.
- Samples project github link button in package manager.

### Changed

- Fixed the OutputParticle context inspector content so it doesn't shift vertically when you resize the inspector panel.
- Fixed so that the context name isn't lost when you convert to a different type.
- Added a missing range slider for the blend property to the custom attribute blend block.
- Fixed so the space property is carried over when copying/pasting a VFX property.

### Fixed

- Fixed possible NaNs in Vortex Subgraph node.
- Improved node position when you create a node by dragging an edge.
- Fixed an exception when setting when changing the space of a shape to world.
- Displayed context labels in the inspector with all outputs.
- Made the shader graph **exposed properties** order consistent with the shader graph blackboard in the `Output Particle` blocks.
- Fixed Picking and Selection passes.
- Reduced GC.Allocs in the SceneViewGUICallback.
- HDRP Decals are not in experimental.
- Fixed Motion vectors in XR.
- Fixed Undo/Redo with Prefabs.
- Fixed node input type so it doesn't change when you insert a new node on an edge.
- Fixed so that when you duplicate Event Array elements, it creates linked instances of the elements.
- Fixed the mixing of Vector4 & Color with SampleGraphicsBuffer within the same graph.
- Fixed material inspector so it displays in outputs with shader graph.
- Fixed a crash in DX12 that potentially affected other platforms when the GPU events systems had an incorrect order.
- The VFX Graph gizmo can't be manipulated.
- The VFX asset preview isn't animated by default anymore to save CPU usage.

## [14.0.2] - 2021-02-04

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [14.0.1] - 2021-12-07

### Fixed

- Creating a new VFX of the same name as an already opened VFX will reuse the existing window [Case 1382841](https://issuetracker.unity3d.com/product/unity/issues/guid/1382841/)
- Incorrect behavior of Tangent Space in ShaderGraph [Case 1363279](https://issuetracker.unity3d.com/product/unity/issues/guid/1363279/)
- ShaderGraph made with new VFX SG integration where not listed when searching for a shader graph output [Case 1379523](https://issuetracker.unity3d.com/product/unity/issues/guid/1379523/)
- Enable/disable state of VFX blocks and operators are preserved after copy/paste
- Blackboard "Add" button for output could be hidden when the panel is too small (https://issuetracker.unity3d.com/product/unity/issues/guid/1389927/)
- Forbid pasting a subgraph in the same subgraph [Case 1364480](https://issuetracker.unity3d.com/product/unity/issues/guid/1364480/)
- VFX Subgraph operator and block windows now have dedicated icons
- Some operators were missing in node search window (gradient for instance)
- Allows for attribute-less systems. [Case 1341789](https://issuetracker.unity3d.com/product/unity/issues/guid/1341789/)
- Editing the values in the graph did not impact the system in real-time after saving [Case 1371089](https://issuetracker.unity3d.com/product/unity/issues/guid/1371089/)
- Fixed null reference exception when opening another VFX and a debug mode is enabled [Case 1347420](https://issuetracker.unity3d.com/product/unity/issues/guid/1347420/)
- Sticky note title keeps the selected font sizewhen being edited
- Collision with zero scale lead to undefined behavior [Case 1381562](https://issuetracker.unity3d.com/product/unity/issues/guid/1381562/)
- Fixed GPU event particle init after restarting VisualEffect [Case 1378335](https://issuetracker.unity3d.com/product/unity/issues/guid/1378335/)
- No more exception raised when selecting all nodes with CTRL+A and then deleting them
- Particle Strip without lifetime do not die when Alive is set to false. [Case 1376278](https://issuetracker.unity3d.com/product/unity/issues/guid/1376278/)
- Resize custom operator (multiply, add...) to the minimum size when changing input types
- Show opened VFX asset in inspector when clicking in a void area and unselect node when VFX graph loose focus
- Disabled text inputs were unreadable [Case 1387237](https://issuetracker.unity3d.com/product/unity/issues/guid/1387237/)
- Folder named with a ".vfx" extension could lead to an error on macOS [case 1385206](https://issuetracker.unity3d.com/product/unity/issues/guid/1385206/)

## [14.0.0] - 2021-11-17

### Fixed

- Gradient field doesn't support HDR values [Case 1381867](https://issuetracker.unity3d.com/product/unity/issues/guid/1381867/)
- Allows for attribute-less systems. [Case 1341789](https://issuetracker.unity3d.com/product/unity/issues/guid/1341789/)
- Editing the values in the graph did not impact the system in real-time after saving [Case 1371089](https://issuetracker.unity3d.com/product/unity/issues/guid/1371089/)
- Fixed null reference exception when opening another VFX and a debug mode is enabled [Case 1347420](https://issuetracker.unity3d.com/product/unity/issues/guid/1347420/)

## [13.1.2] - 2021-11-05

### Fixed

- Removed extra nodes in Ribbon template. [Case 1355602](https://issuetracker.unity3d.com/product/unity/issues/guid/1355602/)

## [13.1.1] - 2021-10-04

### Added

- Multiple VFX graphs can be opened at the same time

### Changed

- Search window now lists more nodes variants and they are organized by attribute first instead of operation

### Fixed

- Compilation error while using not exposed texture in ShaderGraph [Case 1367167](https://issuetracker.unity3d.com/product/unity/issues/guid/1367167/)
- Texture picker lists only textures with expected dimensions (2D, 3D, Cubemap)
- Fix SDF Baker fail on PS4 & PS5 [Case 1351595](https://fogbugz.unity3d.com/f/cases/1351595/)
- Particles were rendered pink with some debug modes [Case 1342276](https://issuetracker.unity3d.com/product/unity/issues/guid/1342276/)
- Removed bool from the built-in list of blittable types for GraphicsBuffer [Case 1351830](https://issuetracker.unity3d.com/product/unity/issues/guid/1351830/)
- Extract position from a transform is wrong on GPU [Case 1353533](https://issuetracker.unity3d.com/product/unity/issues/guid/1353533/)
- Fix potentially invalid value for pixel dimensions in HDRPCameraBinder
- Exposed Camera property fails to upgrade and is converted to a float type [Case 1357685](https://issuetracker.unity3d.com/product/unity/issues/guid/1357685/)
- Unexpected possible connection between GPUEvent and Spawn context [Case 1362739](https://issuetracker.unity3d.com/product/unity/issues/guid/1362739/)
- Fixed Collision with Depth when using a physical camera. [Case 1344733](https://issuetracker.unity3d.com/product/unity/issues/guid/1344733/)
- Fix bounds helper tool (automatic systems culling, world bounds computation, ...)

## [13.1.0] - 2021-09-24

### Fixed

- Rename "Material Offset" to "Sorting Priority" in output render state settings [Case 1365257](https://issuetracker.unity3d.com/product/unity/issues/guid/1365257/)

## [13.0.0] - 2021-09-01

### Added

- New options to select how to sort particles in the Output Context.

### Fixed

- Prevent vector truncation error in HDRP Decal template
- Fix potential infinite compilation when using subgraphs [Case 1346576](https://issuetracker.unity3d.com/product/unity/issues/guid/1346576/)
- Prevent out of sync serialization of VFX assets that could cause the asset to be dirtied without reason
- Fix undetermitism in space with LocalToWorld and WorldToLocal operators [Case 1355820](https://issuetracker.unity3d.com/product/unity/issues/guid/1355820/)
- Unexpected compilation error while modifying ShaderGraph exposed properties [Case 1361601](https://issuetracker.unity3d.com/product/unity/issues/guid/1361601/)
- Compilation issue while using new SG integration and SampleTexture/SampleMesh [Case 1359391](https://issuetracker.unity3d.com/product/unity/issues/guid/1359391/)
- Added a missing paste option in the context menu for VFX contexts. Also the paste options is now disabled when uneffective
- Prevent VFX Graph compilation each time a property's min/max value is changed
- Prevent vfx re-compilation in some cases when a value has not changed
- Eye dropper in the color fields kept updating after pressing the Esc key
- Automatically offset contexts when a new node is inserted to avoid overlapping

## [12.0.0] - 2021-01-11

### Added

- Added support for Texture2D Arrays in Flipbooks
- Added new setting in "Preferences -> Visual Effects" to control the fallback behavior of camera buffers from MainCamera node when the main camera is not rendered.
- Sample vertices of a transformed skinned mesh with Position (Skinned Mesh) and Sample Skinned Mesh operator.
- Placement option (Vertex, Edge, Surface) in Sample Mesh & Skinned Mesh, allows triangle sampling.
- Material Offset setting in inspector of the rendered outputs.
- Restore "Exact Fixed Time Step" option on VisualEffectAsset.
- Support 2D Renderer in URP for Unlit.
- New tool to help set VFX Bounds
- New tool : Signed Distance Field baker.
- Provide explicit access to spawnCount in graph
- Support of direct link event to initialize context (which support several event within the same frame)
- Structured Graphics Buffer support as exposed type
- Added HDRP Decal output context.
- Motion vectors enabled for particle strips
- Added Is Inside subgraph into VFX Graph additions package
- The VFX editor automatically attach to the current selection if the selected gameobject uses the currently edited VFX asset
- Two new buttons are available in the editor's tool bar. One will display a popup panel to handle attachement and one to lock/unlock the current attachement
- Improved toolbar design: added icons, removed labels and grouped commands into dropdown menus

### Changed

- Allow remaking an existing link.
- Sphere and Cube outputs are now experimental
- Property Binder : Handle Remove Component removing linked hidden scriptable objectfields
- Property Binder : Prevent multiple VFXPropertyBinder within the same game object
- Transform integrated to VFXTypes : Circle, ArcCircle, Sphere, ArcSphere, Torus, ArcTorus, Cone, ArcCone

### Fixed

- VFXEventBinderBase throwing a null reference exception in runtime
- Unexpected compilation warning in VFXMouseBinder [Case 1313003](https://issuetracker.unity3d.com/product/unity/issues/guid/1313003/)
- Prevent creation of subgraph containing only partial systems [Case 1284053](https://issuetracker.unity3d.com/product/unity/issues/guid/1284053/)
- Prevent pasting context within operator/block subgraph [Case 1235269](https://issuetracker.unity3d.com/product/unity/issues/guid/1235269/)
- VFXEventBinderBase throwing a null reference exception in runtime
- Fix CameraFade for shadow maps [Case 1294073](https://fogbugz.unity3d.com/f/cases/1294073/)
- Modified Sign operator node output for float when input is 0.0f [Case 1299922](https://fogbugz.unity3d.com/f/cases/1299922/)
- An existing link can be remade.
- Use alphabetical order in type list in blackboard "+" button [Case 1304109](https://issuetracker.unity3d.com/product/unity/issues/guid/1304109/)
- Consistently displays the Age Particles checkbox in Update context [Case 1221557](https://issuetracker.unity3d.com/product/unity/issues/guid/1221557/)
- Fix compute culling compilation in URP [Case 1309174](https://fogbugz.unity3d.com/f/cases/1309174/)
- pCache: Unexpected ditable field in Mesh Statistics, Save & Cancel pCache, error trying to access not readable texture [Case 1122417](https://issuetracker.unity3d.com/product/unity/issues/guid/1122417/)
- Handle correctly locked VisualEffectAsset with version control system [Case 1261051](https://issuetracker.unity3d.com/product/unity/issues/guid/1261051/)
- Artefact in VFXView using efficient debug mode in component target board [Case 1243947](https://issuetracker.unity3d.com/product/unity/issues/guid/1243947/)
- Sample Mesh Color when value is stored as float.
- Compilation error due to direct access to GetWorldToObjectMatrix instead of VFXGetWorldToObjectMatrix [Case 1308481](https://issuetracker.unity3d.com/product/unity/issues/guid/1308481/)
- Prevent infinite compilation loop [Case 1298466](https://issuetracker.unity3d.com/product/unity/issues/guid/1298466/)
- Remove some useless compilation triggers (modifying not connected or disabled nodes for instance)
- Tidy up of platform abstraction code for random number generation, requires a dependency on com.unity.render-pipelines.core for those abstractions.
- Fixed shader compilation errors with textures in shader graph [Case 1309219](https://issuetracker.unity3d.com/product/unity/issues/guid/1309219/)
- Fixed issue with VFX using incorrect buffer type for strip data
- Safe Normalization of Cross Products in Orient blocks [Case 1272724](https://issuetracker.unity3d.com/product/unity/issues/guid/1272724)
- Property Binder : Undo after reset [Case 1293794](https://issuetracker.unity3d.com/product/unity/issues/guid/1293794/)
- Property Binder : Allow copy/past from a game object to another
- Deleting a context node and a block while both are selected throws a null ref exception. [Case 315578](https://issuetracker.unity3d.com/product/unity/issues/guid/1315578/)
- Target GameObject attach button does not allow attaching a valid VFX if the last selection was invalid. [Case 1312178](https://issuetracker.unity3d.com/product/unity/issues/guid/1312178/)
- Deleting flow edge between Init and Update throw an invalid opeation exception [Case 1315593](https://issuetracker.unity3d.com/product/unity/issues/guid/1315593/)
- Regression with some settings not always triggering a recompilation [Case 1322844](https://issuetracker.unity3d.com/product/unity/issues/guid/1322844/)
- Having more than five GPU Event output leads to "unexpected token 'if" at compilation [Case 1323434](https://issuetracker.unity3d.com/product/unity/issues/guid/1323434/)
- Deleted properties still show up in the inspector [Case 1320952](https://issuetracker.unity3d.com/product/unity/issues/guid/1320952/)
- Exception in VFXFilterWindow if search field is empty [Case 1235269](https://issuetracker.unity3d.com/product/unity/issues/guid/1235269/)
- Fixed null reference exception when exposing Camera type in VFX graph [Case 1315582](https://issuetracker.unity3d.com/product/unity/issues/guid/1315582/)
- Fixed VFX with output mesh being always reimported [Case 1309753](https://issuetracker.unity3d.com/product/unity/issues/guid/1309753/)
- Modified state in the VFX tab has now a correct state
- Motion Vector map sampling for flipbooks were not using correct mips
- Remove unexpected expression in spawn context evaluation [Case 1318412](https://issuetracker.unity3d.com/product/unity/issues/guid/1318412/)
- Fix unexpected Spawn context execution ordering
- Fix incorrect buffer type for strips
- Enabled an optimization for motion vectors, storing projected positions for vertices instead of the transform matrix
- In the Gradient editor undo will now properly refresh the gradient preview (color swatches)
- Eye dropper in the color fields kept updating after pressing the Esc key
- Sticky notes can now be deleted through contextual manual menu
- Blackboard fields can now be duplicated either with a shortcut (Ctrl+D) or with a contextual menu option
- Properties labels do not overlap anymore
- VFX Graph operators keep the same width when expanded or collpased so that the button does not change position
- Fix Soft Particle depth computation when using an orthographic camera [Case 1309961](https://issuetracker.unity3d.com/product/unity/issues/guid/1309961)
- When adding a new node/operator in the graph editor and using the search field, the search results are sorted in a smarter way
- Unexpected operator and block removal during migration [Case 1344645](https://issuetracker.unity3d.com/product/unity/issues/guid/1344645/)
- Inspector group headers now have a better indentation and alignment
- Zoom and warning icons were blurry in the "Play Controls" and "Visual Effect Model" scene overlays
- Random crash using subgraph [Case 1345426](https://issuetracker.unity3d.com/product/unity/issues/guid/1345426/)
- Fixed Collision with Depth Buffer when using Orthographic camera [Case 1309958](https://issuetracker.unity3d.com/product/unity/issues/guid/1309958/)
- Fix culling of point output [Case 1225764](https://issuetracker.unity3d.com/product/unity/issues/guid/1225764/)
- Compilation issue when normal is used in shadergraph for opacity with unlit output
- Fix Exception on trying to invert a degenerate TRS matrix [Case 1307068](https://issuetracker.unity3d.com/product/unity/issues/guid/1307068/)
- Fix IsFrontFace shader graph node for VFX.
- Fix crash when loading SDF Baker settings holding a mesh prefab [Case 1343898](https://issuetracker.unity3d.com/product/unity/issues/guid/1343898/)
- Exception using gizmo on exposed properties [Case 1340818](https://issuetracker.unity3d.com/product/unity/issues/guid/1340818/)
- GPU hang on some initialize dispatch during dichotomy (platform specific)
- Compilation error undeclared identifier 'Infinity' [Case 1328592](https://issuetracker.unity3d.com/product/unity/issues/guid/1328592/)
- Exposed Parameter placement can be moved after sanitize
- Fix rendering artifacts on some mobile devices [Case 1149057](https://issuetracker.unity3d.com/product/unity/issues/guid/1149057/)
- Fix compilation failure on OpenGLES [Case 1348666](https://issuetracker.unity3d.com/product/unity/issues/guid/1348666/)
- Don't open an empty VFX Graph Editor when assigning a VFX Asset to a Visual Effect GameObject from the inspector [Case 1347399](https://issuetracker.unity3d.com/product/unity/issues/guid/1347399/)
- Visual Effect inspector input fields don't lose focus anymore while typing (Random seed)
- Subgraph output properties tooltips were not easily editable when multiline

## [11.0.0] - 2020-10-21

### Added

- Added new setting to output nodes to exclude from TAA
- New Sample Point cache & Sample Attribute map operators

### Changed

- Changed the "Edit" button so it becomes "New" when no asset is set on a Visual Effect component, in order to save a new visual effect graph asset.

### Fixed

- Forbid incorrect link between incompatible context [Case 1269756](https://issuetracker.unity3d.com/product/unity/issues/guid/1269756/)
- Serialization issue with VFXSpawnerCallbacks
- Unexpected exception while trying to display capacity warning [Case 1294180](https://issuetracker.unity3d.com/product/unity/issues/guid/1294180/)
- Exclude Operator, Context, Block and Subgraph from Preset [Case 1232309](https://issuetracker.unity3d.com/product/unity/issues/guid/1232309/)
- Fix [Case 1212002](https://fogbugz.unity3d.com/f/cases/1212002/)
- Fix [Case 1223747](https://fogbugz.unity3d.com/f/cases/1223747/)
- Fix [Case 1290493](https://fogbugz.unity3d.com/f/cases/1290493/#BugEvent.1072735759)
- Incorrect path on Linux while targetting Android, IOS or WebGL [Case 1279750](https://issuetracker.unity3d.com/product/unity/issues/guid/1279750/)

## [10.2.0] - 2020-10-19

### Added

- Warning using Depth Collision on unsupported scriptable render pipeline.
- Warning in renderer inspector using Light Probe Proxy Volume when this feature isn't available.
- New operator : Sample Signed distance field
- New Position on Signed Distance Field block
- Added command to delete unuser parameters.
- Harmonized position, direction and velocity composition modes for position (shape, sequential, depth) and Velocity from Direction & Speed blocks
- New particle strip attribute in Initialize: spawnIndexInStrip
- Added Get Strip Index subgraph utility operator in Additional Samples
- Added Encompass (Point) subgraph utility operator in Additional Samples

### Fixed

- "Create new VisualEffect Graph" creates a graph from the default template [Case 1279999](https://fogbugz.unity3d.com/f/cases/1279999/)
- Fix [Case 1268977](https://issuetracker.unity3d.com/product/unity/issues/guid/1268977/)
- Fix [Case 1114281](https://fogbugz.unity3d.com/f/cases/1114281/)
- Forbid creation of context in VisualEffectSubgraphBlock through edge dropping. No context should be allowed.
- Fix [Case 1199540](https://issuetracker.unity3d.com/product/unity/issues/guid/1199540/)
- Fix [Case 1219072](https://issuetracker.unity3d.com/product/unity/issues/guid/1219072/)
- Fix [Case 1211372](https://issuetracker.unity3d.com/product/unity/issues/guid/1211372/)
- Fix [Case 1262961](https://issuetracker.unity3d.com/product/unity/issues/guid/1262961/)
- Fix [Case 1268354](https://fogbugz.unity3d.com/f/cases/1268354/)
- Fix VFX Graph window invalidating existing Undo.undoRedoPerformed delegates.
- Fix for VisualEffect prefab override window [Case 1242693](https://issuetracker.unity3d.com/product/unity/issues/guid/1242693/)
- Fix [Case 1281861](https://issuetracker.unity3d.com/product/unity/issues/guid/1281861/)
- Unexpected exception while installing samples inside an URP project [Case 1280065](https://issuetracker.unity3d.com/product/unity/issues/guid/1280065/)
- Fix edited operator being collapsed [Case 1270517](https://issuetracker.unity3d.com/product/unity/issues/guid/1270517/)
- Filters out renderer priority on SRP which doesn't support this feature.
- Fallback to builtIn rendering layer if srpAsset.renderingLayerMaskNames returns null.
- Fix missing prepass in URP [Case 1169487](https://issuetracker.unity3d.com/product/unity/issues/guid/1169487/)
- Fix SubPixelAA block while rendering directly in backbuffer.
- Property Binder : Incorrect Destroy called from edit mode. [Case 1274790](https://issuetracker.unity3d.com/product/unity/issues/guid/1274790/)
- Property Binder : Unexpected null reference exception while using terrain binder. [Case 1247230](https://issuetracker.unity3d.com/product/unity/issues/guid/1247230/)
- Property Binder : HierarchyRoot null reference exception while using Hierarchy to Attribute Map. [Case 1274788](https://issuetracker.unity3d.com/product/unity/issues/guid/1274788/)
- Property Binder : Properties window isn't always up to date. [Case 1248711](https://issuetracker.unity3d.com/product/unity/issues/guid/1248711/)
- Property Binder : Avoid Warning while building on Mobile "Presence of such handlers might impact performance on handheld devices." when building for Android" [Case 1279471](https://issuetracker.unity3d.com/product/unity/issues/guid/1248711/)
- Fixed [case 1283315](https://issuetracker.unity3d.com/product/unity/issues/guid/1283315/)
- Addressing for mirror and clamp modes in sequential operators and blocks
- Incorrect volume spawning for Sphere & Circle with thickness absolute
- Fix View Space Position is VFX Shadergraph [Case 1285603](https://fogbugz.unity3d.com/f/cases/1285603/)
- Fix [Case 1268354](https://fogbugz.unity3d.com/f/cases/1268354/)
- Fixed rare bug causing the vfx compilation to do nothing silently.
- Fixed vfx compilation when a diffusion profile property is added to a vfx shadergraph
- SpawnOverDistance spawner block now behaves correctly
- Quad strip outputs take into account orientation block
- Fixed Random Vector subgraph utility operator in Additional Samples
- Fixed Set Strip Progress Attribute utility block in Additional Samples
- Fix [Case 1255182](https://fogbugz.unity3d.com/f/cases/1255182/)
- Remove temporarily "Exact Fixed Time Step" option on VisualEffectAsset to avoid unexpected behavior
- Disable implicit space transformations in sublock graphs as they led to unexpected behaviors

## [10.1.0] - 2020-10-12

### Added

- Compare operator can take int and uint as inputs
- New operator : Sample Signed distance field
- New WorldToViewportPoint operator
- New ViewportToWorldPoint operator
- Added Output Event Handler API
- Added Output Event Handler Samples
- Added ExposedProperty custom Property Drawer
- Error display within the graph.

### Fixed

- Mesh Sampling incorrect with some GPU (use ByteAddressBuffer instead of Buffer<float>)
- Fix for node window staying when clicking elsewhere
- Make VisualEffect created from the GameObject menu have unique names [Case 1262989](https://issuetracker.unity3d.com/product/unity/issues/guid/1262989/)
- Missing System Seed in new dynamic built-in operator.
- Prefab highlight missing for initial event name toggle [Case 1263012](https://issuetracker.unity3d.com/product/unity/issues/guid/1263012/)
- Correctly frame the whole graph, when opening the Visual Effect Editor
- Optimize display of inspector when there is a lot of exposed VFX properties.
- fixes the user created vfx default resources that were ignored unless loaded
- fix crash when creating a loop in subgraph operators [Case 1251523](https://issuetracker.unity3d.com/product/unity/issues/guid/1251523/)
- fix issue with multiselection and objectfields [Case 1250378](https://issuetracker.unity3d.com/issues/vfx-removing-texture-asset-while-multiediting-working-incorrectly)
- Normals with non uniform scales are correctly computed [Case 1246989](https://issuetracker.unity3d.com/product/unity/issues/guid/1246989/)
- Fix exposed Texture2DArray and Cubemap types from shader graph not being taken into account in Output Mesh [Case 1265221](https://issuetracker.unity3d.com/product/unity/issues/guid/1265221/)
- Allow world position usage in shaderGraph plugged into an alpha/opacity output [Case 1259511](https://issuetracker.unity3d.com/product/unity/issues/guid/1259511/)
- GPU Evaluation of Construct Matrix
- Random Per-Component on Set Attribute in Spawn Context [Case 1279294](https://issuetracker.unity3d.com/product/unity/issues/guid/1279294/)
- Fix corrupted UI in nodes due to corrupted point cache files [Case 1232867](https://fogbugz.unity3d.com/f/cases/1232867/)
- Fix InvalidCastException when using byte properties in point cache files [Case 1276623](https://fogbugz.unity3d.com/f/cases/1276623/)
- Fix https://issuetracker.unity3d.com/issues/ux-cant-drag-a-noodle-out-of-trigger-blocks
- Fix [Case 1114281](https://issuetracker.unity3d.com/product/unity/issues/guid/1114281/)
- Fix shadows not being rendered to some cascades with directional lights [Case 1229972](https://issuetracker.unity3d.com/issues/output-inconsistencies-with-vfx-shadow-casting-and-shadow-cascades)
- Fix VFX Graph window invalidating existing Undo.undoRedoPerformed delegates.
- Fix shadergraph changes not reflected in VisualEffectGraph [Case 1278469](https://fogbugz.unity3d.com/f/cases/resolve/1278469/)

## [10.0.0] - 2019-06-10

### Added

- Tooltips for Attributes
- Custom Inspector for Spawn context, delay settings are more user friendly.
- Quick Expose Property : Holding Alt + Release Click in an Empty space while making property edges creates a new exposed property of corresponding type with current slot value.
- Octagon & Triangle support for planar distortion output
- Custom Z axis option for strip output
- Custom Inspector for Update context, display update position/rotation instead of integration
- Tooltips to blocks, nodes, contexts, and various menus and options
- VFX asset compilation is done at import instead of when the asset is saved.
- New operators: Exp, Log and LoadTexture
- Duplicate with edges.
- Right click on edge to create a interstitial node.
- New quad distortion output for particle strips
- New attribute for strips: particleCountInStrip
- New options for quad strips texture mapping: swap UV and custom mapping
- Naming for particles system and spawn context
- Noise evaluation now performed on CPU when possible
- Range and Min attributes support on int and uint parameters
- New Construct Matrix from Vector4 operator
- Allow filtering enums in VFXModels' VFXSettings.
- Sample vertices of a mesh with the Position (Mesh) block and the Sample Mesh operator
- New built-in operator providing new times access
- More efficient update modes inspector
- Ability to read attribute in spawn context through graph
- Added save button to save only the current visual effect graph.
- Added Degrees / Radians conversion subgraphs in samples
- uint parameter can be seen as an enum.
- New TransformVector4 operator
- New GetTextureDimensions operator
- Output Event context for scripting API event retrieval.
- per-particle GPU Frustum culling
- Compute culling of particle which have their alive attribute set to false in output
- Mesh and lit mesh outputs can now have up to 4 differents meshes that can be set per Particle (Experimental)
- Screen space per particle LOD on mesh and lit mesh outputs (Experimental)

### Fixed

- Moved VFX Event Tester Window visibility to Component Play Controls SceneView Window
- Universal Render Pipeline : Fog integration for Exponential mode [Case 1177594](https://issuetracker.unity3d.com/issues/urp-slash-fog-vfx-particles)
- Correct VFXSettings display in Shader Graph compatible outputs
- No more NullReference on sub-outputs after domain reload
- Fix typo in strip tangent computation
- Infinite recompilation using subgraph [Case 1186191](https://issuetracker.unity3d.com/product/unity/issues/guid/1186191/)
- Modifying a shader used by an output mesh context now automatically updates the currently edited VFX
- Possible loss of shadergraph reference in unlit output
- ui : toolbar item wrap instead of overlapping.
- Selection Pass for Universal and High Definition Render Pipeline
- Copy/Paste not deserializing correctly for Particle Strip data
- WorldPosition, AbsoluteWorldPosition & ScreenPos in shadergraph integration
- Optimize VFXAssetEditor when externalize is activated
- TransformVector|Position|Direction & DistanceToSphere|Plane|Line have now spaceable outputs
- Filter out motion vector output for lower resolution & after post-process render passes [Case 1192932](https://issuetracker.unity3d.com/product/unity/issues/guid/1192932/)
- Sort compute on metal failing with BitonicSort128 [Case 1126095](https://issuetracker.unity3d.com/issues/osx-unexpected-spawn-slash-capacity-results-when-sorting-is-set-to-auto-slash-on)
- Fix alpha clipping with shader graph
- Fix output settings correctly filtered dependeing on shader graph use or not
- Fix some cases were normal/tangent were not passes as interpolants with shader graph
- Make normals/tangents work in unlit output with shader graph
- Fix shader interpolants with shader graph and particle strips
- SpawnIndex attribute is now working correctly in Initialize context
- Remove useless VFXLibrary clears that caused pop-up menu to take long opening times
- Make sure the subgraph is added to the graph when we set the setting. Fix exception on Convert To Subgraph.
- Subgraph operators appear on drag edge on graph.
- Sample Scene Color & Scene Depth from Shader Graph Integration using High Definition and Universal Render Pipeline
- Removed Unnecessary reference to HDRP Runtime Assembly in VFX Runtime Assembly
- Allow alpha clipping of motion vector for transparent outputs [Case 1192930](https://issuetracker.unity3d.com/product/unity/issues/guid/1192930/)
- subgraph block into subgraph context no longer forget parameter values.
- Fix exception when compiling an asset with a turbulence block in absolute mode
- Fixed GetCustomAttribute that was locked to Current
- Shader compilation now works when using view direction in shader graph
- Fix for destroying selected component corrupt "Play Controls" window
- Depth Position and Collision blocks now work correctly in local space systems
- Filter out Direction type on inconsistent operator [Case 1201681](https://issuetracker.unity3d.com/product/unity/issues/guid/1201681/)
- Exclude MouseEvent, RigidBodyCollision, TriggerEvent & Sphere binders when physics modules isn't available
- Visual Effect Activation Track : Handle empty string in ExposedProperty
- in some cases AABox position gizmo would not move when dragged.
- Inspector doesn't trigger any exception if VisualEffectAsset comes from an Asset Bundle [Case 1203616](https://issuetracker.unity3d.com/issues/visual-effect-component-is-not-fully-shown-in-the-inspector-if-vfx-is-loaded-from-asset-bundle)
- OnStop Event to the start of a Spawn Context makes it also trigger when OnPlay is sent [Case 1198339](https://issuetracker.unity3d.com/product/unity/issues/guid/1198339/)
- Remove unexpected public API : UnityEditor.VFX.VFXSeedMode & IncrementStripIndexOnStart
- Fix yamato error : check vfx manager on domain reload instead of vfx import.
- Filter out unrelevant events from event desc while compiling
- Missing Packing.hlsl include while using an unlit shadergraph.
- Fix for nesting of VFXSubgraphContexts.
- Runtime compilation now compiles correctly when constant folding several texture ports that reference the same texture [Case 1193602](https://issuetracker.unity3d.com/issues/output-shader-errors-when-compiling-the-runtime-shader-of-a-lit-output-with-exposed-but-unassigned-additional-maps)
- Fix compilation error in runtime mode when Speed Range is 0 in Attribute By Speed block [Case 1118665](https://issuetracker.unity3d.com/issues/vfx-shader-errors-are-thrown-when-quad-outputs-speed-range-is-set-to-zero)
- NullReferenceException while assigning a null pCache [Case 1222491](https://issuetracker.unity3d.com/issues/pointcache-nullrefexception-when-compiling-an-effect-with-a-pcache-without-an-assigned-asset)
- Add message in inspector for unreachable properties due to VisualEffectAsset stored in AssetBundle [Case 1193602](https://issuetracker.unity3d.com/product/unity/issues/guid/1203616/)
- pCache importer and exporter tool was keeping a lock on texture or pCache files [Case 1185677](https://issuetracker.unity3d.com/product/unity/issues/guid/1185677/)
- Convert inline to exposed property / Quick expose property sets correct default value in parent
- Age particles checkbox was incorrectly hidden [Case 1221557](https://issuetracker.unity3d.com/product/unity/issues/guid/1221557/)
- Fix various bugs in Position (Cone) block [Case 1111053] (https://issuetracker.unity3d.com/product/unity/issues/guid/1111053/)
- Handle correctly direction, position & vector types in AppendVector operator [Case 1111867](https://issuetracker.unity3d.com/product/unity/issues/guid/1111867/)
- Fix space issues with blocks and operators taking a camera as input
- Generated shaderName are now consistent with displayed system names
- Remove some shader warnings
- Fixed Sample Flipbook Texture File Names
- Don't lose SRP output specific data when SRP package is not present
- Fix visual effect graph when a subgraph or shader graph dependency changes
- Support of flag settings in model inspector
- height of initial event name.
- fix colorfield height.
- fix for capacity change for locked asset.
- fix null value not beeing assignable to slot.
- Prevent capacity from being 0 [Case 1233044](https://issuetracker.unity3d.com/product/unity/issues/guid/1233044/)
- Fix for dragged parameters order when there are categories
- Avoid NullReferenceException in Previous Position Binder" component. [Case 1242351](https://issuetracker.unity3d.com/product/unity/issues/guid/1242351/)
- Don't show the blocks window when context cant have blocks
- Prevent from creating a context in VisualEffectSugraphOperator by draggingfrom an output slot.
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
- Fix creation of StringPropertyRM
- Enum fields having headers show the header in the inspector as well.
- Handle correctly disabled alphaTreshold material slot in shaderGraph.

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
- Turbulence noise updated: noise type and frequency can be specified [Case 1141282] (https://issuetracker.unity3d.com/issues/vfx-particles-flicker-when-blend-mode-is-set-to-alpha-turbulence-block-is-enabled-and-there-is-more-than-50000-particles)
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
