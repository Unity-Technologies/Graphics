# Properties

Properties are editable fields that exist on graph elements. They connect to other graph elements using the [property workflow](GraphLogicAndPhilosophy.md) to pass data around a graph. You can find Properties on:

* [Contexts](Contexts.md)
* [Blocks](Blocks.md)
* [Operators](Operators.md)

## Using Properties

Properties live on graph elements and change their value dynamically according to the behavior of the graph. If a property represents a graph element's input, you can set its value either directly, using the input field, or indirectly by connecting another property to the property's port. If you connect another property, it overrides the value you set in the input field. If you later remove the connected property, the value reverts to the one set in the input field.

## Property types

Properties in Visual Effect Graph can be of many types. The types available include:

* Base types like: boolean, integer, float, Vector, Texture AnimationCurve, and Gradient.
* [Compound type](#compound-property-types)

### Accessing Property components

Some properties consist of multiple components. For example, a Vector3 contains three components, **x**, **y**, and **z**. These properties can display every component individually. This enables you to get/set property values on a per-component basis. To display the components of a property, click the arrow next to the property. You can use this arrow to fold/unfold the list of components.

![](Images/PropertyComponents.png)

### Casting Properties

Properties can connect between base types to perform casts. Casts are useful to change the data type you are working with to make use of a type's inherent behavior. For example, casting a float to integer allows for integer division.

![](Images/PropertyCast.png)

Casting from one type to another abides with the following rules:

* All Casting rules from HLSL apply:
  * Except boolean, which the Visual Effect Graph does not cast
  * When scalars cast into vectors, they set all components to the scalar value.
* Larger vectors cast into smaller vectors by taking only the first N components.

### Compound Property types

A compound Property is a Property that consists of many base data types to describe more complex data structures. For example, a [Sphere](Type-Sphere.md) consists of a **Position** (Vector3) and a **Radius** (float). Properties that consist of multiple data types are compound Properties.

![](Images/PropertyCompound.png)

To expand compound Property types and access their underlying components, click the arrow, just like with regular Properties.

### Spaceable Properties

A spaceable Property is a Property that carries space information (local/world) along with its value. The graph uses this information to perform automatic space transformations. 

The space modifier is to the left to the property field and you can change the space by clicking it.

For example,  the [Position](Type-Position.md) type carries a Vector3 value and a space. If its space is local and the Vector3 value is [0,1,0], this tells the graph to use the [0,1,0] value in local space.

Depending on the [System Simulation Space](Systems.md#system-spaces), the graph automatically transforms the value to the correct simulation space.

You can also change a Property's space using the [Change Space](Operator-ChangeSpace.md) Operator.

## Property nodes

Property nodes are  [Operators](Operators.md) Nodes that provide access to graph-wide properties defined in the [Blackboard](Blackboard.md). You can use Property nodes to reuse the same value throughout the graph at different places.

Property nodes display a green dot to the left to the Property name if the property is exposed. For information how to expose Properties, see [Blackboard](Blackboard.md).

![](Images/PropertyNodes.png)

### Using Property nodes

To create a Property node, you can:

* Drag the node from the [Blackboard](Blackboard.md) panel into the graph.
* Right-click, then go to **Create Node > Property** and select the property you want to create in the graph.

You can convert a Property node to an inline node of the same type by right-clicking the Property node and selecting **Convert to Inline**.

Deleting a property from the Blackboard also deletes all its instances from the graph.