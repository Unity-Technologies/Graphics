# Set Position (Shape : Line)

Menu Path : **Position > Set Position (Shape : Line)**

The **Set Position (Shape : Line)** Block calculates a position based on an input Line and stores the result in the [position attribute](Reference-Attributes.md), based on composition.


This Block also calculates a direction vector based on the calculated position on the shape, and stores it to the [direction attribute](Reference-Attributes.md), based on composition. This direction is equal to the normalized vector that goes from the start point of the line to its end point.

Note: [Velocity from Direction and Speed](Block-VelocityFromDirectionAndSpeed.md) Blocks can then process the direction attribute.

![](Images/Block-SetPosition(Line)Main.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

## Block settings

| **Setting**               | **Type** | **Description**                                              |
| ------------------------- | -------- | ------------------------------------------------------------ |
| **Spawn Mode**            | Enum     | Specifies how to distribute particles along the line: The options are:<br/>&#8226; **Random**: Calculates a per-particle random progress (from 0 to 1) along the line.<br/>&#8226; **Custom**: Allows you to manually specify the progress on the line in the **Line Sequencer** property. |
| **Composition Position**  | Enum     | **(Inspector)** Specifies how this Block composes the position attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Composition Direction** | Enum     | **(Inspector)** Specifies how this Block composes the direction attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |

## Block properties

| **Input**           | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **Line**            | Line     | The Line that determines the shape to calculate the position from.. |
| **Line Sequencer**  | Float    | Determines the position of a particle on the line (as a percentage of its progress).<br/>This property only appears if you set **Spawn Mode** to **Custom**. |
| **Blend Position**  | Float    | The blend percentage between the current position attribute value and the newly calculated position value.<br/>This property only appears if you set **Composition Position** to **Blend**. |
| **Blend Direction** | Float    | The blend percentage between the current direction attribute value and the newly calculated direction value.<br/>This property only appears if you set **Composition Direction** to **Blend**. |
