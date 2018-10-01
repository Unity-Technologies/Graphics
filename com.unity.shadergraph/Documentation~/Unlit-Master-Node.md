## Description

A [Master Node](Master-Node.md) for unlit materials.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Color      | Input | Vector 3 | None | Defines material's color value. Expected range 0 - 1. |
| Alpha      | Input | Vector 1 | None | Defines material's alpha value. Used for transparency and/or alpha clip. Expected range 0 - 1.  |
| Alpha Clip Threshold      | Input | Vector 1 | None | Fragments with an alpha below this value will be discarded. Requires a node connection. Expected range 0 - 1. |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Workflow      | Dropdown | Metallic, Specular | Defines workflow mode for the material |
| Surface      | Dropdown | Opaque, Transparent | Defines if the material is transparent |
| Blend      | Dropdown | Alpha, Premultiply, Additive, Multiply | Defines blend mode of a transparent material |
| Two Sided      | Toggle | True, False | If true both front and back faces of the mesh are rendered |