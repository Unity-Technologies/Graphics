# Velocity from Direction & Speed (Random Direction)

> [!IMPORTANT]
> This feature is experimental. To use this feature, open the **Preferences** window, go to the **Visual Effects** tab, and enable **Experimental Operators/Blocks**.

Menu Path : **Velocity > [Set/Add] Velocity from Direction & Speed (Random Direction)**

The **Velocity from Direction And Speed (Random Direction)** Block calculates a velocity for the particle based on a blend ratio between a random direction (per-particle), and the direction attribute.

The Block then scales the final direction vector by a speed, and composes it with the velocity attribute.

![Velocity from Direction And Speed (Random Direction) Block sample.](Images/Block-VelocityFromDirectionAndSpeed(RandomDirection)Example.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)

## Block settings

| **Setting**     | **Type** | **Description**                                              |
| --------------- | -------- | ------------------------------------------------------------ |
| **Composition** | Enum     | **(Inspector)** Specifies how this Block composes the velocity attribute. The options are:<br/>&#8226; **Set**: Overwrites the velocity attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the velocity attribute value.<br/>&#8226; **Multiply**: Multiplies the velocity attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the velocity attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Speed Mode**  | Enum     | Specifies how to calculate the speed. The options are:<br/>&#8226; **Constant**: Applies a constant speed which you set in the **Speed** property.<br/>&#8226; **Random**: Applies a random speed between **Min Speed** and **Max** **Speed**. |

## Block properties

| **Input**           | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **Speed**           | float    | The speed multiplier to apply to the direction vector in order to calculate the velocity.<br/>This property only appears if you set **Speed Mode** to **Constant**. |
| **Min Speed**       | float    | The minimum speed multiplier to apply to the direction vector in order to calculate the velocity.<br/>This property only appears if you set **Speed Mode** to **Random**. |
| **Max Speed**       | float    | The maximum speed multiplier to apply to the direction vector in order to calculate the velocity.<br/>This property only appears if you set **Speed Mode** to **Random**. |
| **Blend Direction** | float    | The blend percentage between the current direction attribute value and the newly calculated direction value. |
| **Blend Velocity**  | float    | The blend percentage between the current direction attribute value and the newly calculated direction value.<br/>This property only appears if you set **Composition** to **Blend**. |
