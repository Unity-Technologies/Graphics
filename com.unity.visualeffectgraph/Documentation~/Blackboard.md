# Blackboard

The Blackboard is a utility panel in the [Visual Effect Graph window](VisualEffectGraphWindow.md) that allows you to manage **properties**. Here, you can define, order, and categorize properties. You can also expose properties so that you can access them from outside the graph.

Properties you define in the Blackboard are global variables that you can use multiple times throughout the graph. For example, you can define a bounding box property once and then use it for multiple particle systems in the graph.

![Blackboard-Properties](Images/Blackboard-Properties.png)

Properties in the Blackboard are either **constants** or **exposed**. If you make a property exposed, you can see and edit it on the [Visual Effect Component](VisualEffectComponent.md) as well as via the C# API.

To differentiate between exposed properties and constants, the Blackboard displays a green dot on the left of an exposed property's label.

## Using the Blackboard

To open the Blackboard, click the **Blackboard** button in the Visual Effect Graph window [Toolbar](VisualEffectGraphWindow.md#Toolbar). To resize the Blackboard, click on any edge or corner and drag. To reposition the Blackboard, click on the header and drag.

### Menu Category

In order to set the Menu path of the currently edited Subgraph, you can double-click the sub-title of the blackboard and enter the desired Category Name, then validate using the Return Key

![Blackboard-Category](Images/Blackboard-Category.gif)

### Creating properties

To create a property, click the plus (**+**) button in the top-right of the Blackboard then select a property type from the menu.

You can also convert an inline Operator to a property. To do this, right-click on the Node and select either:

- **Convert to Property** if you want to create a constant.
- **Convert to Exposed Property** if you want to create an exposed property

Regardless of the option you choose, you can enable or disable the **Exposed** setting later.

### Editing properties

To edit a property in the Blackboard, click the folding arrow to the left of the property. This exposes settings that you can use to edit the property. Different properties expose different settings. The core settings are:

| **Setting** | **Description**                                              |
| ----------- | ------------------------------------------------------------ |
| **Exposed** | Specifies whether the property is exposed or not. When enabled, you can see and edit the property on the [Visual Effect Component](VisualEffectComponent.md) as well as via the C# API. |
| **Value**   | Specifies the default value of the property. The Visual Effect Graph uses this value if you do not expose the property or if you expose the property, but do not override it. |
| **Tooltip** | Specifies text that appears when you hover over the property in the Inspector for the Visual Effect. |


### Filtering properties
Float, Int and Uint properties have some filter mode :
* default. does nothing special. You will edit the property value in a textfield.
* Range. You will specify a minimum and a maximum value in the blackboard and you will edit the property with a slider instead of just a textfield
* Enum. Exclusive to uint, You will specify a list of names in the blackboard and you will edit the property with a popup menu.

### Arranging properties

* To **rename** a property:
  1. Either double click the property name or right-click the property name and select **Rename** from the context menu.
  2. In the editable field, type the new name.
  3. Finally, to validate the change, press the **Enter** key or click away from the field.
* To **reorder** properties, **drag and drop** them in the Blackboard.
* To **delete** a property, either:
  * Right-click the property then select **Delete** from the context menu.
  * Select the property then press the **Delete** key (for macOS, **Cmd** + **Delete** key).

### Property categories

Categories allow you to sort properties into groups so you can manage them more easily. You can **rename**, **reorder**, and **delete** categories in the same way as you can for properties.

* To **create** a category, click the plus (**+**) button in the top-right of the Blackboard, then select **Category** from the menu.
* You can **drag and drop** properties from one category to another, or if you want a property to not be part of any category, to the top of the window.

## Property Nodes

Property Nodes look slightly different to standard Nodes. They display the property name and a green dot if the property is exposed.

You can expand them to use a sub-member of the property value.

![PropertyNode](Images/PropertyNode.png)

## Exposed Properties in the Inspector

When you enable the **Exposed** setting for a property, the property becomes visible in the **Properties** section of the Inspector for a [Visual Effect](VisualEffectComponent.md). Properties appear in the same order and categories that you set in the Blackboard.

![Properties-Inspector](Images/Properties-Inspector.png)

### Overriding property values

To edit a property value, you need to override it. To do this, enable the checkbox to the left of the property's name. When you enable this checkbox, the Visual Effect Graph uses the value that you specify in the Inspector. If you disable this checkbox, the Visual Effect Graph uses the default value that you set in the Blackboard.

### Using Gizmos

You can use Gizmos to edit certain advanced property types. To enable Gizmo editing, click the **Show Property Gizmos** button. To use a Gizmo to edit a compatible property, click the **Edit** button next to the property.
