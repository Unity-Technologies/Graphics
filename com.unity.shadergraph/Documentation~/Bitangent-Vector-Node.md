# Bitangent Node

## Description

Provides access to the mesh vertex or fragment's **Bitangent Vector**, depending on the effective [Shader Stage](Shader-Stage.md) of the graph section the [Node](Node.md) is part of. The coordinate space of the output value can be selected with the **Space** dropdown parameter.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Vector 3 | None | **Bitangent Vector** for the Mesh Vertex/Fragment. |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Space | Dropdown | Object, View, World, Tangent | Selects coordinate space of **Bitangent Vector** to output. |