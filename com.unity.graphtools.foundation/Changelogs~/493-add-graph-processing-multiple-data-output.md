### Added
- `Stencil.GetGraphProcessorContainer()`, to get the GraphProcessorContainer. It contains a list of graph processors.
- `Stencil.CreateGraphProcessors()`, to add graph processors to the GraphProcessorContainer. The Stencil can have more than one graph processor now.
- `GraphProcessingStateComponent.RawErrors`, a list of all `GraphProcessingError` from all `GraphProcessingStateComponent.RawResults`.

### Removed
- `Stencil.CreateGraphProcessor()`: use Stencil.CreateGraphProcessors() instead.

### Changed
- `Stencil.CanCreateVariableInGraph()` changed to `Stencil.CanAllowVariableInGraph()`.
- `GraphProcessingStateComponent.SetResults()` now takes a `IReadOnlyList<GraphProcessingResult>` as its first parameter, instead of a single `GraphProcessingResult`.
- `GraphProcessingStateComponent.RawResults` is now a `IReadOnlyList<GraphProcessingResult>`, instead of a single `GraphProcessingResult`.
- `GraphProcessingHelper.ProcessGraph()` now returns a `IReadOnlyList<GraphProcessingResult>`, instead of a single `GraphProcessingResult`.
- `GraphProcessingHelper.GetErrors()` now takes a `IReadOnlyList<GraphProcessingResult>` as its second parameter, instead of a single `GraphProcessingResult`.
