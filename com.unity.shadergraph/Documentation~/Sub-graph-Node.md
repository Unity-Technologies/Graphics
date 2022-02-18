# Subgraph node

## Description

Provides a reference to a [Subgraph Asset](Sub-graph-Asset.md). All ports on the reference node are defined by the properties and outputs defined in the Subgraph Asset. This is useful for sharing functionality between graphs or duplicating the same functionality within a graph.

The preview used for a Subgraph Node is determined by the first port of that [Subgraph](Sub-graph.md) Output node. Valid [Data Types](Data-Types.md) for the first port are `Float`, `Vector 2`, `Vector 3`, `Vector 4`, `Matrix2`, `Matrix3`, `Matrix4`, and `Boolean`. Any other data type will produce an error in the preview shader and the Subgraph will become invalid.

## Subgraph Nodes and Shader Stages

If a [Node](Node.md) within a Subgraph specifies a [Shader Stage](Shader-Stage.md), such as how [Sample Texture 2D Node](Sample-Texture-2D-Node.md) specifies the **fragment** Shader Stage, then that entire Subgraph) is now locked to that stage. As such a Subgraph node that references the graph will also be locked to that Shader Stage.

Furthermore, when an [Edge](Edge.md) connected to an output [Port](Port.md) on a **Subgraph Node** flows into a port on the [Master Stack](Master-Stack.md) that **Subgraph Node** is now locked to the Shader Stage of that [Block Node](Block-Node.md) in the Master Stack.
