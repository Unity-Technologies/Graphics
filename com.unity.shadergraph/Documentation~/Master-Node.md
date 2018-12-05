# Master Node

## Description

The **Master Node** is a special kind of [Node](Node.md). It is the end point of a [Shader Graph](Shader-Graph.md) that defines the final surface appearance of the shader. Your [Shader Graph](Shader-Graph.md) should always contain one, and only one, **Master Node**. The **Master Node** will automatically handle the conversion of a shader between different **Scriptable Render Pipelines** if there is an available backend.

For a full list of all available **Master Nodes** see [Master Nodes](Master-Nodes.md) in the [Node Library](Node-Library.md).

## Controls

All **Master Nodes** share a common set of **Controls** although certain **Master Nodes** may include more. See [Master Nodes](Master-Nodes.md) for special **Controls** on different **Master Nodes**.