# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.12.2-preview] - 2021-11-03

### Fixed
- Fixed conflicting GUID
- Fixed missing icons

## [0.12.1-preview] - 2021-11-03

## [0.12.0-preview] - 2021-11-02

### Added
- `IGraphModel.GetEdgesForPort()`
- `GraphModel.GetEdgesForPort()`, implemented as a fast way to get the edge models connected to a port.
- `GraphModel.GetEdgeConnectedToPorts` extension method to find the edge connected to a pair of ports.
- It's now possible to access the Connector VisualElement of a `PortConnectorPart` using `PortConnectorPart.Connector`.
- Manipulation of blocks (creation, drag and drop, copy / paste, ...)
- Added a button to toggle the Item Library (AKA Searcher) preview panel.
- State of preview panel visibility is stored per user per tool.
- `SearcherWindow.ShowReusableWindow` allows re-using a `NormalWindow` instead of creating a popup window.
- `CreateNodeCommandExtensions` are helped methods to generate commands creating nodes on a graph, edge or port.


### Removed
- `IGraphModel.GetEdgesConnections(IPortModel)`: use `IGraphModel.GetEdgesForPort()` instead.
- `IGraphModel.GetEdgesConnections(INodeModel)`: use `IGraphModel.GetEdgesForPort()` instead.
- `IGraphModel.GetConnections()`: use `IGraphModel.GetEdgesForPort()` instead.
- Removed all UQuery fields on `GraphView`: `GraphElements`, `Nodes`, `Ports`, `Edges`, `Stickies` and `Placemats`. Use the model and `model.GetUI()` instead.
- Removed `GraphView.Highlightables` and `Blackboard.Highlightables`. Highlighting is now done in the `UpdateFromModel` method.
- Removed `HighlightHelper` class.
- Removed `IHighlightable` interface and implementations in `BlackboardField`, `Node` and `TokenNode`.
- `Stencil.OnDragAndDropVariableDeclarations` removed, the caller (Blackboard) now dispatches a `CreateNodeCommand` with all variables.


### Changed
- `ModelUIPartList` no longer implements `IEnumerable<IModelUIPart>`. To enumerate the parts, use `ModelUIPartList.Parts`.
- `GraphModel.InstantiatePortal` has been renamed `GraphModel.InstantiatePortalDeclaration`
- Renamed `GraphElementMapping.GetAllUIForModel` to `AppendAllUIs`; method now appends elements to a caller supplied list.
- Renamed `GraphElementMapping.FirstOrDefault` to `FirstUIOrDefault`.
- UI creation context changed from a string to a `IUIContext`. See `BlackboardCreationContext` for an example.
- `IPlacematModel.ZOrder` has been replaced by the extension method `GetZOrder()` on any `IGraphElementModel`.
- Renamed `CreateNodeFromSearcherCommand` to `CreateNodeCommand`.
- Deprecated `CreateNodeCommand` constructor to encourage `CreateNodeCommand` extensions and static methods.
- Deprecated `CreateNodeFromPortCommand` to encourage using `CreateNodeCommand`.
- Deprecated `CreateNodeOnEdgeCommand` to encourage using `CreateNodeCommand`.
- Deprecated `CreateVariableNodesCommand` to encourage using `CreateNodeCommand`.
- Made the hit box to drag ports larger than the connector.


## [0.11.2-preview] - 2021-07-06

### Changed
- Put back `MovedFrom` attributes on model classes.


## [0.11.1-preview] - 2021-06-15

### Changed
- Searcher namespace changed to `UnityEditor.GraphToolsFoundation.Searcher` to avoid clash with package.
- Searcher assemblies renamed to `Unity.GraphToolsFoundation.Searcher.*` to avoid clash with package.
- Searcher meta files have new guids to avoid clash with package.


## [0.11.0-preview] - 2021-06-14

### Added
- New extensible node inspector. See `ModelInspectorView`, `FieldsInspector`, `ModelPropertyField<T>` and `ICustomPropertyField`.

### Removed
- Removed dependencies on `com.unity.properties` and `com.unity.properties.ui` packages.
- `TypeSearcherDatabase.FromItems`, use `new SearcherDatabase(items)` instead.

### Changed
- MathExpressionParser can parse mathematical expressions.
- Math Book sample's MathOperator nodes now have variable input ports.
- All asmdef in the package have their `autoReferenced` properties set to `false`.
- `DeclarationModel.Rename` made virtual.
- `TypeSerializer` renamed to `TypeHandleHelpers`.
- Capabilities are no longer serialized in graph element models.
- The searcher was changed and its code integrated to the GTF codebase.
- `IStencil.GetSearcherRect` was replaced by `GraphToolStateExtensionsForSearcherSize.GetSearcherSize`.
- `IStencil.SetSearcherSize` was replaced by `GraphToolStateExtensionsForSearcherSize.SetSearcherSize`.
- `UpdateModelPropertyValueCommand` was replaced by `SetModelFieldCommand`.
- `ChangeElementColorCommand` now derives from `ModelCommand<TModel, TValue>`.
- Many uses of `GraphView` were replaced by uses of `IModelView`.
- `DefaultFactoryExtensions` was renamed `GraphViewFactoryExtensions`.
- Directory `Editor/GraphElements/Elements` was renamed to `Editor/GraphElements/ModelUI`.
- `GraphView IModelUI.GraphView` changed to `IModelView View`.
- `IModelUI.AddToGraphView` renamed to `AddToView`.
- `IModelUI.RemoveFromGraphView` renamed to `RemoveFromView`.
- `IPropertyVisitorNodeTarget` renamed to `IHasInspectorSurrogate`
- `OpenedGraph.FileId` renamed to `OpenedGraph.AssetLocalId`.
- `SearcherItemUtility.GetItemFromPath` is now an extension method in `SearcherItemCollectionExtensions`
- `SearcherGraphView` made internal.
- `TypeSearcherDatabase` renamed to `TypeSearcherExtensions`
- `ModelUIPartList` no longer implements `IEnumerable<IModelUIPart>`. To enumerate the parts, use `ModelUIPartList.Parts`.
- `GraphModel.InstantiatePortal` has been renamed `GraphModel.InstantiatePortalDeclaration`
- Renamed `GraphElementMapping.GetAllUIForModel` to `AppendAllUIs`; method now appends elements to a caller supplied list.
- Renamed `GraphElementMapping.FirstOrDefault` to `FirstUIOrDefault`.

### Fixed

- Fixed node inspector and some elements' appearance in light skin.


## [0.10.1-preview] - 2021-05-14

### Changed

- Auto itemization of constants and variables is no longer on by default. Use the respectively the `BoolPref.AutoItemizeConstants` and `BoolPref.AutoItemizeVariables` preferences to control the behavior.

### Fixed

- Bug that needlessly triggered full UI rebuild in certain conditions.


## [0.10.0-preview] - 2021-04-30

### Added

- The models and their interfaces now interact with the stencil through the new `IStencil` interface.
- Added the `ContextNode` class.
- Added the `BlockNode` class.

### Removed

- `IBlackboardGraphModel.PopulateCreateMenu`; Please use `Stencil.PopulateBlackboardCreateMenu` now.
- The version of `GraphModel.CreateItemizedNode` that took a `GraphToolState` as a parameter; Use the other version instead.
- `SerializableGUID.FromParts`. Use the appropriate `SerializableGUID` constructor instead.
- The Resources folder.
- `ToggleAllPortOrientationCommand`
- `ToggleEdgePortOrientationCommand`
- `InlineValueEditor.CreateEditorForNodeModel`, use `InlineValueEditor.CreateEditorForConstant` instead.
- `Port.OnDropModel`
- `Port.OnDropVariableDeclarationModel`
- `IHasMainExecutionInputPort`
- `IHasMainExecutionOutputPort`
- `IHasMainInputPort`
- `IHasMainOutputPort`
- `IMigratePorts`

### Changed

- `NodeModel.m_Collapsed` is now serialized.
- `Stencil` are not serialized anymore.
- `GraphViewEditorWindow.CanHandleAssetType` is now abstract.
- Moved the following to the new `Unity.GraphTools.Foundation.Overdrive.Model` assembly:
  - All the model interfaces and extensions
  - GTF implementations of the model interfaces
  - `Capabilities`
  - `Direction`
  - `Orientation`
  - `PortType`
  - Helpers to convert `SerializableGUID` to `GUID` and vice versa
  - `IDependency`, `LinkedNodesDependency` and `PortalNodesDependency`
- Moved the following to the new `Unity.GraphTools.Foundation.Overdrive` runtime assembly:
  - `Enumeration`
  - `SerializableGUID`
  - `MemberInfoUtility`
  - `TaskUtility`
  - `TypeHandle` and all the supporting classes and extensions
  - Extensions for `Assembly`, `HashSet`, `IEnumerable` and `string`
  - Serialization dictionary helpers
- `StringExtensions.CodifyString` is now a real extension method.
- Reworked `EditorStateCache.GetState` to always add newly created state components to cache.
- `Command` was renamed `UndoableCommand`.
- `CommandDispatcher.GraphToolState` is obsolete. Use `CommandDispatcher.State`.
- `GraphToolState.PreDispatchCommand` and `GraphToolState.PostDispatchCommand` are now methods of the `CommandDispatcher`.
- `StateObserver` is now a generic. Use the observed state type as the type parameter.
- `PersistedEditorState` was renamed `PersistedState`.
- `BlackboardField.NameLabel` setter is now protected.
- `DeclarationModel.Title` is not anymore modified after renaming to make it C# compatible.
- `GraphView` name is now mandatory in constructor.
- `IGraphModel.Name` no longer has a setter. The default `GraphModel` implementation returns the name of the associated asset.
- The default value of `GraphView.SupportsWindowedBlackboard` changed from `false` to `true`.
- By default, the "Build All" and "Live Tracing" buttons will not show up for graph tools.
  You will need to provide your own ToolbarProvider to enable them.
- When opening an graph window on its blank page, only the "New Graph", "Save All" (both disabled) and "Option" (enabled) buttons will now show in the toolbar.
- All asmdef in the package have their `autoReferenced` properties set to `false`.
- `DeclarationModel.Rename` made virtual.
- `TypeSerializer` renamed to `TypeHandleHelpers`.
- `SetNodeEnabledStateCommand` renamed to `ChangeNodeStateCommand`.
- `SetNodeCollapsedCommand` renamed to `CollapseNodeCommand`.
- `UpdateConstantNodeValueCommand` renamed to `UpdateConstantValueCommand`.
- `SetPlacematCollapsedCommand` renamed to `CollapsePlacematCommand`.
- `ToggleLockConstantNodeCommand` renamed to `LockConstantNodeCommand`.
- `UpdateExposedCommand` renamed to `ExposeVariableCommand`.
- `ExpandOrCollapseBlackboardRowCommand` renamed to `CollapseVariableInBlackboard`.
- `ToggleTracingCommand` renamed to `ActivateTracingCommand`.
- `DropTarget.CanAcceptSelectionDrop` renamed to `CanAcceptDrop`
- `GraphTraversal` is not public anymore.
- `ICloneableExtensions` was renamed to `CloneHelpers`.
- `IEdgeModel.TryMigratePorts` was replaced by `IEdgeModel.AddPlaceHolderPorts`
- `Direction` renamed to `PortDirection`
- `Orientation` renamed to `PortOrientation`

### Fixed

- Math book samples using functions now reload properly.
- Math book graphs have access to shortcut keys.
- EditableLabels no longer need 3 clicks to focus on recent editors.
- Shortcuts registered with `ToolShortcutEventAttribute` can now be registered for a specific tool.
- When a graph asset is deleted or renamed, the window is updated to reflect the change.
- Fixed rendering issue that occurs when the UI Toolkit package is installed.
- Fixed Unity shortcuts being broken by using a GTF based tool.
- Fixed the dispatcher not notifying observers of state modified by another observer.


## [0.9.2-preview] - 2021-04-07

### Added

- Added `UxmlFactory` and `UxmlTraits` to `CollapseButton` to make it usable from uxml files.

### Changed

- Moved the creation of the `IBlackboardGraphModel` to the `Stencil`.
- Made the extension method `IGraphElementModel.GetAllUIs` public.
- Made the members of `BlackboardSectionListPart` accessible from derived classes.


## [0.9.1-preview] - 2021-03-29

### Removed

- `DisposableStateComponentUpdater` (no longer needed)
- `IStateComponentUpdater.BeginStateChange` and `IStateComponentUpdater.EndStateChange`.
  Since `IStateComponentUpdater` now implements `IDisposable`, use the Disposable pattern for updaters. A new `Initialize` method now replaces the `BeginStateChange`.

### Changed

- Renamed `StateComponent.Updater` to `StateComponent.UpdateScope`.


## [0.9.0-preview.2] - 2021-03-23

### Added

- `SelectElementsCommand` `ClearSelectionCommand` and  to select and unselect graph view elements.
- `ReframeGraphViewCommand` to change the graph view pan and zoom.
- `GetPlacematMinZOrder`, `GetPlacematMaxZOrder` and `GetSortedPlacematModels` extension methods on `IGraphModel`.
- `GraphModel.GetEdgeType`, `GraphModel.GetStickyNoteType`, `GraphModel.GetPlacematType`, `GraphModel.GetVariableDeclarationType` and `GraphModel.GetPortalType` virtual methods used to define the type of elements to create in `GraphModel.CreateEdge`, `GraphModel.CreateStickyNote`, `GraphModel.CreatePlacemat`, `GraphModel.CreateVariableDeclaration` and `GraphModel.CreatePortal`, respectively.
- `IGraphModel.CreateGraphVariableDeclaration` can take a type parameter to create any `IVariableDeclarationModel`-derived declaration.
  Also provided in a generic version of the same.
- Support for shortcuts defined in Unity ShortcutManager.
- Class deriving from `Stencil` must implement the `ToolName` property that returns a unique, human readable tool name.
- `GraphElementModel` class as the parent class for most classes implementing `IGraphElementModel`
- `IGraphElementModel` new members `Color`, `HasUserColor` and `ResetColor()` to customize elements color
- `Capbilities.Colorable` to tell an `IGraphElementColor` can change color. `NodeModel` and `PlacematModel` have this capability by default.
- `PortModel` now save it's `AssetModel` instead of getting it from its `NodeModel`. Assigned during `CreatePort()`
- `ResetElementColorCommand` and `ChangeElementColorCommand` can be called on any `IGraphElementModel` and will check for `Colorable` capability.
- `GraphElementModel` introduces versioning with `Version` for backward compatibility.
- `VerticalPortContainerPart` created to house the vertical ports on nodes.
- New Vertical Flow sample showcasing the vertical flow.
- Dirty state displayed in the window title with an *.
- `IChangeset` and `ChangesetManager` to enable `IStateComponent` to manage changesets.
- `IStateComponentUpdater` to encapsulate updates to state components.
- `IStateObserver` for classes that observes `IStateComponent`s and want to be notified of their changes.
- `Observation`, to encapsulate an observation of an `IStateComponent` by an `IStateObserver`
- `VisualElement.SafeQ`, equivalent to `VisualElement.Q` but does a null check first.
- Searcher Size is kept between uses.

### Removed

- `ISelection`; query or use commands to modify the `GraphToolState.SelectionState` instead.
- `ISelectableGraphElement`: use `IGraphElement` instead. Query the selection or use commands to modify the selection
  using `GraphToolState.SelectionState` instead. React to change in selection in `GraphElement.UpdateElementFromModel()` instead.
- `GraphView.OnSelectionChangedCallback`; react to selected state in `GraphElement.UpdateElementFromModel()` instead.
- `VisualElementBridge`; derive from `VisualElement` instead and use extension methods from `GraphViewStaticBridge`.
- Unused classes and interface `SearchTreeEntry`, `SearchTreeGroupEntry`, `SearchWindowContext`, `ISearchWindowProvider` and `SearchWindow`.
- `IGraphViewSelection`, `GraphViewUndoRedoSelection` and `PersistedSelection`; functionality is now handled by `SelectionStateComponent`.
- `ViewTransformChangedCallback`. Install an observer on `ReframeGraphViewCommand` instead, using `CommandDispatcher.RegisterObserver`.
- `GraphModel.NodesByGuid`; use `GraphModel.TryGetModelFromGuid()` instead.
- `ChangePlacematLayoutCommand` and `ChangeStickyNoteLayoutCommand`: use `ChangeElementLayoutCommand` instead.
- `ResizeFlags` and `IResizableGraphElement`
- `GraphModel.CreateNodeInternal` was merged into `GraphModel.CreateNode`
- `NodeModel.DataTypeString`
- `NodeModel.VariableString`
- `VariableDeclarationModel.Create`; use `GraphModel.CreateGraphVariableDeclaration()` instead.
- `GraphModel.DuplicateGraphVariableDeclaration` has been made generic for any `IVariableDeclarationModel`.
- `ShortcutHandler` manipulator. Replaced by callbacks on subclasses of `ShortcutEventBase` events.
- `GraphView.ShortcutHandler` and `GraphView.GetShortcutDictionary()` To define new shortcuts, create a subclass
  of `ShortcutEventBase` and add the `[ToolShortcutEvent]` attribute to it. See the `ShortcutFrameAllEvent` class
  for an example. To respond to shortcuts, register a callback on the new shortcut event subclass.
- `GraphElement.IsDeletable()`: ask model instead
- `GraphElement.IsResizable()`: ask model instead
- `GraphElement.IsDroppable()`: ask model instead
- `GraphElement.IsRenamable()`: ask model instead
- `GraphElement.IsCopiable()`: ask model instead
- `GraphViewBridge`; derive from `VisualElement` instead.
- `Stencil.GetThisType()`
- Remove `IVariableDeclarationMetadataModel` from `IVariableDeclarationModel`
- Remove `GetMetadataModel()` and `SetMetadataModel()` from `VariableDeclarationModel`
- `UndoRedoTraversal`
- `IEdgeModel.ResetPorts()`: Found in  Basic model `EdgeModel`.
- Removed the distinction between pre- and post-command observers. CommandObservers are now all executed before the command handler.
- `PrefixRemoveFromClassList`; use `PrefixEnableInClassList` instead.

### Changed

- `GraphToolState.PushUndo()` now saves the `GraphViewStateComponent` (graph view pan and zoom), the `BlackboardViewStateComponent`
  (blackboard variable expanded state) and `SelectionStateComponent` (the current selection).
- `GraphView.Selection` changed to `GraphView.GetSelection()`.
- `ISelection` replaced by `IDragSource`.
- Renamed `NodeSerializationHelpers` to `SerializationHelpers
- Renamed `BoolPref.LogAllDispatchedActions` to `BoolPref.LogAllDispatchedCommands`
- `PlacematContainer.CycleDirection` moved to `PlacematCommandsExtension.CycleDirection`
- The `MainToolbar` USS name `vseMenu` has been removed. It now has the USS class `ge-main-toolbar`.
- All list of graph element models in `GraphModel` are now private. Use the corresponding properties and `Add...` / `Remove...` to access them.
- `IVariableDeclarationModel.VariableName` was replaced by `IVariableDeclarationModel.GetVariableName()`
- `CreateGraphVariableDeclarationCommand` constructor can take a type parameter to create any `IVariableDeclarationModel`-derived declaration.
- `GraphModel.GetVariableDeclarationType` was renamed to `GraphModel.GetDefaultVariableDeclarationType`
- `PromptSearcherEvent` moved from the internal bridge to the main code base.
- Tools need to call `ShortcutHelper.RegisterDefaultShortcuts()` to use the default shortcuts provided by GTF. We suggest adding an
  `[InitializeOnLoad]` static method in the tool editor window class.
- `CollapisbleInOutNode` now has a top and bottom container for vertical ports.
- `InOutPortContainerPart` only handles horizontal ports.
- `NodeModel.DeletePort` has been deprecated. The default behavior (with `removeFromOrderedPorts = false`) did not actually delete any ports. Use `NodeModel.DisconnectPort` instead. In the next release, `NodeModel.DeletePort` will lost its extra parameter and will always delete the ports.
- `INodeModel.CreatePort` now takes in an `Orientation` parameter.
- `INodeModel.AddInputPort`, `INodeModel.AddOutputPort`, `INodeModel.AddPlaceHolderPort`, `INodeModel.AddDataInputPort`, `INodeModel.AddDataOutputPort`, `INodeModel.AddExecutionInputPort` and `INodeModel.AddExecutionOutputPort` now all take in an optional `Orientation` parameter.
- Moved and renamed `GraphViewBridge.contentContainer` to `GraphView.ContentContainer`
- Moved and renamed `GraphViewBridge.viewTransform` to `GraphView.ViewTransform`
- Moved and renamed `GraphViewBridge.redrawn` to `GraphView.Redrawn`
- `IPortNode` renamed to `IPortNodeModel`
- `IInOutPortsNode` renamed to `IInputOutputPortsNodeModel`
- `ISingleInputPortNode` renamed to `ISingleInputPortNodeModel`
- `ISingleOutputPortNode` renamed to `ISingleOutputPortNodeModel`
- `IReorderableEdgesPort` renamed to `IReorderableEdgesPortModel`
- Moved Debugging plugin (tracing) from `VisualScripting` to `Plugins` directory.
- Moved Debugging plugin(tracing) to `UnityEditor.GraphtoolFundation.Plugins.Debugging` namespace.
- Renamed Debugging `Port` to `DebuggingPort`.
- Port height can be more than 24px in height.
- `EditorStateComponent` renamed to `StateComponent`
- State components interface is now read-only. Updates to state components should go through their Updater.
- UI update process (most of what was in `GraphViewEditorWindow.Update()`) was refactored into implementations of `IStateObserver`:
  `GraphView.Updater`, `BlackboardUpdateObserver`, `GraphProcessingStatusObserver`, `SidePanelObserver`, etc.
- The graph processing was refactored into the `AutomaticGraphProcessor` observer.
- Graph processing error badges are not part of the `IGraphModel` anymore. They are held by the `GraphProcessingStateComponent`.

### Fixed

- PortContainer instantiate ports UI when it is built.
- `CollapsibleInOutNode.Progress` is now able to find the progress bar.
- CopyPasteData only use interfaces


## [0.8.2-preview] - 2021-02-18

### Fixed

- Clear the models to select after rebuilding the UI.
- CalculateRectToFitAll works edges drawn using something else than the Edge class.


## [0.8.1-preview] - 2021-02-16

### Fixed

- Constant migration in nodes


## [0.8.0-preview] - 2021-02-08

### Added

- Obsolete names on `Enumeration`
- Allow dragging multiple edges from the same port.
- `class BasicModel.GraphTemplate<TStencil>` which provides a default `IGraphTemplate` implementation
- `DefaultSearcherDatabaseProvider` which provides a default `ISearcherDatabaseProvider` implementation
- `DropTarget` abstract `ModelUI` class handling drag and drop from SelectionDragger
- `IDragAndDropHandler` interface demonstrating ability to handle drag and drop events

### Removed

- `GtfoGraphView` class was merged into `GraphView`
- `GtfoEditorWindow` class was merged into `GraphViewEditorWindow`
- `VariableType` enum
- `ITranslator.TranslateAndCompile`
- `ITranslator.CanCompile`
- `RequestCompilationAction` (moved to Visual Scripting ECS package)
- `IGraphAssetModel.SourceFilePath`
- `IDroppable` interface
- `IDropTarget`. Use new class `DropTarget` to catch `GraphView` mouse drags as drag and drop instead of move.
- `Resizer`

### Changed

- `GtfoWindow.GetShortcutDictionary()` is no longer abstract. It is a virtual that provides a default dictionary.
- `IGraphElement` renamed to `IModelUI`
- `GraphElement` and `Port` now inherits from new class `ModelUI`
- Renamed `State` to `GraphToolState`
- Renamed `Store` to `CommandDispatcher`
- Renamed `StoreHelper` to `CommandDispatcherHelper`
- Renamed `BaseAction` to `Command` and all *Action* to *Command*
- Renamed all `DefaultReducer` to `CommandHandler`
- `Preferences` class is not abstract anymore.
- `GraphView` class is not abstract anymore.
- `GraphViewEditorWindow` class is not abstract anymore.
- `vse-blank-page` USS class name changed to `ge-blank-page`
- `CompilationResult` renamed to `GraphProcessingResult`
- `CompilationStatus` renamed to `GraphProcessingStatus`
- `GraphViewEditorWindow.RecompilationTriggerActions` renamed to `GraphViewEditorWindow.GraphProcessingTriggerActions`
- `GraphViewEditorWindow.RecompileGraph` renamed to `GraphViewEditorWindow.ProcessGraph`
- `Stencil.CreateTranslator` renamed to `Stencil.CreateGraphProcessor`
- `Stencil.OnCompilationStarted` renamed to `Stencil.OnGraphProcessingStarted`
- `Stencil.OnCompilationSucceeded` renamed to `Stencil.OnGraphProcessingSucceeded`
- `Stencil.OnCompilationFailed` renamed to `Stencil.OnGraphProcessingFailed`
- `Stencil.GetCompilationPluginHandlers` renamed to `Stencil.GetGraphProcessingPluginHandlers`
- `ITranslator` renamed to `IGraphProcessor`
- `ITranslator.Compile` renamed to `IGraphProcessor.ProcessGraph`
- `BoolPref.AutoRecompile` renamed to `BoolPref.AutoProcess`
- `CompilationStateComponent` renamed to `GraphProcessingStateComponent`
- `CompilationTimer` renamed to `GraphProcessingTimer`
- `GraphView.RecompilationTriggerActions` renamed to `GraphView.GraphProcessingTriggerActions`
- `NoOpTranslator` renamed to `NoOpGraphProcessor`
- `CompilerErrorBadgeModel` renamed to `GraphProcessingErrorBadgeModel`
- `CompilerError` renamed to `GraphProcessingError`
- `CompilerQuickFix` renamed to `QuickFix`
- `CompilationOptions` renamed to `GraphProcessingOptions`
- `BoolPref.AutoProcess` and `BoolPref.AutoAlignDraggedEdges` are now false by default.
- `IGraphElementPart`, `BaseGraphElementPart` renamed to `IModelUIPart`, `BaseModelUIPart` respectively.
- `GraphElementPartList` renamed to `ModelUIPartList`
- `ICreatableGraphTemplate` merged into `IGraphTemplate`
- `IOnboardingProvider` interface changed to `OnboardingProvider` abstract class
- Default title of `GraphViewEditorWindow` is changed from *Visual Script* to *Graph Tool*
- `Stencil.MoveNodeDependenciesByDefault` is now initialized to `false`
- `GraphView` and `PlaceMat` no longer implement `IDropTarget`
- `Stencil.DragNDropHandler` changed to `Stencil.GetExternalDragNDropHandler()`. Allows to dynamically select a drag and drop handler depending on context.
- `Graphview.ExtractVariablesFromDroppedElements` changed to `Stencil.ExtractVariableFromGraphElement`
- `SerializableGUID` is now backed by a Hash128 rather than a GUID.
- The internal bridge was renamed from InternalAPIEngineBridgeDev.003 to InternalAPIEngineBridge.015

### Fixed

- Dragging a node to a port doesn't dispatch 2 actions anymore
- `IMovable.Move(delta)` correctly uses `delta` parameter for `NodeModel`


## [0.7.0-preview.1] - 2021-01-11

### Fixed

- Remove orphaned .meta files for empty folders.

## [0.7.0-preview] - 2021-01-05

### Added

- `Store.MarkStateDirty` to dirty the state and rebuild the UI completely.
- `Store.BeginStateChange` and `Store.EndStateChange` to frame modifications to models. Except inside action reducers (where this
  is done for you), all calls to `State.MarkModelNew`, `State.MarkModelChanged` and `State.MarkModelDeleted` should occur between
  `Store.BeginStateChange` and `Store.EndStateChange`.
- `Store.BeginViewUpdate` and `Store.EndViewUpdate`. These should be the first and last operations when you update the UI.
  `GtfoWindow.Update` call them for you.
- Dependency system for UI: a graph element can declare dependencies to other graph elements. They can be forward dependencies
  (elements that need to be updated when the element changes) or reverse dependencies (elements that cause the element to be updated
  when they change). There are also additional model dependencies: a graph element can specifies it needs an update whenever some model changes.
- Graph element parts are notified when their owner is added or removed from the graph view, by calls to
  `BaseGraphElementPart.PartOwnerAddedToGraphView` and `BaseGraphElementPart.PartOwnerRemovedFromGraphView`.
- `IGraphElement.Setup` and `IGraphElement.SetupBuildAndUpdate` now take an additional context parameter, a string that can be used
  to modulate the UI. This parameters is most often null, but can take different values to specify the instantiation point
  of the UI. The goal is to specify a context for the model representation, when we need to use different graph elements for the same
  model type represented in different parts of the graph view.
- `GraphElement.AddToGraphView` and `GraphElement.RemoveFromGraphView` are called when an element is added or removed from the graph view.
- `ChangeVariableDeclarationAction`, sent when the user changes the variable declaration of a variable node.
- `RequestCompilationAction`, sent when the user request a compilation.
- Polymorphic `AnyConstant`
- `AnimationClipConstant`, `MeshConstant`, `Texture2DConstant` and `Texture3DConstant`
- `INodeModel.OnCreateNode()`, called when using `GraphModel.CreateNode`

### Removed

- `State.AddModelToUpdate`. Use `State.MarkModelNew`, `State.MarkModelChanged` and `State.MarkModelDeleted` instead.
- `State.ClearModelsToUpdate`
- `State.MarkForUpdate`. Use `State.RequestUIRebuild`
- `Store.StateChanged`. Use store observers instead.
- `GraphElementFactory.CreateUI<T>(this IGraphElementModel)` extension method was removed. Use the static `GraphElementFactory.CreateUI<T>`
  instead.
- `GraphView.DeleteElements()`. Use `GraphView.RemoveElement()` instead.
- `GtfoGraphView.UpdateTopology`. Override `GtfoGraphView.UpdateUI` instead.
- `GraphModel.LastChanges`. Use `State.MarkModelNew`, `State.MarkModelChanged` and `State.MarkModelDeleted` instead.
- `INodeModel.DefineNode()`. Use `OnCreateNode` instead if you don't implement `BasicModel.NodeModel`

### Changed

- Add `BadgeModel` as a model for `Badge`
- Itemize menu item is enabled only on constants and (Get) variables that have more than one edge connected to their data output port.
- GTF-140 The blackboard and minimap no longer close when the add graph(+) button is pressed.
- Default BlankPage now provided
- `BasicModel.DeclarationModel.DisplayTitle` now marked virtual
- `Store.RegisterObserver` and `Store.UnregisterObserver` now take a parameter to register the observer as a
  pre-observer, triggered before the action is executed, or a post-observer, triggered after the action was executed.
- `GraphElementFactory.GetUI`, `GraphElementFactory.GetUI<T>`, `GraphElementFactory.GetAllUIs` were moved to the `UIForModel` class.
- `CreateEdgeAction.InputPortModel` renamed to `CreateEdgeAction.ToPortModel`
- `CreateEdgeAction.OutputPortModel` renamed to `CreateEdgeAction.FromPortModel`
- `GraphView.PositionDependenciesManagers` renamed to `GraphView.PositionDependenciesManager` (without the final s)
- `GraphView.AddElement` and `GraphView.RemoveElement` are now virtual.
- Compilation is now an observer on the `Store`. The virtual property `GtfoWindow.RecompilationTriggerActions` lists the actions
  that should trigger a compilation.
- `IGraphModel` delete methods and extension methods now return the list of deleted models.
- Visual Scripting `CompiledScriptingGraphAsset` now serialized in `VsGraphModel` instead of `DotsStencil`
- Manipulators for all graph elements as well as graph view are now overridable.
- `BlackboardGraphModel` is now owned by the `GraphAssetModel` instead of the `State`.
- `BlackboardField` is now a `GraphElement`
- `Blackboard.GraphVariables` renamed to `Blackboard.Highlightables`
- `ExpandOrCollapseVariableDeclarationAction`  renamed to `ExpandOrCollapseBlackboardRowAction`
- `BlackboardGraphModel` was moved to the `UnityEditor.GraphToolsFoundation.Overdrive.BasicModel` namespace
- Removed the `k_` prefix from all non-private readonly fields.
- Moved some images used by USS.
- `Stencil.GetConstantNodeValueType()` replaced by `TypeToConstantMapper.GetConstantNodeType()`
- Constant editor extension methods now takes an `IConstant` as their second parameter,
  instead of an object representing the value of the constant.
- `ConstantEditorExtensions.BuildInlineValueEditor()` is now public.

### Fixed

- GTF-126: NRE when itemize or convert variables on a set var node
- `TypeSerializer` wasn't resolving `TypeHandle` marked with `MovedFromAttribute` when type wasn't in any namespace.
- Fix a bug where dragging a token on a port would block further dragging
- Fix a bug where dragging a token to a port wouldn't create an edge
- GTF-145 Collapsed placemats at launch not hiding edges
- Fix a bug where dragging a blackboard variable to a port wouldn't be allowed

### Deprecated

- Stencil shouldn't be considered serialized anymore. Kept Serializable for backward compatibility

## [0.6.0-preview.4] - 2020-12-02

### Changed
- Updating minimum requirements for com.unity.collections
- BasicModel.DeclarationModel.DisplayTitle now marked virtual
- Updating minimum requirements for com.unity.collections
- GraphModel.OnDuplicateNode now marked virtual
- BasicModel.DeclarationModel.DisplayTitle now marked virtual

### Fixed
- TypeSerializer wasn't resolving TypeHandle marked with MovedFromAttribute when type wasn't in any namespace.

### Added

- GtfoWindow-derived classes needs to implement `CanHandleAssetType(Type)` to dictate supported asset types.
- Added hook (`OnDuplicateNode(INodeModel copiedNode)`) on `INodeModel` when duplicating node
- Added options to toggle Tracing / Options elements on `MainToolbar`
- Added the `CloneGraph` function in the `GraphModel` for duplicating all the models of a source graph

## [0.5.0-preview.3] - 2020-10-28

## [0.5.0-preview.2] - 2020-10-20

### Added

- Generic `CreateEdge` on base `GraphModel` for easier overriding.
- `GetPort` extension method to get ports by direction and port type.
- Add `GraphModel` reference to `Stencil`
- Add `InstantiateStencil`, changing `Stencil` set pattern in `GraphModel`
- new virtual property `HasEditableLabel` for `EditableTitlePart`

### Removed

- `ChangePlacematColorAction`
- `OpenDocumentationAction`
- `UnloadGraphAssetAction`
- `VariableType.ComponentQueryField` enum value
- `SpawnFlags.CreateNodeAsset` and `SpawnFlags.Undoable` enum values
- `CreateGraphAssetAction` and `CreateGraphAssetFromModelAction`. Use `GraphAssetCreationHelper` to create assets and `LoadGraphAssetAction` to load it in a window.
- `ContextualMenuBuilder` and `IContextualMenuBuilder`; to populate a contextual menu, use the UIToolkit way of registering a callback on a `ContextualMenuPopulateEvent` or, for classes deriving from `GraphElement`, override `BuildContextualMenu`.
- `IEditorDataModel` and `EditorDataModel`: use `EditorStateComponent` if you want to hold state that is related to a window or a window-asset combination.
- `IPluginRepository`
- `ICompilationResultModel`

### Changed

- All reducers in the `UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting` namespace have been moved to the `UnityEditor.GraphToolsFoundation.Overdrive` namespace.
- Reducers do not return the `State` anymore.
- Almost all reducers are undoable.
- Replace interface `IAction` by class `BaseAction`.
- `RemoveNodesAction` renamed to `BypassNodesAction`
- `ItemizeVariableNodeAction` and `ItemizeConstantNodeAction` renamed to `ItemizeNodeAction`
- `CreatePortalsOppositeAction` renamed to `CreateOppositePortalAction`
- `SplitEdgeAndInsertNodeAction` renamed to `SplitEdgeAndInsertExistingNodeAction`
- `UpdateConstantNodeActionValue` renamed to `UpdateConstantNodeValueAction`
- `ChangePlacematPositionAction` renamed to `ResizePlacematAction`
- `DropEdgeInEmptyRegionAction` renamed to `DeleteEdgeAction`
- `CreateNodeFromInputPortAction` and `CreateNodeFromOutputPortAction` renamed to `CreateNodeFromPortAction`
- Moved `GraphAssetModel` outside of `BasicModel` namespace since the asset is needed by GTF code.
- The namespaces `UnityEditor.GraphToolsFoundation.Overdrive.Models`,
  `UnityEditor.GraphToolsFoundation.Overdrive.GraphElements` and
  `UnityEditor.GraphToolsFoundation.Overdrive.SmartSearch` have all been merged into
  `UnityEditor.GraphToolsFoundation.Overdrive`.
- Simplify `IGraphModel`. Lots of methods moved out of the interface and made extension methods. Order of parameters has been somewhat standardized.
- `EdgePortalModel` is now backed by a `DeclarationModel` rather than a `VariableDeclarationModel`
- `Store.GetState()` is now `Store.State`
- Bug fix: the expanded/collapsed state of blackboard variables is persisted again.
- Blackboard UI adopts the same architecture as the `GraphView`, with a backing model
  (`IBlackboardGraphProxyElementModel`), using `IGraphElementParts` and the Setup/Build/Update lifecycle.
- Added a `priority` parameter to `GraphElementsExtensionMethodsCacheAttribute` to enable overriding of reducers
  (and all other extensions)
- `Store.GetState()` is now `Store.State`
- `State.CurrentGraphModel` is now `State.GraphModel`
- `IBlackboardGraphProxyElementModel` is now `IBlackboardGraphModel`.
- Moved `EditorDataModel.UpdateFlags`, `EditorDataModel.AddModelToUpdate` and `EditorDataModel.ClearModelsToUpdate` to `State`.
- `CompilationResultModel` is now `CompilationStateComponent`
- `TracingDataModel` is now `TracingStateComponent`
- Made `CreatePort` and `DeletePort` part ot the `IPortNode` interface.
- Made `AddInputPort` and `AddOutputPort` part of the `IInOutPortsNode` interface.
- Moved all variations of `AddInputPort` and `AddOutputPort` to extension methods.

## [0.5.0-preview.1] - 2020-09-25

### Added
- `ContextualMenuBuilder`, implementing the common functionality of `VseContextualMenuBuilder`
- `BlankPage`, implementing the common functionality of `VseBlankPage`
- `GtfoGraphView`, implementing the common functionality of `VseGraphView`
- `GtfoWindow`, implementing the common functionality of `VseWindow`

### Removed
- `VSNodeModel`
- `VSPortModel`
- `VSEditorDataModel`
- `VSPreferences`
- `VSTypeHandle`
- `VseContextualMenuBuilder`
- `VseBlankPage`
- `VseGraphView`
- `VseWindow`
- `DebugDisplayElement`
- `ISystemConstantNodeModel`
- `VseUIController`, now part of `GraphView`
- `VisualScripting.State`, now part of `Overdrive.State`
- `UICreationHelper`, now part of `PackageTransitionHelper`
- `IEditorDataModel` now part of `IGTFEditorDataModel`
- `IBuilder`
- `InputConstant`
- `InputConstantModel`
- `IStringWrapperConstantModel.GetAllInputs`
- `Stencil.Builder`
- `ConstantEditorExtensions.BuildStringWrapperEditor`
- `PortAlignmentType`
- `VisualScripting.Node`, merged into `CollapsibleInOutNode`
- `VisualScripting.Token`, merged into `TokenNode`
- All factory extension method in `GraphElementFactoryExtensions` except the one that creates ports.

### Changed
- A lot of classes have been moved outside the `VisualScripting` namespace.
- `IGTFStringWrapperConstantModel` was merged with `IStringWrapperConstantModel`
- When one drops an edge outside of a port, instead of deleting the edge, we now pop the searcher to create a new node.
- Rename `CreateCollapsiblePortNode` to `CreateNode`.
- Remove `GTF` from class and interface names. For example, `IGTFNodeModel` becomes `INodeModel`.
- `PackageTransitionHelper` becomes `AssetHelper.
- Base capabilities are no longer serialized with the `GraphToolsFoundation` prefix.

## [0.4.0-preview.1] - 2020-07-28

Drop-12

### Added

- Added an automatic spacing feature

#### API Changes

- `IGTFNodeModel` now has a `Tooltip` property.
- `IGTFEdgeModel` `FromPort` and `ToPort` are settable.
- Implementations of Unity event functions are now virtual.
- `GraphModel` basic implementation now serializes edges, sticky notes and placemats as references to enable the use of derived classes for them.
- `EditorDataModel.ElementModelToRename` was moved to `IGTFEditorDataModel`.
- Added default value for `IGTFGraphModel.CreateNode` `spawnFlag` parameter.
- Added support for `List<>` in `TypeSerializer`.

### Removed

- `PanToNodeAction`. Call `GraphView.PanToNode` instead.
- `RefreshUIAction`. Call `Store.ForceRefreshUI` instead.

### Fixed

- Fix issue when moving two nodes connected by edge with control points.
- Fix issue with auto placement of vertical ports with labels.
- Fix behavior of the default move and auto-placement reducers.

### Changed

- Changed the automatic alignment feature to consider connected nodes
- Extract basic model implementation from VisualScripting folder/namespace to GTFO.
- Split `IGTFNodeModel` and `IGTFPortModel` into finer grained interfaces.
- Add default implementation of some interfaces.
- Replace `IGraphModel` by `IGTFGraphModel`
- Replace `IVariableDeclarationModel` by `IGTFVariableDeclarationModel`
- Remove unused `BlackboardThisField` and `ThisNodeModel`
- Base Store class is now sealed. All derived store classes have been merged into the base class.
- Capabilities API modified to be more versatile
  - Capabilities are no longer interfaces but rather "simple" capabilities that can be added to models.
  - `IPositioned` has been renamed `IMovable`.
- Test models in `Tests\Editor\Overdrive\GraphElements\GraphViewTesting\BasicModel` and `Tests\Editor\Overdrive\GTFO\UIFromModelTests\Model` have been unified under `Tests\Editor\Overdrive\TestModels`

## [0.3.0-preview.1] - 2020-07-31

Drop 11

## [0.2.3-preview.3] - 2020-07-15

### Added

- Added dirty asset indicator in the window title
- Made VseWindow.Update virtual to enable derived classes to override it

### Fixed

- Fixed copy / paste issues with graph edges
- Mark graph asset dirty when edges are created or deleted
- Fixed resize issues with the sticky notes

## [0.2.3-preview.2] - 2020-06-18

## [0.2.3-preview.1] - 2020-06-18

## [0.2.3-preview] - 2020-06-12

## [0.2.2-preview.1] - 2020-05-06

### Changed

- Enabling vertical alignment in out-of-stack nodes w/ execution ports

## [0.2.1-preview.1] - 2020-03-20

### Added

- AnimationCurve constant editor
- Allow support of polymorphic edges in graph.
- Allow windows to decide if they handle specific asset types

### Changed

- Rework pills visual to tell apart read-only/write-only fields

### Fixed

- Fix graph dirty flag when renaming token

## [0.2.0-preview.4] - 2020-03-20

### Changed

- Updated com.unity.properties.ui@1.1.0-preview

## [0.2.0-preview.3] - 2020-02-26

### Fixed

- Fixed package warnings

## [0.2.0-preview.2] - 2020-02-21

### Changed

- Changed the handing of the MovedFrom attribute to accept assembly strings without version and fixed support for nested types

## [0.2.0-preview.1] - 2020-02-06

## [0.2.0-preview] - 2020-02-05

## [0.1.3-preview.1] - 2019-01-29

### Added

- Added support for migrating node types which have been moved or renamed

## [0.1.2-preview.10] - 2019-01-16

## [0.1.2-preview.9] - 2019-12-17

## [0.1.2-preview.8] - 2019-12-10

## [0.1.2-preview.7] - 2019-12-09

## [0.1.2-preview.6] - 2019-11-25

## [0.1.2-preview.5] - 2019-11-12

## [0.1.2-preview.4] - 2019-11-11

## [0.1.2-preview.3] - 2019-10-28

## [0.1.2] - 2019-08-15

## [0.1.1] - 2019-08-12

## [0.1.0] - 2019-08-01

### This is the first release of _Visual Scripting framework_.

_Short description of this release_
