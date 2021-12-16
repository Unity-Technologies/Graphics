# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [10.8.1] - 2021-12-16

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [10.8.0] - 2021-09-20

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [10.7.0] - 2021-07-02
### Fixed
- Removed shader warnings due to SAMPLE_DEPTH_TEXTURE redefinition [Case 1331262](https://issuetracker.unity3d.com/product/unity/issues/guid/1331262/)
- Fix Soft Particle depth computation when using an orthographic camera [Case 1309961](https://issuetracker.unity3d.com/product/unity/issues/guid/1309961)
- Compilation error undeclared identifier 'Infinity' [Case 1328592](https://issuetracker.unity3d.com/product/unity/issues/guid/1328592/)
- Extract position from a transform is wrong on GPU [Case 1353533](https://issuetracker.unity3d.com/product/unity/issues/guid/1353533/)
- Fix rendering artifacts on some mobile devices [Case 1149057](https://issuetracker.unity3d.com/product/unity/issues/guid/1149057/)
- Fix compilation failure on OpenGLES [Case 1348666](https://issuetracker.unity3d.com/product/unity/issues/guid/1348666/)
- Fix potential infinite compilation when using subgraphs [Case 1346576](https://issuetracker.unity3d.com/product/unity/issues/guid/1346576/)
- Prevent out of sync serialization of VFX assets that could cause the asset to be dirtied without reason
- Fix undetermitism in space with LocalToWorld and WorldToLocal operators [Case 1355820](https://issuetracker.unity3d.com/product/unity/issues/guid/1355820/)

## [10.6.0] - 2021-04-29
### Fixed
- Fixed VFX with output mesh being always reimported [Case 1309753](https://issuetracker.unity3d.com/product/unity/issues/guid/1309753/)
- Modified state in the VFX tab has now a correct state
- Remove unexpected expression in spawn context evaluation [Case 1318412](https://issuetracker.unity3d.com/product/unity/issues/guid/1318412/)
- Fix incorrect buffer type for strips
- Compilation issue when normal is used in shadergraph for opacity with unlit output

## [10.5.0] - 2021-04-19
### Fixed
- Target GameObject attach button does not allow attaching a valid VFX if the last selection was invalid. [Case 1312178](https://issuetracker.unity3d.com/product/unity/issues/guid/1312178/)
- Deleting flow edge between Init and Update throw an invalid opeation exception [Case 1315593](https://issuetracker.unity3d.com/product/unity/issues/guid/1315593/)
- Having more than five GPU Event output leads to "unexpected token 'if" at compilation [Case 1323434](https://issuetracker.unity3d.com/product/unity/issues/guid/1323434/)
- Deleted properties still show up in the inspector [Case 1320952](https://issuetracker.unity3d.com/product/unity/issues/guid/1320952/)
- Regression with some settings not always triggering a recompilation [Case 1322844](https://issuetracker.unity3d.com/product/unity/issues/guid/1322844/)
- Exception in VFXFilterWindow if search field is empty [Case 1235269](https://issuetracker.unity3d.com/product/unity/issues/guid/1235269/)

## [10.4.0] - 2021-03-11
### Changed
- Sphere and Cube outputs are now experimental
- Property Binder : Handle Remove Component removing linked hidden scriptable objectfields
- Property Binder : Prevent multiple VFXPropertyBinder within the same game object
- Tidy up of platform abstraction code for random number generation, requires a dependency on com.unity.render-pipelines.core for those abstractions.

### Fixed
- VFXEventBinderBase throwing a null reference exception in runtime
- Unexpected compilation warning in VFXMouseBinder [Case 1313003](https://issuetracker.unity3d.com/product/unity/issues/guid/1313003/)
- Prevent infinite compilation loop [Case 1298466](https://issuetracker.unity3d.com/product/unity/issues/guid/1298466/)
- Remove some useless compilation triggers (modifying not connected or disabled nodes for instance)
- Modified Sign operator node output for float when input is 0.0f [Case 1299922](https://fogbugz.unity3d.com/f/cases/1299922/)
- Use alphabetical order in type list in blackboard "+" button [Case 1304109](https://issuetracker.unity3d.com/product/unity/issues/guid/1304109/)
- Consistently displays the Age Particles checkbox in Update context [Case 1221557](https://issuetracker.unity3d.com/product/unity/issues/guid/1221557/)
- Fix compute culling compilation in URP [Case 1309174](https://fogbugz.unity3d.com/f/cases/1309174/)
- pCache: Unexpected ditable field in Mesh Statistics, Save & Cancel pCache, error trying to access not readable texture [Case 1122417](https://issuetracker.unity3d.com/product/unity/issues/guid/1122417/)
- Handle correctly locked VisualEffectAsset with version control system [Case 1261051](https://issuetracker.unity3d.com/product/unity/issues/guid/1261051/)
- Artefact in VFXView using efficient debug mode in component target board [Case 1243947](https://issuetracker.unity3d.com/product/unity/issues/guid/1243947/)
- Compilation error due to direct access to GetWorldToObjectMatrix instead of VFXGetWorldToObjectMatrix [Case 1308481](https://issuetracker.unity3d.com/product/unity/issues/guid/1308481/)
- Safe Normalization of Cross Products in Orient blocks [Case 1272724](https://issuetracker.unity3d.com/product/unity/issues/guid/1272724)
- Property Binder : Undo after reset [Case 1293794](https://issuetracker.unity3d.com/product/unity/issues/guid/1293794/)
- Property Binder : Allow copy/past from a game object to another
- Deleting a context node and a block while both are selected throws a null ref exception. [Case 315578](https://issuetracker.unity3d.com/product/unity/issues/guid/1315578/)
- Fixed shader compilation errors with textures in shader graph [Case 1309219](https://issuetracker.unity3d.com/product/unity/issues/guid/1309219/)
- Fixed issue with VFX using incorrect buffer type for strip data

## [10.3.1] - 2021-01-26

Version Updated
The version number for this package has increased due to a version update of a related graphics package.

## [10.3.0] - 2020-11-16
### Added
- Added new setting to output nodes to exclude from TAA
- New Sample Point cache & Sample Attribute map operators
- Changed the "Edit" button so it becomes "New" when no asset is set on a Visual Effect component, in order to save a new visual effect graph asset.

### Fixed
- Forbid incorrect link between incompatible context [Case 1269756](https://issuetracker.unity3d.com/product/unity/issues/guid/1269756/)
- Serialization issue with VFXSpawnerCallbacks
- Unexpected exception while trying to display capacity warning [Case 1294180](https://issuetracker.unity3d.com/product/unity/issues/guid/1294180/)
- Exclude Operator, Context, Block and Subgraph from Preset [Case 1232309](https://issuetracker.unity3d.com/product/unity/issues/guid/1232309/)
- Incorrect path on Linux while targetting Android, IOS or WebGL [Case 1279750](https://issuetracker.unity3d.com/product/unity/issues/guid/1279750/)
- Prevent creation of subgraph containing only partial systems [Case 1284053](https://issuetracker.unity3d.com/product/unity/issues/guid/1284053/)
- Fix [Case 1223747](https://fogbugz.unity3d.com/f/cases/1223747/)
- Prevent pasting context within operator/block subgraph [Case 1235269](https://issuetracker.unity3d.com/product/unity/issues/guid/1235269/)
- VFXEventBinderBase throwing a null reference exception in runtime
- Fix [Case 1290493](https://fogbugz.unity3d.com/f/cases/1290493/#BugEvent.1072735759)

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
- Fix  https://issuetracker.unity3d.com/issues/ux-cant-drag-a-noodle-out-of-trigger-blocks
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
