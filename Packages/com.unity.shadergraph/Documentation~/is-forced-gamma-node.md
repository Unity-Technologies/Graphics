---
uid: is-forced-gamma-node
---

# Is Forced Gamma node

[!include[](include_note_uitk.md)]

The Is Forced Gamma node outputs whether the UI Toolkit renderer is currently rendering the element with Force Gamma Rendering enabled. Use this node to create custom logic that behaves differently when the renderer forces gamma-space compositing, for example to skip a gamma conversion the renderer already applies.

Force Gamma Rendering is a [Panel Settings](xref:uie-runtime-panel-settings) option that keeps a panel in gamma color space even in a Linear Color Space project. Unity enables it automatically when a panel renders to a Render Texture.

## Ports

| Name | Direction | Type    | Description                                                                     |
|------|-----------|---------|----------------------------------------------------------------------------------|
| Out  | Output    | Boolean | True when Force Gamma Rendering is enabled for the UI element. |

## Additional resources

- [Panel Settings](xref:uie-runtime-panel-settings)
- [Create and apply custom filters](xref:uie-uss-custom-filters)
- [Render Type node](xref:render-type-node)