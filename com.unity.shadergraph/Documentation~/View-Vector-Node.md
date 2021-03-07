# View Vector Node

## Description

This node provides access to the mesh vertex or fragment's **View Direction** vector. It does not normalize any of the values it stores.
Select a **Space** to modify the coordinate space of the output value.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Vector 3 | None |View Direction for the Mesh Vertex/Fragment. |


## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Space | Dropdown | Object, View, World, Tangent | Selects coordinate space of View Direction to output. |
