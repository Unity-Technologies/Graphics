# Sub Graph Dropdown Node

The Sub Graph Dropdown node is a node representation of a Dropdown property. It allows you to create a custom dropdown menu. You can specify the number of options that appear in the dropdown menu, and their names.

After you create a Dropdown property and add its Dropdown node to a Sub Graph, the Sub Graph's node in any parent Shader Graph displays with a dropdown control:

![An image of the Graph window, showing a parent Shader Graph with a Sub Graph node. The Sub Graph node has a dropdown menu because the Sub Graph has a Dropdown property and Dropdown node.](images/sg-subgraph-dropdown-node-example.png)

The Sub Graph Dropdown node is similar to the [Keyword node](Keyword-Node.md), and the Graph Inspector displays many of the same properties as an [enum Keyword](Keywords.md#enum-keywords).

## Create Node menu category

The Sub Graph Dropdown node isn't accessible from the Create Node menu.

To add a Sub Graph Dropdown node to a Sub Graph:

1. In the Shader Graph window, open a Sub Graph.

2. In the Blackboard, select **Add** (+) and select **Dropdown**.

3. Enter a name for your new Dropdown property, and press Enter.

4. Select your Dropdown property and drag it onto your graph to create a new Sub Graph Dropdown node.

5. Select your new Dropdown node in your graph or the Dropdown property in the Blackboard and open the Graph Inspector.

6. Select the **Node Settings** tab.

7. In the **Entries** table, select **Add to the list** (+) to add a new option to your dropdown. Each Entry adds a corresponding input port to your node.
    To remove an Entry, select its handle in the list and select **Remove selection from the list** (-).

8. (Optional) In the **Default** list, select the default Entry that you want Shader Graph to select on your property.

![An image of the Graph window, showing a Dropdown node in the Graph Editor. The Dropdown property is selected in the Blackboard and the Graph Inspector shows the available options for correctly configuring the Dropdown property.](images/sg-subgraph-dropdown-node.png)


## Compatibility

The Sub Graph Dropdown node is compatible with all render pipelines.

## Ports

> [!NOTE]
> The node's number of input ports and their names directly correspond to the settings you specify in the Graph Inspector's **Node Settings** tab. The node always has one output port.

A Sub Graph Dropdown node's input ports always have the **DynamicVector** type. This means that you can make a connection to an input port from any node that outputs a float, Vector2, Vector3, Vector4, or Boolean value. For more information, see [Dynamic Data Types](Data-Types.md#dynamic-data-types).

| **Name**     | **Direction** | **Type** | **Description**  |
| :---         | :---          | :------  |   :----------    |
| Out          | Output        | DynamicVector    |  The option from the dropdown menu selection on the parent graph's Sub Graph node. Can also be the specified **Default** for the property in the Graph Inspector's **Node Settings** tab.     |
