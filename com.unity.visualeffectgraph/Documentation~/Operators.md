# Operators

Operators are the main elements of the Visual Effect Graph's [property (horizontal) workflow](GraphLogicAndPhilosophy.md#property-workflow-horizontal-logic). You use them to create custom behaviors in the graph. For example, you can use them to calculate values from math operations, use these values to sample curves or gradients, then pass the results to [Block](Blocks.md) or [Context](Contexts.md) input [Properties](Properties.md).

![Operators](Images/Operators.png)

## Creating and connecting Operators

An Operator is a type of [graph element](GraphLogicAndPhilosophy.md#graph-elements) so to create one, see [Adding graph elements](VisualEffectGraphWindow.md#adding-graph-elements).

Operators connect to other graph elements horizontally. To achieve this, they use ports. Depending on the Operator's purpose, it may have ports on the left side, to represent inputs, and/or on the right side, to represent outputs.

## Configuring Operators

To change the behavior of the Operator, adjust its [Settings](GraphLogicAndPhilosophy.md#settings) in the Node UI or the Inspector. When you change settings, it may expose or hide certain properties and change the way the Operator looks.

For example, for the [Position (Depth)](Operator-Position(Depth).md) Operator, if you change the **Cull Mode** to **Range**, it exposes the **Depth Range** property.

## Uniform Operators

Some Operators have a single input that can accept various types. For example, the [Absolute](Operator-Absolute.md) Operator can take a float, a Vector3, or an integer. Operators that have this behavior are uniform Operators.

![](Images/OperatorsUniform.png)

A uniform Operator's output type is always the same as its input type. Connecting a new input with a different type automatically changes the output type of the Operator. To manually set the input to a specific type, see [configuring uniform Operators](#configuring-uniform-operators).

##### Configuring uniform Operators

You can configure uniform Operators to manually change the type of the input. 

![](Images/OperatorsUniformOptions.png)

To configure a uniform Operator, first enable configuration mode. To do this, click the gear icon in the top-right corner of the Operator. In configuration mode, a type drop-down replaces the Operator's settings and properties. Use this drop-down to manually set the Operator's input type. 

## Unified Operators

In addition to uniform Operators, some Operators have multiple inputs that can accept different types. For example, the [Lerp](Operator-Lerp.md) Operator can interpolate between two vectors either uniformly, based on a float, or on a per-component basis using a vector with the same length as the other two input vectors. Operators that have this behavior are unified Operators.

![](Images/OperatorsUnified.png)

Unified Operators come with some type constraints, but allow some flexibility to adapt to a variety of types. To manually set the inputs to specific types, see [configuring unified Operators](#configuring-unified-operators).

#### Configuring unified Operators

You can configure unified Operators to manually change the type of each input. 

![](Images/OperatorsUnifiedOptions.png)

To configure a unified Operator, first enable configuration mode. To do this, click the gear icon in the top-right corner of the Operator. In configuration mode, a type drop-down for each input property replaces the Operator's settings and properties. Use this drop-down to manually set the type for each input. In some cases, changing one input type changes another input type as well. This ensures compatibility between particular inputs.

## Cascaded Operators

Some Operators can process a varying number of inputs that can each handle different types. For example, the [Add](Operator-Add.md) allows you to specify a varying number of inputs of different types. Operators that have this behavior are Cascaded operators.

![](Images/OperatorsCascaded.png)

You can connect many inputs to a cascaded operator. To add a new input, connect an edge to the last gray input at the bottom of the Operator. This automatically creates a new input that uses the property type you connected.

If you delete a connection, it removes the input property from the list as well. You can also manually delete an input property when in configuration mode. For information about this and other configuration options, see [configuring cascaded Operators](#configuring-cascaded-operators).

#### Configuring cascaded Operators

![](Images/OperatorsCascadedOptions.png)

To configure a cascaded Operator, first enable configuration mode. To do this, click the gear icon in the top-right corner of the Operator. In configuration mode, a list of the inputs replaces the Operator's settings and properties. For each input, you can:

* Use the text field to rename it.
* Use the type drop-down to set its type.
* Drag the handle on the left to reorder it.

In configuration mode, you can also:

* Use the **+** button to manually add a new input.
* Use the **-** button to delete the selected input.
