# Sub Graph Dropdown node

The Sub Graph Dropdown node is a node representation of a Dropdown property. It allows you to create a custom dropdown menu on a Sub Graph node in its parent Shader Graph. You can specify the number of options that appear in the dropdown menu, and their names.

After you create a Dropdown property and add a Dropdown node to a Subgraph, the Subgraph node in any parent Shader Graph displays with a dropdown control.

For more information, refer to [Change the behavior of a Sub Graph with a dropdown](Change-Behaviour-Sub-Graph-Dropdown.md).

## Compatibility

The Subgraph Dropdown [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->

## Ports

> [!NOTE]
> The Subgraph Dropdown node's number of input ports and their names directly correspond to the settings you specify in the Graph Inspector's **Node Settings** tab. The node always has one output port.

A Subgraph Dropdown node's input ports always have the **DynamicVector** type. This means that you can make a connection to an input port from any node that outputs a float, Vector 2, Vector 3, Vector 4, or Boolean value. For more information, see [Dynamic Data Types](Data-Types.md#dynamic-data-types).

It has one output port:

| **Name**     | **Type**      | **Description**  |
| :---         | :-----------  |   :----------    |
| Out          | DynamicVector |  The selected option from the dropdown menu on the parent Shader Graph's Subgraph node. This value can also be the specified **Default** for the property in the Graph Inspector's **Node Settings** tab.     |

## Related nodes

<!-- OPTIONAL. Any nodes that may be related to this node in some way that's worth mentioning -->

[!include[nodes-related](./snippets/nodes-related.md)] Subgraph Dropdown node:

- [Subgraph node](Sub-graph-Node.md)