# View Direction Node

## Description

Provides access to the mesh vertex or fragment's **View Direction** vector. This is the vector from the vertex or fragment to the camera. The coordinate space of the output value can be selected with the **Space** dropdown parameter.

NOTE: In versions prior to 11.0, the **View Direction** vector was not normalized in the **Universal Render Pipeline**. Version 11.0 changed this behavior, and this vector is now normalized in both the **High-Definition Render Pipeline** and the **Universal Render Pipeline**. To mimic old behavior, you can use the [Position Node](Position-Node.md) in **World** space and subtract the **Position** output of the [Camera Node](Camera-Node.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Vector 3 | None | **View Direction** for the Mesh Vertex/Fragment. |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Space | Dropdown | Object, View, World, Tangent | Selects coordinate space of **View Direction** to output. |
