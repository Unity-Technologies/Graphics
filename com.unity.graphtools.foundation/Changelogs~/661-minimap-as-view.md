### Changed

- File `Minimap.uss` renamed to `MiniMapView.uss`.
- `MiniMap` now derives from `ModelUI` and is not a `GraphElement` anymore.

### Removed

- `Dragger`: now unused. No alternative provided.
- `GraphViewMinimapWindow.OnDestroy()`.
- `MiniMap.anchoredModifierClassName` and  `MiniMap.Anchored`: minimap is now always anchored.
- `MiniMap.windowedModifierClassName` and `MiniMap.Windowed`: minimap is now always windowed.
