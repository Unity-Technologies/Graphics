### Added
- `IGraphAssetModel.IsContainerGraph`, a virtual method to verify if a graph is a container graph or not.

### Removed
- `IGraphAssetModel.GraphAssetType`: use `IGraphAssetModel.IsContainerGraph` to know if a graph is a container graph or an asset graph.
- `GraphAssetType` enum: No need for this enum anymore as there is only Asset and Container graph types now.

### Changed
- The signature for graph asset creation methods have changed, they no longer require a `GraphAssetType` parameter.
