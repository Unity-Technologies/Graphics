# Operators

Operators are the atomic elements of the [Property Workflow](GraphLogicAndPhilosophy.md#property-workflow-horizontal-logic). These Nodes allow you to define custom expressions in Visual Effect Graphs that you can use to create custom behaviors. For instance compute values from math operations and use the result of these operations to sample curves, gradients, to use the resulting values into [Block](Blocks.md) or [Context](Contexts.md) input [Properties](Properties.md).

![Operators](Images/Operators.png)

## Adding Operator Nodes

You can add Operator Nodes in the followings ways:

* In the Create Node Menu:
  * Right-click in an empty space and select **Create Node** from the menu.
  * Right-click on a edge and selecting **Create Node** from the menu.
  * Press the Spacebar key when the cursor is in an empty space.
  * Drag an Edge Connection from a Property, then release in an empty space.
* Duplicate Nodes:
  * Select **Duplicate** in the Context menu (or Ctrl+D).
  * **Copy**, **Cut** and **Paste** the Operator from Context menu (or Ctrl+C/Ctrl+X then Ctrl+V).

## Configuring Operators

When you change the Operator [Settings](GraphLogicAndPhilosophy.md#settings) in the Node UI or the Inspector, the Operator changes how it looks and behaves.

For example, when you change the Cull Mode of a `Position (Depth)` Operator from **None** to **Range**, Unity adds an extra **Depth Range** property to the Operator.

## Uniform Operators

Uniform Operators are Nodes that you can use with a single input of a Variable Type. For example, you can use absolute values for a float, a Vector3 or an Integer.

![](Images/OperatorsUniform.png)

The output type of any Uniform Operator is always the same as its input Type. Connecting a new input with a different type will change automatically the output type of the operator. If you want to manually set the Node to a specific type, see **Configuring Uniform Operators**.

##### Configuring Uniform Operators

![](Images/OperatorsUniformOptions.png)

Press the Options icon in the top-right corner to switch the Node view to Configuration mode. In this mode you can manually change the operator Type.

## Unified Operators

In addition to the Uniform Operators, some operators with many inputs can handle **multiple inputs of Variable Types**. These Nodes are called **Unified Operators**.

For example, the **Lerp** Operator can interpolate between two Vectors uniformly based on a float or every component using a Vector of the Same Length.

![](Images/OperatorsUnified.png)

Unified Operators have type constraints but allow some flexibility in order to adapt to some variety of types.

#### Configuring Unified Operators

![](Images/OperatorsUnifiedOptions.png)

Press the Options icon in the top-right corner to switch the Node view to Configuration mode. In this mode you can manually change the operator Types for every input. In some cases, changing one input type will change another input type in order to maintain compatibility.

## Cascaded Operators

**Cascaded Operators** process a variable input count. These operators can process many outputs and handle different input Types, like **Unified Operators**

For example, the Add Node allows you to add many inputs of different types using a single Node.

![](Images/OperatorsCascaded.png)

You can connect many inputs to a Cascaded Operator. To add a new item to the list, connect an edge to the last gray input at the bottom of the Nod. This creates a new input that uses the property type you connected.

When you delete a connection, Unity also removes the input property from the list. However you can also delete an input property manually using the Configuration Mode.

#### Configuring Cascaded Operators

![](Images/OperatorsCascadedOptions.png)

Press the Options icon in the top-right corner to switch the Node view to Configuration mod. In this mode you can:

* Rename Inputs using their Text Field.
* Change Input Types using the type Popup.
* Reorder Inputs by dragging the Handle on the left of each input line.
* Manually Add Inputs using the ''+'' button.
* Delete Selected Input using the ''-'' button.
