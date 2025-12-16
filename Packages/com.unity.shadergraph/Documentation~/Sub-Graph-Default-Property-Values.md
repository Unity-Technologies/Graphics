# Set default inputs for a Sub Graph

To set the default inputs for a Sub Graph, use one of the following approaches:

- For uniform defaults, for example a color or single coordinate, set the default value in the Graph Inspector window.
- For varying defaults, for example a full set of texture coordinates, use a [Branch On Input Connection node](Branch-On-Input-Connection-Node.md).

## Set uniform defaults

After you [add an input to a Sub Graph](Create-Sub-Graph.md#set-inputs), follow these steps:

1. In the Sub Graph, select the property in the Blackboard window.
2. In the Graph Inspector window, select **Node Settings**, then set **Default Value**. 

When you add the Sub Graph to a parent Shader Graph, the input port of the Sub Graph node uses the default value if the port isn't connected.

## Set varying defaults

> [!NOTE]
> You can't use the Branch On Input Connection node with a Streaming Virtual Texture property. For more information, refer to [Using Streaming Virtual Texturing in Shader Graph](https://docs.unity3d.com/Documentation/Manual/svt-use-in-shader-graph.html).

After you [add an input to a Sub Graph](Create-Sub-Graph.md#set-inputs), follow these steps:

1. In the Sub Graph, select the property in the Blackboard window.
2. In the Graph Inspector window, select **Node Settings**, then enable **Use Custom Binding**.
3. Enter a name in the **Label** field. This label appears on the input in the parent shader graph.
4. Right-click in the Sub Graph workspace to open the context menu, then select **Create Node** > **Utility** > **Logic** > **Branch On Input Connection** to add a [Branch On Input Connection node](Branch-On-Input-Connection-Node.md).
5. Connect the property to the **Input** port of the Branch On Input Connection node. The node now outputs different values depending on whether the parent shader graph connects a node to the input.
6. Connect the property to the **Connected** port. If the parent shader graph connects a node to the input, the Branch On Input Connection node uses that value. 
7. Connect another node to the **NotConnected** port, for example a [UV node](UV-Node.md). If the parent Shader Graph doesn't connect a node to the input, the Branch On Input Connection node uses this as the default input.
8. Connect the output of the Branch On Input Connection node to the rest of your sub graph.

> [!NOTE]
> The preview in the Branch On Input Connection node always uses the **NotConnected** value.

## Example

The following sub graph inputs the default UV0 coordinates of the mesh if the parent Shader Graph doesn't connect a node to the input.

![A Vector2 property connected to the Input and Connected inputs of a Branch On Input Connnection node. A UV node is connected to the NotConnected input. The Use Custom Binding property of the Vector2 property is enabled. The Branch On Input Connection node outputs to an Output node.](images/shader_graph_branch_on_connection.png)

## Additional resources

- [Branch node](Branch-Node.md)
- [Sub Graphs](Sub-graphs.md)
