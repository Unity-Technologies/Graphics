# Create Node Menu

## Description

Use the **Create Node Menu** to create [nodes](Node.md) in Shader Graph. To open the **Create Node Menu**, either right-click on the workspace in the [Shader Graph Window](Shader-Graph-Window.md) and select **Create Node**, or press the spacebar.

At the top of the **Create Node Menu** is a search bar. To search for a node, type any part of its name in the search field. The search box gives you autocomplete options, and you can press Tab to accept the predictive text. It highlights matching text in yellow.

The **Create Node Menu** lists all nodes that are available in Shader Graph, categorized by their function. User-created [Sub Graphs](Sub-graph.md) are also available in the **Create Node Menu** under **Sub Graph Assets**, or in a custom category that you define in the Sub Graph Asset.

To add a node to the workspace, double-click it in the **Create Node Menu**.

### Contextual Create Node Menu

A contextual **Create Node Menu** filters the available nodes, and only shows those that use the [Data Type](Data-Types.md) of a selected edge. It lists every available [Port](Port.md) on nodes that match that Data Type.

To open a contextual **Create Node Menu**, click and drag an [Edge](Edge.md) from a Port, and then release it in an empty area of the workspace.

### Master Stack Create Node Menu
To add a new [Block Node]() to the [Master Stack](), either right click and select **Create Node** or press spacebar with the stack selected.

The **Create Node Menu** will display all available blocks for the master stack based on the render pipelines in your project. Any block can be added to the master stack via the **Create Node Menu**. If the block added is not compatible with the current Graph settings, the block will be disabled until the settings are configured to support it.
