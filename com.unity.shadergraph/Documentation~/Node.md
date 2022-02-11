# Node

## Description

A **Node** defines an input, output or operation on the Shader Graph, depending on its available [Ports](Port.md). A **Node** may have any number of input and/or output ports. You create a Shader Graph by connecting these ports with [Edges](Edge.md). A **Node** might also have any number of **Controls**, these are controls on the **Node** that do not have ports.

You can collapse a **Node** by clicking the **Collapse** button in the top-right corner of the **Node**. This will hide all unconnected ports.

For components of a **Node** see:
* [Port](Port.md)
* [Edge](Edge.md)

There are many available **Nodes** in Shader Graph. For a full list of all available **Nodes** see the [Node Library](Node-Library.md).

## Preview

Many nodes include a preview. This preview displays the main output value at that stage in the graph. Hide this preview with the Collapse control that displays when you hover over the node. You can also collapse and expand node previews via the Context Menu in the [Shader Graph Window](Shader-Graph-Window.md).  To configure the appearance of node previews, see [Preview Mode Control](Preview-Mode-Control.md).



## Context Menu

Right clicking on a **Node** will open a context menu. This menu contains many operations that can be performed on the **Node**. Note that when multiple nodes are selected, these operations will be applied to the entire selection.

| Item        | Description |
|:------------|:------------|
| Copy Shader | Copies the generated HLSL code at this stage in the graph to the clipboard |
| Disconnect All | Removes all edges from all ports on the **Node(s)** |
| Cut | Cuts selected **Node(s)** to the clipboard |
| Copy | Copies selected **Nodes(s)** to the clipboard |
| Paste | Pastes **Node(s)** in the clipboard |
| Delete | Deletes selected **Node(s)** |
| Duplicate | Duplicates selected **Node(s)** |
| Convert To Sub-graph | Creates a new [Sub-graph Asset](Sub-graph-Asset.md) with the selected **Node(s)** included |
| Convert To Inline Node | Converts a [Property Node](Property-Types.md) into a regular node of the appropriate [Data Type](Data-Types.md) |
| Convert To Property | Converts a **Node** into a new **Property** on the [Blackboard](Blackboard.md) of the appropriate [Property Type](Property-Types.md) |
| Open Documentation | Opens a new web browser to the selected **Nodes** documentation page in the [Node Library](Node-Library.md) |

## Color Mode
**Nodes** interact with the Shader Graph Window's Color Modes. Colors are displayed on nodes underneath the text on the node title bar. See [Color Modes](Color-Modes.md) for more information on available colors for nodes.

<image>
