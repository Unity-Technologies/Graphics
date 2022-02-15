# View Direction Node

## Description

Provides access to the mesh vertex or fragment's **View Direction** vector. This is the vector from the vertex or fragment to the camera. Select a **Space** to modify the coordinate space of the output value.

Prior to version 11.0, the **View Direction Node** works differently in HDRP than in URP. In URP, it only stored Object space vectors normalized. HDRP stores all vectors normalized.

From 11.0 onwards, this node stores all vectors normalized in both the **High-Definition Render Pipeline** and the **Universal Render Pipeline**.

If you want to keep using the old behavior in URP outside of object space, replace this node with a [View Vector Node](View-Vector-Node.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Vector 3 | None | View Vector for the Mesh Vertex/Fragment. |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Space | Dropdown | Object, View, World, Tangent | Selects coordinate space of **View Direction** to output. |
