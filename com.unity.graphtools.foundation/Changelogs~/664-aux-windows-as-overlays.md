### Changed

- In Unity 2022.2, GTF uses overlays and editor toolbars for its toolbars. The old toolbars are still used in Unity 2020.3.
    - `MainToolbar` has been split and replaced by `MainOverlayToolbar`, `BreadcrumbsToolbar`, `PanelsToolbar` and `OptionsMenuToolbar`.
    - `ErrorToolbar` is replaced by `ErrorOverlayToolbar`.

  As before, the content of the toolbars is determined by a toolbar provider class. In 2022.2, the provider should implements `IOverlayToolbarProvider`.

- `Stencil.GetToolbarProvider()` was moved to `BaseGraphTool`. If you need the stencil to determine the content of the toolbar, you can access it using `BaseGraphTool.ToolState.GraphModel.Stencil`.
- `MainToolbar.BuildOptionMenu()` was moved to `GraphView`.
- The `GraphViewEditorWindow.rootVisualElement` was previously assigned the USS name `gtfRoot`. It is now assigned the USS class `gtf-root`. Update your stylesheets by replacing `#gtfRoot` with `.gtf-root`.

### Added

- `BaseGraphTool.GetToolbarProvider(OverlayToolbar toolbar)` to get the toolbar providers for a toolbar (2022.2+ only).
- `IOverlayToolbarProvider`, the interface for the toolbar providers for overlay toolbars.

### Removed

- `GraphViewEditorWindow.WithSidePanel` (2022.2+ only). Consider this to now be `false`.
- `GraphViewEditorWindow.MainToolbar` (2022.2+ only). Use `EditorWindow.TryGetOverlay()` instead.
- `GraphViewEditorWindow.CreateMainToolbar()` (2022.2+ only). Instead, use an `IOverlayToolbarProvider` to customize the content of the toolbar. If you want to remove the toolbar, use `EditorWindow.TryGetOverlay()` and `EditorWindow.overlayCanvas.Remove()`.
- `GraphViewEditorWindow.CreateErrorToolbar()` (2022.2+ only). Instead, use an `IOverlayToolbarProvider` to customize the content of the toolbar. If you want to remove the toolbar, use `EditorWindow.TryGetOverlay()` and `EditorWindow.overlayCanvas.Remove()`.
