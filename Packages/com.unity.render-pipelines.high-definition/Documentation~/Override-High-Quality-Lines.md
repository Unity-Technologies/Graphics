# High Quality Line Rendering Volume Override reference

Refer to [High quality line rendering](high-quality-line-rendering.md) for more information.

## Properties

| **Property** || **Description** |
|--|--|--|
| **Composition Mode** || Determine when in the render pipeline lines are rendered into the main frame. |
|| **Before Color Pyramid** | Use this setting if you want lines to appear in transparency effects. |
|| **After Temporal Anti-Aliasing** | Use this setting if you use [Temporal Anti-Aliasing](Anti-Aliasing.md#temporal-antialiasing-taa), so HDRP uses a stable depth buffer. |
|| **After Depth Of Field** | Use this setting if the lines will be in focus against a blurrier scene. |
| **Cluster Count**          || Set the number of clusters in a tile, between the camera's near and far plane. |
| **Sorting Quality**        || Set the quality of the line rendering, which affects the maximum number of segments HDRP can sort within a cluster. The options are **Low**, **Medium**, **High** and **Ultra**. The higher the quality, the more memory high quality line rendering uses. |
| **Tile Opacity Threshold** || Set the opacity value that qualifies as an opaque tile. A threshold lower than 1.0 can greatly improve performance. |
