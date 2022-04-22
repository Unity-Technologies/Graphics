### Changed

- `IPersistedStateComponent.AssetKey` renamed to `IPersistedStateComponent.GraphKey`.
- `PersistedState.MakeAssetKey` renamed to `MakeAssetKey.MakeGraphKey`.
- `PersistedStateComponent<>.AssetKey` renamed to `PersistedStateComponent<>.GraphKey`.
- `PersistedStateComponentHelpers.SaveAndLoadPersistedStateForAsset` renamed to `PersistedStateComponentHelpers.SaveAndLoadPersistedStateForGraph`.
- `ToolStateComponent.StateUpdater.LoadGraphAsset` renamed to `ToolStateComponent.StateUpdater.LoadGraph`.
- `ToolStateComponent.StateUpdater.AssetChangedOnDisk` renamed to `ToolStateComponent.StateUpdater.GraphChangedExternally`.
- `SelectionStateComponent.GraphAssetLoadedObserver` renamed to `SelectionStateComponent.GraphLoadedObserver`.
- `SelectionStateComponent.StateUpdater.SaveAndLoadStateForAsset` renamed to `SelectionStateComponent.StateUpdater.SaveAndLoadStateForGraph`.
- `OpenedGraph.GetGraphAssetModel` renamed to `OpenedGraph.GetGraphAsset`.
- `OpenedGraph.GetGraphAssetModelPath` renamed to `OpenedGraph.GetGraphAssetPath`.
- `OpenedGraph.GraphModelAssetGuid` renamed to `OpenedGraph.GraphAssetGuid`.
- `IGraphAssetModel` renamed to `IGraphAsset`.
- `GraphAssetModel` renamed to `GraphAsset, implements ISerializedGraphAsset`.
- `IGraphModel.AssetModel` renamed to `IGraphModel.Asset`.
- `IGraphModel.OnLoadGraphAsset` renamed to `IGraphModel.OnLoadGraph`.
- `ISubgraphNodeModel.SubgraphAssetModel` renamed to `ISubgraphNodeModel.SubgraphModel`.
- `LoadGraphAssetCommand` renamed to `LoadGraphCommand`.
- `UnloadGraphAssetCommand` renamed to `UnloadGraphCommand`.
- `SaveAllButton` renamed to `SaveButton, saves only the displayed graph.`.
- `GraphAssetCreationHelpers<`> is not generic anymore.
- `GraphAssetCreationHelpers.PromptToCreate` renamed to `GraphAssetCreationHelpers.PromptToCreateGraphAsset`.
- `BlackboardGraphAssetLoadedObserver` renamed to `BlackboardGraphLoadedObserver`.
- `BlackboardViewStateComponent.StateUpdater.SaveAndLoadStateForAsset` renamed to `BlackboardViewStateComponent.StateUpdater.SaveAndLoadStateForGraph`.
- `GraphModelStateComponent.StateUpdater.SaveAndLoadStateForAsset` renamed to `GraphModelStateComponent.StateUpdater.SaveAndLoadStateForGraph`.
- `GraphViewStateComponent.StateUpdater.SaveAndLoadStateForAsset` renamed to `GraphViewStateComponent.StateUpdater.SaveAndLoadStateForGraph`.
- `ModelInspectorGraphAssetLoadedObserver` renamed to `ModelInspectorGraphLoadedObserver`.
- `ModelInspectorStateComponent.StateUpdater.SaveAndLoadStateForAsset` renamed to `ModelInspectorStateComponent.StateUpdater.SaveAndLoadStateForGraph`.

### Added

- `IGraphModel.IsContainerGraph`
- `IGraphModel.CanBeSubgraph`
- `ISerializedGraphAsset`, interface for graph assets that can be saved to a file.
- `IGraphTemplate.GraphFileExtension`

### Removed

- `ToolStateComponent.AssetModel`.
- `NodeModel.Stencil`: use `INodeModel.GraphModel.Stencil` instead.
- `GraphAssetModelExtensions.GetPath(this IGraphAssetModel self)`.
- `GraphAssetModelExtensions.GetFileId(this IGraphAssetModel self)`.
- `GraphModelExtensions.CheckIntegrity` : use `IGraphModel.CheckIntegrity` instead.
- `IGraphAssetModel.FriendlyScriptName` : use `IGraphModel.GetFriendlyScriptName` instead.
- `IGraphAssetModel.IsContainerGraph` : use `IGraphModel.IsContainerGraph` instead.
- `IGraphAssetModel.CanBeSubgraph` : use `IGraphModel.CanBeSubgraph` instead.
- `IGraphAssetModel.UndoRedoPerformed`.
- `IGraphAssetModelHelper.Create`.
- `IGraphAssetModelHelper.ValidateObject`.
- `IGraphElementModel.AssetModel` : use `IGraphElementModel.GraphModel.Asset` instead.
- `GraphAssetCreationHelpers<>` : use the non generic `GraphAssetCreationHelpers` instead.
- `GraphModelStateComponent.AssetModel`.
- `LoadGraphAssetCommand.AssetPath` and `LoadGraphAssetCommand.FileId`.
- `OnboardingProvider.k_AssetExtension`.
- `CsoTool.Parent`.
