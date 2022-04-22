### Removed

- `AssetStateComponent`, `AssetViewStateComponent`, `ViewStateComponent`: replaced by `PersistedStateComponent`.
- `IAssetStateComponent`, `IAssetViewStateComponent`, `IViewStateComponent`: replaced by `IPersistedStateComponent`.
- `PersistedState.GetOrCreateViewStateComponent`, `PersistedState.GetOrCreateAssetStateComponent`, `PersistedState.GetOrCreateAssetViewStateComponent`: replaced by `PersistedState.GetOrCreatePersistedStateComponent`.
- `PersistedStateComponentHelpers.SaveAndLoadAssetStateForAsset`, `PersistedStateComponentHelpers.SaveAndLoadAssetViewStateForAsset`: replaced by `PersistedStateComponentHelpers.SaveAndLoadPersistedStateForAsset`.

### Changed:

- `StateComponent<T>` is not `IDisposable` anymore. Move code from `Dispose()` to `StateComponent.OnRemovedFromState()`.
- `BlackboardView`, `BlackboardViewModel` are not `IDisposable` anymore.
