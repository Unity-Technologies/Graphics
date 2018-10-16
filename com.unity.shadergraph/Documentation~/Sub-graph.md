# Sub-graph

## Description

A **Sub-graph** is a special type of [Shader Graph](Shader-Graph.md). It is used to create graphs that can be referenced inside other graphs. This is useful when you wish to perform the same operations multiple times in one graph or across multiple graphs. A **Sub-graph** differs from a [Shader Graph](Shader-Graph.md) in 3 main ways:
- [Properties](Property-Types.md) in the [Blackboard](Blackboard.md) of a **Sub-graph** define the input [Ports](Ports.md) of a [Sub-graph Node](Sub-graph-Node.md) when the **Sub-graph** is referenced in another graph.
- A **Sub-graph** has its own asset type. For more information, including how to make a new **Sub-graph**, see [Sub-graph Asset](Sub-graph-Asset.md).
- A **Sub-graph** does not have a [Master Node](Master-Node.md). Instead it has a [Node](Node.md) called **SubGraphOutputs**. For more information see below.

For components of a **Sub-graph** see:
* [Sub-graph Asset](Sub-graph-Asset.md)

## SubGraphOutputs

The **SubGraphOutputs** [Node](Node.md) defines the output [Ports](Ports.md) of a [Sub-graph Node](Sub-graph-Node.md) when the **Sub-graph** is referenced in another graph. You can add and remove [Ports](Ports.md) using the **Add Slot** and **Remove Slot** buttons.

## Sub-graphs and Shader Stages

If a [Node](node.md) within a **Sub-graph** specifies a shader stage, such as how [Sample Texture 2D Node](Same-Texture-2D-Node.md) specifies the **fragment** shader stage, then that entire **Sub-graph** is now locked to that stage. No [Nodes](Node.md) that specify a different shader stage will be able to be connected to the **Sub-graph Output Node** and any [Sub-graph Nodes](Sub-graph-Node.md) that reference the graph will also be locked to that shader stage.
