# Add inputs and outputs to a Sub Graph

To pass data in and out of a [Sub Graph](Sub-graph.md), create input and output ports.

## Create an input port

To create an input port, add a property to the blackboard of the Sub Graph. Follow these steps:

1. Open the Sub Graph asset.
2. In the Blackboard, click the **+** button and select the type of property you want to add.
3. Drag the property from the Blackboard into the graph area to create a Property Node.

When you add the Sub Graph to a shader graph, the property appears as an input port on the Sub Graph Node.

## Create an output port

To create an output port, add a port to the **Output** Node of the Sub Graph. Follow these steps:

1. Open the Sub Graph asset.
1. Select the **Output** node.
1. In the **Graph Inspector** window, select the **Node Settings** tab.
1. Under **Inputs**, select the Add (**+**) button.
1. Use the dropdown to select the output type.

When you add the Sub Graph to a shader graph, the property appears as an output port on the Sub Graph node.

## Avoid preview errors

The Preview window uses the first output port to generate the preview image. To avoid an error, ensure the first output port is one of the following data types:

- `Boolean`
- `Float`
- `Vector 2` 
- `Vector 3`
- `Vector 4`
- `Matrix 2`
- `Matrix 3`
- `Matrix 4`

For more information, refer to [Custom Port Menu](Custom-Port-Menu.md).

## Additional resources

- [Branch On Input Connection node](Branch-On-Input-Connection-Node.md)
