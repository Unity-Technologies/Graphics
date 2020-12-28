# Sub Graph Node

## Description

Provides a reference to a [Sub Graph Asset](Sub-graph-Asset.md). All ports on the reference node are defined by the properties and outputs defined in the [Sub Graph Asset](Sub-graph-Asset.md). This is useful for sharing functionality between graphs or duplicating the same functionality within a graph.

The preview used for a Sub Graph Node is determined by the first port of that [Sub Graph](Sub-graph.md) Output Node. Valid [Data Types](Data-Types.md) for the first port are `Float`, `Vector 2`, `Vector 3`, `Vector 4`, `Matrix2`, `Matrix3`, `Matrix4`, and `Boolean`. Any other data type will produce an error in the preview shader and the [Sub Graph](Sub-graph.md) will become invalid. 

## Sub Graph Nodes and Shader Stages

If a [Node](Node.md) within a [Sub Graph](Sub-graph.md) specifies a [Shader Stage](Shader-Stage.md), such as how [Sample Texture 2D Node](Sample-Texture-2D-Node.md) specifies the **fragment** [Shader Stage](Shader-Stage.md), then that entire [Sub Graph](Sub-graph.md) is now locked to that stage. As such a [Sub Graph Node](Sub-graph-Node.md) that references the graph will also be locked to that [Shader Stage](Shader-Stage.md).

Furthermore, when an [Edge](Edge.md) connected to an output [Port](Port.md) on a **Sub Graph Node** flows into a [Port](Port.md) on a [Master Node](Master-Node.md) that **Sub Graph Node** is now locked to the [Shader Stage](Shader-Stage.md) of that [Master Node](Master-Node.md) [Port](Port.md). 
