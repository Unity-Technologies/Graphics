# Set Position (Shape : AABox)

Menu Path : **Position > Set Position (Shape : AABox)**

The **Set Position (Shape : AABox)** Block calculates a position based on an input [Axis-Aligned Box](Type-AABox.md) and stores the result in the [position attribute](Reference-Attributes.md), based on composition.

This Block can calculate the position either from the AABox's **Surface**, **Volume**, or **Thick Surface** where thickness can be relative to the size of the shape, or an absolute value.


This Block also calculates a direction vector based on the calculated position on the shape, and stores it to the [direction attribute](Reference-Attributes.md), based on composition. This direction is equal to the normal of the face the calculated particle position is on. Selection of the face is made based on six pyramids whose base is each face and whose tip is the box center.

Note: [Velocity from Direction and Speed](Block-VelocityFromDirectionAndSpeed.md) Blocks can then process the direction attribute.

![](Images/Block-SetPosition(AABox)Main.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

## Block settings

| **Setting**               | **Type** | **Description**                                              |
| ------------------------- | -------- | ------------------------------------------------------------ |
| **Position Mode**         | Enum     | Specifies how this Block uses the shape to calculate a position. The options are:<br/>&#8226; **Surface**: Calculates positions only on the shape’s surface.<br/>&#8226; **Volume**: Calculates positions inside the entire shape’s volume.<br/>&#8226; **Thickness Absolute**: Calculates positions on a thick surface of given absolute thickness.<br/>&#8226; **Thickness Relative** will compute a position on a thick surface of a given percentage of the Shape’s size. |
| **Composition Position**  | Enum     | **(Inspector)** Specifies how this Block composes the position attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Composition Direction** | Enum     | **(Inspector)** Specifies how this Block composes the direction attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |

## Block properties

| **Input**           | **Type**               | **Description**                                              |
| ------------------- | ---------------------- | ------------------------------------------------------------ |
| **Box**             | [AABox](Type-AABox.md) | The Axis-Aligned Box that determines the shape to calculate the position from. |
| **Thickness**       | Float                  | The thickness of the shape’s surface for position calculation.<br/>This property only appears if you set **Position Mode** to **Thickness Relative** or **Thickness Absolute**. |
| **Blend Position**  | Float                  | The blend percentage between the current position attribute value and the newly calculated position value.<br/>This property only appears if you set **Composition Position** to **Blend**. |
| **Blend Direction** | Float                  | The blend percentage between the current direction attribute value and the newly calculated direction value.<br/>This property only appears if you set **Composition Direction** to **Blend**. |