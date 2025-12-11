# Create a Sub Graph

To perform the same operations multiple times within a single shader graph or across multiple shader graphs, create a [Sub Graph](Sub-graph.md). 

## Create a Sub Graph from existing nodes

To create a [Sub Graph](Sub-graph.md) from an existing set of shader graph nodes, follow these steps:

1. In the Shader Graph window, select the nodes you want to include in the Sub Graph.
2. Right-click on one of the selected nodes to open the context menu.
3. Select **Convert To Sub-graph**.

Unity creates a Shader Graph asset in the Project window. To edit the Sub Graph, double-click the asset.

**Note:** If a node in a Sub Graph specifies a [shader stage](Shader-Stage.md), the Sub Graph can only include nodes that work with or specify the same shader stage.

## Create an empty Sub Graph

To create an empty Sub Graph, in the Project window, right-click and select **Create** > **Shader** > **Sub Graph**.

## Additional resources

- [Change the behavior of a Sub Graph with a dropdown](Change-Behaviour-Sub-Graph-Dropdown.md)
- [Custom Function Node](Custom-Function-Node.md)
