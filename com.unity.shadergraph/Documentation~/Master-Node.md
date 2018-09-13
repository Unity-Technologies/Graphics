## Description

The **Master Node** is a special kind of [Node](Node.md). It is the end point of a [Shader Graph](Shader-Graph.md) that defines the final surface appearance of the shader. Your shader should always contain one, and only one, **Master Node**. The **Master** node will automatically handle the conversion of a shader between different **Scriptable Render Pipelines** if there is an available backend.

For a full list of all available **Master Nodes** see [Master Nodes](Master-Nodes.md) in the [Node Library](Node-Library.md).

## Parameters

All **Master Nodes** share a common set of **Parameters** although certain **Master Nodes** may include more. See [Master Nodes](Master-Nodes.md) for special **Parameters** on different **Master Nodes**.

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Surface      | Dropdown | Opaque, Transparent | Defines if the material is transparent |
| Blend      | Dropdown | Alpha, Premultiply, Additive, Multiply | Defines blend mode of a transparent material |
| Two Sided      | Toggle | True, False | If true both front and back faces of the mesh are rendered |