# Sub-graph Node

## Description

Provides a reference to a [Sub-graph Asset](Sub-graph-Asset.md). All ports on the reference node are defined by the properties and outputs defined in the [Sub-graph Asset](Sub-graph-Asset.md). This is useful for sharing functionality between graphs or duplicating the same functionality within a graph.

## Sub-graph Nodes and Shader Stages

If a [Node](node.md) within a [Sub-graph](Sub-graph.md) specifies a [Shader Stage](Shader-Stage.md), such as how [Sample Texture 2D Node](Same-Texture-2D-Node.md) specifies the **fragment** [Shader Stage](Shader-Stage.md), then that entire [Sub-graph](Sub-graph.md) is now locked to that stage. As such a [Sub-graph Node](Sub-graph-Node.md) that references the graph will also be locked to that [Shader Stage](Shader-Stage.md).

Furthermore, when an [Edge](Edge.md) connected to an output [Port](Port.md) on a **Sub-graph Node** flows into a [Port](Port.md) on a [Master Node](Master-Node.md) that **Sub-graph Node** is now locked to the [Shader Stage](Shader-Stage.md) of that [Master Node](Master-Node.md) [Port](Port.md). 
