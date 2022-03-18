### Added
- `GraphViewDisplayMode` enum, a set of display modes for the `GraphView`.
  - In `Interactive` mode, the `GraphView` behavior is the same as it currently is.
  - In `NonInteractive` mode, no direct user interaction is possible with the `GraphView` and its content. The `GraphView` still reflect changes to the model as they occur.
- `GraphView.DisplayMode`, the display mode of the `GraphView` (readonly).
- `GraphView` now has a USS class reflecting its display mode.

### Changed
- `GraphView` constructor now has a `GraphViewDisplayMode` parameter.
- The Searcher (aka Item Library) now uses the same `GraphView` type as the tool, thus correctly displaying nodes with custom UI.
- `SearcherService.ShowInputToGraphNodes()` takes a type parameter to specify the type of `GraphView` to use to display preview nodes.
- `SearcherService.ShowOutputToGraphNodes()` takes a type parameter to specify the type of `GraphView` to use to display preview nodes.
- `SearcherService.ShowEdgeNodes()` takes a type parameter to specify the type of `GraphView` to use to display preview nodes.
- `SearcherService.ShowGraphNodes()` takes a type parameter to specify the type of `GraphView` to use to display preview nodes.
- `Stencil.GetSearcherAdapter()` takes a type parameter to specify the type of `GraphView` to use to display preview nodes.
- `GraphNodeSearcherAdapter` constructor takes a type parameter to specify the type of `GraphView` to use to display preview nodes.

### Removed
- `SearcherService.GraphView`
