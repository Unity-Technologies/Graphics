### Removed
- Removed support for plugins:
  - `PluginRepository`
  - `IPluginHandler`
  - `GraphProcessingOptions`
- Removed support for tracing:
  - `ActiveTracingCommand`
  - `TracingTimeline`
  - `TracingToolbar`
- Removed support for debugging:
  - `DebuggingPort`
  - `DebuggingErrorBadgeModel`
  - `DebuggingValueBadgeModel`
  - `DebugDataObserver`
  - `DebugInstrumentationHandler`
- Removed unused badge models and interfaces: `BadgeModel`, `ErrorBadgeModel` and `ValueBadgeModel`, and `IValueBadgeModel`.
- Removed unused badge `ValueBadge` and matching styling and UXML file.

### Changed
- `Stencil.CreateGraphProcessor` has been made protected. To access the graph processor of a stencil, use `Stencil.GraphProcessor`.
- Renamed `AutomaticGraphProcessor` to `AutomaticGraphProcessingObserver`.
