### Added

- `IEdgeModel.IsSibblingSelected` and `IEdgeModel.ShouldShowLabel` (the former affects the latter)
- `IGraphModel.ReorderEdges` and enum `ReorderType`, used in `ReorderEdgeCommand`
- enum `ZOrderMove` added for `ChangePlacematOrderCommand`
- `List<T>.ReorderElements` extension method to reorder elements in a list using a `ReorderType`
- `GraphModelExtensions.ReorderModels` and `GraphModelExtensions.MoveByZOrder`
- `IReorderableEdgesPort.ReorderEdge` and `PortModel.ReorderEdge`

### Removed

- `GraphModelExtensions.MoveForward` and `GraphModelExtensions.MoveBackward`, use `GraphModelExtensions.ReorderModels` or `GraphModelExtensions.MoveByZOrder`
- `GraphModelExtensions.MoveBefore` and `GraphModelExtensions.MoveAfter`, use `GraphModelExtensions.ReorderModels` or `GraphModelExtensions.MoveByZOrder`
- `MoveEdgeFirst`, `MoveEdgeUp`, `MoveEdgeDown`, `MoveEdgeLast` for `IReorderableEdgesPort` and `PortModel`. Use `PortModel.ReorderEdge` instead.

### Fixed

- Fix Edge Bubble not updating when reordering edges.
