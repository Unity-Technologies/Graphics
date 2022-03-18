### Changed

- *Beware: `IModelView` still exists but it is not the same interface: the current `IModelView` was renamed to `IRootView` and `IModelUI` renamed to `IModelView`*.
- `IModelView` renamed to `IRootView`.
- `BaseView` renamed to `RootView`.
- `IModelUI` renamed to `IModelView`.
- `ModelUI` renamed to `ModelView`.
- `BaseModelUIPart` renamed to `BaseModelViewPart`.
- `ModelUIPartList` renamed to `ModelViewPartList`.
- `IModelUIContainer` renamed to `IModelViewContainer`.
- `IModelUIPart` renamed to `IModelViewPart`.
- `IUIContext` renamed to `IViewContext`.
- `ModelUIFactory` renamed to `ModelViewFactory`.
- `UIForModel` renamed to `ViewForModel`.
- `UIForModel.GetUI()` renamed to `ViewForModel.GetView()`.
- `UIForModel.GetAllUIs()` renamed to `ViewForModel.GetAllViews()`.
- `GraphView.GraphViewState`, `GraphView.GraphModelState` and `GraphView.SelectionState` have been encapsulated into `GraphViewModel`.
- `GraphView.Update()` renamed to `GraphView.UpdateFromModel()`.
- `BlackboardView.ParentGraphView`, `BlackboardView.ViewState`, `BlackboardView.GraphModelState` and `BlackboardView.SelectionState` have been encapsulated into `BlackboardViewModel`.
- `BlackboardView.RebuildUI()` renamed to `BlackboardView.BuildUI()`.
- `MiniMapView.ParentGraphView` has been encapsulated into `MiniMapViewModel`.
- `MiniMapView.RebuildUI()` renamed to `MiniMapView.BuildUI()`.
- `ModelInspectorView.ModelInspectorState` has been encapsulated into `ModelInspectorObserver`.

### Added

- `IBaseModelView`, parent of `IRootView` and `IModelView`.

### Removed

- `BlackboardUpdateObserver`: replaced by `ModelViewUpdater`.
- `GraphView.UpdateObserver`: replaced by `ModelViewUpdater`.
- `MiniMapUpdateObserver`: replaced by `ModelViewUpdater`.
- `ModelInspectorView.ModelInspectorObserver`: replaced by `ModelViewUpdater`.
