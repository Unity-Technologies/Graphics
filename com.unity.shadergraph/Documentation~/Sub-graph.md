# Sub Graph

## Description

A **Sub Graph** is a special type of [Shader Graph](Shader-Graph.md). It is used to create graphs that can be referenced inside other graphs. This is useful when you wish to perform the same operations multiple times in one graph or across multiple graphs. A **Sub Graph** differs from a [Shader Graph](Shader-Graph.md) in 3 main ways:
- [Properties](Property-Types.md) in the [Blackboard](Blackboard.md) of a **Sub Graph** define the input [Ports](Port.md) of a [Sub Graph Node](Sub-graph-Node.md) when the **Sub Graph** is referenced in another graph. 
- A **Sub-graph** has its own asset type. For more information, including how to make a new **Sub-graph**, see [Sub-graph Asset](Sub-graph-Asset.md).
- A **Sub Graph** does not have a [Master Node](Master-Node.md). Instead it has a [Node](Node.md) called **Output**. For more information see below.

For components of a **Sub-graph** see:
* [Sub-graph Asset](Sub-graph-Asset.md)

## Output Node

The **Output** [Node](Node.md) defines the output [Ports](Port.md) of a [Sub Graph Node](Sub-graph-Node.md) when the **Sub Graph** is referenced in another graph. You can add and remove [Ports](Port.md) using the [Custom Port Menu](Custom-Port-Menu) available via the cog icon in the top right corner of the node.

## Sub Graphs and Shader Stages

If a [Node](Node.md) within a **Sub Graph** specifies a shader stage, such as how [Sample Texture 2D Node](Sample-Texture-2D-Node.md) specifies the **fragment** shader stage, then that entire **Sub Graph** is now locked to that stage. No [Nodes](Node.md) that specify a different shader stage will be able to be connected to the **Sub-graph Output Node** and any [Sub Graph Nodes](Sub-graph-Node.md) that reference the graph will also be locked to that shader stage.
