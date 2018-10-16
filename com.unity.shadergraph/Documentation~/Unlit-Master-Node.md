# Unlit Master Node

## Description

A [Master Node](Master-Node.md) for unlit materials.

## Ports

| Name        | Direction           | Type  | Stage | Binding | Description |
|:------------ |:-------------|:-----|:-----|:---|:---|
| Position      | Input | Vector 3 | Vertex | None | Defines the absolute object space vertex position per vertex |
| Color      | Input | Vector 3 | Fragment | None | Defines material's color value. Expected range 0 - 1. |
| Alpha      | Input | Vector 1 | Fragment | None | Defines material's alpha value. Used for transparency and/or alpha clip. Expected range 0 - 1.  |
| Alpha Clip Threshold      | Input | Vector 1 | Fragment | None | Fragments with an alpha below this value will be discarded. Requires a node connection. Expected range 0 - 1. |

## Material Options

**Unlit Master Node** material options can be accessed by clicking the cog icon in the top right corner of the **Unlit Master Node**. 

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Surface      | Dropdown | Opaque, Transparent | Defines if the material is transparent |
| Blend      | Dropdown | Alpha, Premultiply, Additive, Multiply | Defines blend mode of a transparent material |
| Two Sided      | Toggle | True, False | If true both front and back faces of the mesh are rendered |