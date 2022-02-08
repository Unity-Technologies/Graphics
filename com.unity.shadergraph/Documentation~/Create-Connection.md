# Create a connection between nodes

The connection between two nodes in a Shader Graph is an Edge. Edges connect ports and define the flow of data in your Shader Graph.

To create a new Edge:

1. [!include[with-graph-open](./snippets/sg-with-graph-open.md)] select a port on an existing node, or [create a new node](Create-New-Node.md) in your graph.

2. On the port where you want to create a new Edge, click and drag away from the node.

3. Do one of the following:

    - To create a new node at the end of your new Edge, release in an empty space in your graph to open the Create Node menu. Follow the instructions in [Create a new Shader Graph node](Create-New-Node.md#from-another-node's-edge) to create a new node.

    - To attach your Edge to another port on an existing node, release on the port where you want to create the connection.

    > [!NOTE]
    > You can create Edges between nodes you add to your Shader Graph from the Create Node menu, or to Block nodes in your Master Stack. Edges between nodes and Block nodes determine the final output of your shader. For more information on Block nodes, see [Block node](Block-Node.md).

    ![](images/)
    <!-- Add an image showing two nodes connected to each other -->

## Next steps

You can continue [creating nodes](Create-New-Node.md) and connecting them in your Shader Graph, or [save your Shader Graph Asset](Save-Graph-Asset).
