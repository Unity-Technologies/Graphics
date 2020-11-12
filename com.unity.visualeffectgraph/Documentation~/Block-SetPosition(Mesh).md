# Set Position (Mesh)

Menu Path : **Position > Set Position (Mesh)**

Menu Path : **Position > Set Position (Skinned Mesh)**

The **Set Position (Mesh)** block fetch a position based on an vertex data of  and stores the result in the [position attribute](Reference-Attributes.md), based on composition. 


This Block also calculates a direction vector based on the mesh normal, and stores it to the [direction attribute](Reference-Attributes.md), based on composition.

Note: [Velocity from Direction and Speed](Block-VelocityFromDirectionAndSpeed.md) Blocks can then process the direction attribute.



## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

## Block settings

| **Setting**               | **Type** | **Description**                                              |
| ------------------------- | -------- | ------------------------------------------------------------ |
| **Spawn Mode**            | Enum     | Specifies how to distribute particles along the line: The options are:<br/>&#8226; **Random**: Calculates a per-particle random progress uniform sampling among the chosen primitive in **Placement Mode** .<br/>&#8226; **Custom**: Allows you to manually specify the sampling parameters. |
| **Composition Position**  | Enum     | **(Inspector)** Specifies how this Block composes the position attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Composition Direction** | Enum     | **(Inspector)** Specifies how this Block composes the direction attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |

## Block properties

| **Input**           | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| TODO                |          |                                                              |
| **Blend Position**  | Float    | The blend percentage between the current position attribute value and the newly calculated position value.<br/>This property only appears if you set **Composition Position** to **Blend**. |
| **Blend Direction** | Float    | The blend percentage between the current direction attribute value and the newly calculated direction value.<br/>This property only appears if you set **Composition Direction** to **Blend**. |

