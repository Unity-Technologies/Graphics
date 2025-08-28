# Properties

Properties are editable fields that you can connect to graph elements using [Property workflow](GraphLogicAndPhilosophy.md). They can be found on graph elements such as  [Contexts](Contexts.md),  [Blocks](Blocks.md) and [Operators](Operators.md).

![Two examples of using properties in a VFX Graph. In the first example, a blue line indicates that a Get Attribute: velocity (Current) Operator has its y output property connected to the X input of a Absolute (float) Operator. In the second example, a Total Time Operator outputs into the X parameters of both a Sine (float) Operator and a Cosine (float) Operator, which in turn output into the X and Y input parameters of a Vector3 Operator.](Images/PropertyComponents.png)

## Use Properties

Properties appear on graph elements, and change their value according to their actual value in the graph. When you connect another property to a property port, the graph element displays the computed value of the connected property.

After disconnecting a connected property, the field reverts to the previously set property value.

## Property Types

Properties in Visual Effect Graph can be of any Type, including the following:

* boolean
* integer
* float
* Vectors
* Textures
* AnimationCurve
* Gradient

### Access Property Components

Properties that are made of multiple components (such as Vectors, or Colors) can display every component individually in order to connect these to other properties of compatible type. Use the arrow next to the property to unfold the components.

### Cast Properties

Properties can connect between base types to perform a cast. Casts change the data type you are working on in order to inherit its properties. For example, if you cast a float to integer, the float can use integer division.

Casting from one type to another abides with the following rules:

* All Casting rules from HLSL apply:
  * Except Boolean types that cannot be cast.
  * Scalars will cast into vectors by setting all components.
* Vectors cast into vectors of lesser size by taking only the first N components.

### Compound Property Types

Compound Property Types are made from base data types. These types describe more complex data structures. For example, a Sphere is composed of a Position (Vector3) and a radius (float).

Expand Compound Property Types to access their components.

To access components in a script, add an underscore before the component name. For example to access the `radius` component of `MySphere`, use `MySphere_radius`.

### Spaceable Properties

Spaceable Properties are Property Types that carry **Space information** (Local/World) with its value. This information is used by the graph to perform automatic space transformations when required.

Click on the Space Modifier to the left of the Property Field to change it.

For Example, a Position type carries a Vector3 value and a Spaceable Property. If you set the Spaceable Property to Local [0,1,0], this tells the graph that we refer to the 0,1,0 value in local space.

Depending on the [System Simulation Space](Systems.md#system-spaces), the value will be automatically transformed to the simulation space if required.

> [!TIP]
> You can use the Change Space Operator to manually change a Property Space.

## Property Nodes

Property Nodes are [Operators](Operators.md) that give access to Graph-Wide Properties defined in the [Blackboard](Blackboard.md). You can reuse the same property multiple times in the graph, even across different systems.

* Property Nodes display a Green dot left to the Property name if the property is exposed.
* To create a Property Node:
  * Drag the Node from the Blackboard Panel into the Workspace.
  * Open the Right Click context menu, open the **Create Node** menu and select the desired property from the Property category.
* To convert a Property Node to an Inline Node of the same type, right-click the property Node and select **Convert to Inline**
* When you delete a property from the Blackboard, Unity also deletes its property Node instances from the graph.

## Create a property

To create a new property, follow these steps:

1. Select the **+** button in the top-left corner of the Blackboard panel.

1. Select **Property** and choose a category.

To display a property in the **Properties** section of the **Inspector** window for a [Visual Effect](VisualEffectComponent.md) component, enable the **Exposed** setting.

## Manage a property

You can manage properties as follows:

- To duplicate a property, use the shortcut **Ctrl+D** (macOS: **Cmd+D**) or the context menu.

- To select several properties, hold **Shift+Click** or **Ctrl+Click** (macOS: **Cmd+Click**), then drag and drop them into the graph as needed.

- To copy properties and paste them across different VFX Graphs, use **Ctrl+C** and **Ctrl+V**.

- To highlight all nodes corresponding to a property, hover over the property in the Blackboard panel. Similarly, hover over a node in the graph to highlight its corresponding property in the Blackboard panel.

- To identify unused properties, right-click a category or the top of the Blackboard panel, then select **Select Unused Properties**. This is useful for cleaning up your VFX Graph.

## Convert an inline operator into a property

To convert an inline operator into a property, do one of the following:

- Press **Shift+X**

- Right-click the node in the graph and select **Convert to Property**.

The properties are displayed in the same order and categories as defined in the Blackboard panel.

## Override property values per GameObject

To override an exposed property value for a specific Visual Effect component, follow these steps:

1. Select the GameObject holding the Visual Effect in the hierarchy.

1. In the **Inspector** window, locate the exposed properties list.

1. Modify the value of a property.

	The corresponding checkbox is enabled.

	To revert to the default value set in the Blackboard panel, disable the checkbox.
