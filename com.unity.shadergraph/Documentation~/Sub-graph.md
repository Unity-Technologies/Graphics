# Sub Graph

## Description

A Sub Graph is a special type of Shader Graph, which you can reference from inside other graphs. This is useful when you wish to perform the same operations multiple times in one graph or across multiple graphs. A Sub Graph differs from a Shader Graph in three main ways:
- [Properties](Property-Types) in the [Blackboard](Blackboard) of a Sub Graph define the input [Ports](Port) of a [Sub Graph Node](Sub-graph-Node) when you reference the Sub Graph from inside another graph.
- A Sub Graph has its own Asset type. For more information, including instructions on how to make a new Sub Graph, see [Sub Graph Asset](Sub-graph-Asset).
- A Sub Graph does not have a [Master Stack](Master-Stack). Instead, it has a [Node](Node) called **Output**.

For information about the components of a Sub Graph, see [Sub Graph Asset](Sub-graph-Asset).

## Output Node

![](images/SubGraph-Output-Node.png)

The Output Node defines the output ports of a [Sub Graph Node](Sub-graph-Node.md) when you reference the Sub Graph from inside another graph. To add and remove ports, use the [Custom Port Menu](Custom-Port-Menu) in the **Node Settings** tab of the [Graph Inspector](Internal-Inspector.md) by clicking on the Sub Graph Output node.

The preview used for Sub Graphs is determined by the first port of the Output Node. Valid [Data Types](Data-Types.md) for the first port are `Float`, `Vector 2`, `Vector 3`, `Vector 4`, `Matrix2`, `Matrix3`, `Matrix4`, and `Boolean`. Any other data type will produce an error in the preview shader and the Sub Graph will become invalid.

## Sub Graphs and shader stages
If a Node within a Sub Graph specifies a shader stage (for example, like how the [Sample Texture 2D Node](Sample-Texture-2D-Node.md) specifies the **fragment** shader stage), the Editor locks the entire Sub Graph to that stage. You cannot connect any Nodes that specify a different shader stage to the Sub Graph Output Node, and the Editor locks any Sub Graph Nodes that reference the graph to that shader stage.

## Sub Graphs and Keywords
[Keywords](Keywords) that you define on the [Blackboard](Blackboard) in a Sub Graph behave similarly to those in regular Shader Graphs. When you add a Sub Graph Node to a Shader Graph, Unity defines all Keywords in that Sub Graph in the Shader Graph as well, so that the Sub Graph works as intended.

To use a Sub Graph Keyword inside a Shader Graph, or to expose that Keyword in the Material Inspector, copy it from the Sub Graph to the Shader Graph's Blackboard.
