# Create a new Shader Graph node

You can add nodes from the Create Node menu to a Shader Graph to create your final shader output.

## From a blank space in your Shader Graph

To add a node to a blank space in your Shader Graph Asset:

1. [!include[with-graph-open](./snippets/sg-with-graph-open.md)] open the Create Node menu by doing one of the following:

    - Press the Spacebar.
    - Right-click an empty space in the Graph window and select **Create Node**.

[!include[find-add-node-create-node-menu](./snippets/sg-find-add-node-create-node-menu.md)]

![](images/)
<!-- Add an image that shows the Create Node menu -->

## From another node's Edge

To create a new node from another node's [Edge](Edge.md):

1. [!include[with-graph-open](./snippets/sg-with-graph-open.md)] click and drag from any port on an existing node and release on an empty space to open the Create Node menu.

[!include[find-add-node-create-node-menu](./snippets/sg-find-add-node-create-node-menu.md)]

![](images/)
<!-- Add an image that shows Create Node menu from an Edge -->

> [!NOTE]
> When opening the Create Node menu from an existing node's Edge, you can only access and add nodes that are compatible with the Data Type of your current Edge. For more information, see [Data Types](Data-Types.md).

## In the Master Stack

To add a new Block node to a [Context](Master-Stack.md#Contexts) in your graph's [Master Stack](Master-Stack.md):

1. [!include[with-graph-open](./snippets/sg-with-graph-open.md)] open the Create Node menu in the Master Stack by doing one of the following:

    - Right-click directly above or below an existing Block node in a Context and select **Create Node**.
    - With an existing Block node selected in a Context, press the Spacebar.
    - Select an empty Context and press the Spacebar.

[!include[add-node-master-stack](./snippets/sg-add-node-master-stack.md)]

![](images/)
<!-- Add an image that shows Create Node menu from the Master Stack -->

## Next steps

After you've created a node in your Shader Graph, you can [create connections](Create-Connection.md) to specify how data should flow between nodes in your Shader Graph.

For more information about the nodes available in Shader Graph, see the [Node Library](Node-Library.md).
