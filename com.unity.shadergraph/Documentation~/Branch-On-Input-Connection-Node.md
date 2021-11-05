# Branch On Input Connection Node

The Branch On Input Connection node allows you to change the behavior of a Sub Graph based on the connected state of an input property in the parent Shader Graph. Shader Graph determines whether the property in the parent Shader Graph is connected, or not connected, and chooses a value to use as an output based on that connection state.

![An image of the Graph window, showing the Branch on Input Connection node.](images/sg-branch-on-input-connection-node.png)

Shader Graph uses two ports when it determines the node's connection state:

- The Branch On Input Connection node's **Input** port.

- The Sub Graph node's corresponding Property port in the parent Shader Graph. For more information on Sub Graph nodes, see [Sub Graph Node](Sub-graph-Node).

The Branch On Input Connection node's functionality is based on the [Branch Node](Branch-Node).

> [!NOTE]
> You can't use the Branch On Input Connection node with a Streaming Virtual Texture Property. For more information on Streaming Virtual Texturing, see [Using Streaming Virtual Texturing in Shader Graph](https://docs.unity3d.com/Documentation/Manual/svt-use-in-shader-graph.html).

## Create Node menu category

The Branch On Input Connection node is under the **Utility** &gt; **Logic** category in the Create Node menu. You can only use it in a Shader Sub Graph.

To use the Branch On Input Connection node in a Sub Graph:

1. Open the Sub Graph where you want to add a Branch On Input Connection node.

2. In the Blackboard, do one of the following:

    - To add a new property, select **Add** (+), then select a property type from the menu. Enter a name for your new property and press Enter. Then, select your property in the Blackboard and drag it onto your graph to create a Property node.

    - Select an existing property and drag it onto your graph to create a Property node.

3. With your Property node selected, in the Graph Inspector, enable **Use Custom Binding**.

    > [!NOTE]
    > If you disable **Use Custom Binding**, you can't connect your Property node to the Branch On Input Connection node. If you've already made a connection, the Unity Editor breaks the connection and displays a warning on the node.

4. In the **Label** field, enter the label for the default value that should display on the Sub Graph node's port binding. For more information on port bindings, see [Port Bindings](Port-Bindings.md).

5. Press Spacebar or right-click and select **Create Node**. Search for or locate the **Branch On Input Connection** node in the Create Node Menu, select the node, then click again or press Enter to add it to your Sub Graph.

6. Select the output port on your Property node, and drag its new connection to the Branch On Connection node's **Input** port.

7. Connect a node to the **Connected** port to specify the value Shader Graph should use when the **Input** port is connected on the Sub Graph node in the parent graph. Connect another node to the **NotConnected** port to specify the value that Shader Graph should use when the **Input** port isn't connected.

8. Connect any valid node to the **Output** port to specify how Shader Graph should use the **Connected** or **NotConnected** value in your shader.


## Compatibility

The Branch On Input Connection node is compatible with all render pipelines.

## Ports

| **Name**     | **Direction** | **Type**          | **Description** |
| :---         | :---          | :------           | :----------     |
| Input        | Input         | Property          | The property that determines the branching logic based on its connection in the parent Shader Graph.         |
| Connected    | Input         | Dynamic Vector    | The value to send to the **Out** port when **Input** is connected in the parent Shader Graph.       |
| NotConnected | Input         | Dynamic Vector    | The value to send to the **Out** port when **Input** isn't connected in the parent Shader Graph.  |
| Out          | Output        | Dynamic Vector    | Outputs the value of either **Connected** or **NotConnected**, depending on whether the property specified in **Input** is connected in the parent Shader Graph.        |


## Example shader Sub Graph usage

This Branch On Input Connection node specifies the default behavior for a Sub Graph input property. When the UV property is connected in the parent graph, then the value from that property is passed directly to the Checkerboard node. When the UV property isn't connected, then the Branch On Input Connection node passes the UV0 value from its connected UV node to the Checkerboard node:

> [!NOTE]
> When previewing a Sub Graph, the Branch On Input Connection node always uses the NotConnected value.

![An image of the Graph window, showing a Branch On Input Connection node that changes the behavior for the UV input property based on its connection status in the parent graph, which changes the result from a Checkerboard node.](images/sg-branch-on-input-connection-node-example.png)
