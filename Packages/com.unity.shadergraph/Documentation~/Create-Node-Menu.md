# Add and connect nodes in a shader graph

You can add and connect nodes in a shader graph in different ways depending on your current task.

> [!NOTE]
> To add and connect nodes in a shader graph, you need to [create a shader graph asset](create-shader-graph.md) first and open the asset in the [Shader Graph window](Shader-Graph-Window.md).

## Add a node

To add a [node](Node.md) to your shader graph, follow these steps:

1. Open the **Create Node** menu through either of the following:
   
   * Select the [Shader Graph window](Shader-Graph-Window.md)'s workspace and press the **Spacebar**.
   * Right-click in the Shader Graph Window's workspace and select **Create Node**.

1. In the **Create Node** menu, browse or search for the desired node.
   
   The **Create Node** menu lists all nodes that are available in Shader Graph, categorized by their function. User-created [sub graphs](Sub-graph.md) are also available in the **Create Node** menu under **Sub Graph Assets**, or in a custom category that you define in the Sub Graph Asset.

1. Double-click on a node's name to add the corresponding node in the graph.

> [!NOTE]
> Use the **Create Node** menu search box to filter the listed nodes by name parts and synonyms based on industry terms. It provides autocomplete options and highlights matching text in yellow. You can press **Tab** to accept the predictive text.

## Connect node ports

To connect [ports](Port.md) between two existing [nodes](Node.md) or with the [master stack](Master-Stack.md), select and drag the desired port to the target.

The line resulting from that connection is called an [edge](Edge.md).

You can only connect an output port to an input port, or vice-versa, and you can't connect two ports of the same node together.

## Add and connect a node from an existing port

To connect a [port](Port.md) to a [node](Node.md) that doesn't exist yet and create that targeted node in the process, follow these steps:

1. Select and drag the desired port and release it in an empty area of the workspace.

1. In the **Create Node** menu, browse or search for the node you need to connect to the port you dragged out.
   
   The **Create Node** menu displays every node port available according to the [data types](Data-Types.md) compatible with the port you dragged out.

1. Double-click on a node port's name to add the corresponding node in the graph, with the two expected ports already connected­.

## Add a block node in the Master Stack

To add a new [block node](Block-Node.md) to the [master stack](Master-Stack.md), follow these steps:

1. Open the **Create Node** menu for the Master Stack context through either of the following:
   * Select the Master Stack's targeted context (**Vertex** or **Fragment**) and press the **Spacebar**.
   * Right-click in the Master Stack's targeted context area and select **Create Node**.

1. In the **Create Node** menu, browse or search for the desired block node.
   
   The **Create Node** menu displays all available blocks for the master stack based on the render pipelines in your project.
   
1. Double-click on a block node's name to add the corresponding block node in the Master Stack.

> [!NOTE]
> If the block that you add is not compatible with the current [graph settings](Graph-Settings-Tab.md), the block is deactivated until you configure the settings to support it.

## Additional resources

* [Nodes](Node.md)
* [Ports](Port.md)
* [Edges](Edge.md)
* [Master Stack](Master-Stack.md)
* [Block nodes](Block-Node.md)
