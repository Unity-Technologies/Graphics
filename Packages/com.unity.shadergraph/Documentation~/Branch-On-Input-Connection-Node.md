# Branch On Input Connection node

The Branch On Input Connection node allows you to change the behavior of a [Sub Graph](Sub-graphs.md) based on the connected state of an input property in the parent shader graph. Use the Branch On Input Connection node to create a default input for a port.

For more information, refer to [Set default inputs for a Sub Graph](Sub-Graph-Default-Property-Values.md).

The node generates branching HLSL source code, but during compilation Unity optimizes the branch out of your shader.

## Compatibility

The Branch On Input Connection [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->

You can only use a Branch on Input Connection node in a [Sub Graph](Sub-graphs.md).

## Inputs

The Branch On Input Connection [!include[nodes-inputs](./snippets/nodes-inputs.md)] <!-- MULTIPLE INPUT PORTS INCLUDE -->

| **Name**         | **Type**          | **Description** |
| :---             | :------           | :----------     |
| **Input**        | Property          | The property that determines the branching logic in the node, based on its connection state in the parent Shader Graph.      |
| **Connected**    | Dynamic Vector    | The value to send to the **Out** port when **Input** is connected in the parent Shader Graph.     |
| **NotConnected** | Dynamic Vector    | The value to send to the **Out** port when **Input** isn't connected in the parent Shader Graph. |


## Outputs

The Branch On Input Connection [!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
| **Out**  | Dynamic Vector    | Outputs the value of either **Connected** or **NotConnected**, based on the **Input** property's connection state in the parent Shader Graph.        |

## Related nodes

<!-- OPTIONAL. Any nodes that may be related to this node in some way that's worth mentioning -->

[!include[nodes-related](./snippets/nodes-related.md)] Branch On Input Connection node:

- [Branch node](Branch-Node.md)
- [Subgraph node](Sub-graph-Node.md)
