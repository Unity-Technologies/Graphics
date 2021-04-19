# Set Position (Signed Distance Field)

Menu Path : **Position > Set Position (Signed Distance Field)**

The **Set Position (Signed Distance Field)** Block calculates a position based on an input Signed Distance Field (SDF) and stores the result in the [position attribute](Reference-Attributes.md), based on composition.

This Block can calculate the position either from the SDF's **Surface**, **Volume**, or **Thick Surface** where thickness can be relative to the size of the shape, or an absolute value.


This Block also calculates a direction vector based on the calculated position on the shape, and stores it to the [direction attribute](Reference-Attributes.md), based on composition. This direction is equal to the normal of the surface at the calculated particle position.

Note: [Velocity from Direction and Speed](Block-VelocityFromDirectionAndSpeed.md) Blocks can then process the direction attribute.

![](Images/Block-SetPosition(SDF)Example.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

## Block settings

| **Setting**          | **Type** | **Description**                                              |
| -------------------- | -------- | ------------------------------------------------------------ |
| **Position Mode**    | Enum     | Specifies how this Block uses the shape to calculate a position. The options are:<br> &#8226; **Surface**: Calculates positions only on the shape’s surface.<br> &#8226; **Volume**: Calculates positions inside the entire shape’s volume.<br> &#8226; **Thickness Absolute**: Calculates positions on a thick surface of given absolute thickness.<br> &#8226; **Thickness Relative** Calculates positions on a thick surface as a given percentage of the largest axis’s size. |
| **Spawn Mode**       | Enum     | Specifies how this Block distributes the particles among the shape’s arc.<br/>&#8226; **Random**: Calculates a per-particle random progress (0..1) on the arc.<br/>&#8226; **Custom**: Allows you to specify the progress in the **Arc Sequencer** property port. |
| **Kill Outliers**    | Bool     | (**Inspector**) Indicates whether to kill particles whose position does not adhere to the surface/volume. |
| **Projection Steps** | uint     | (**Inspector**) The number of steps this Block uses to project the particle onto the surface of the SDF. This may impact performance, but can yield less outliers. |

## Block properties

| **Input**         | **Type**               | **Description**                                              |
| ----------------- | ---------------------- | ------------------------------------------------------------ |
| **Box**           | [AABox](Type-AABox.md) | The Axis-Aligned Box that determines the shape to calculate the position from. |
| **Thickness**     | float                  | The thickness of the shape’s surface for position calculation.<br/>This property only appears if you set **Position Mode** to **Thickness Relative** or **Thickness Absolute**. |
| **Arc Sequencer** | float                  | The position in the arc to spawn particles.<br/>This property only appears if you set **Spawn Mode** to **Custom**. |

## Notes

This Block calculates the relative thickness based on the largest axis of the SDF, which is not necessarily the size of the object the SDF represents. Therefore, the positions this Block calculates can be inside the entire shape’s volume even with a relative thickness smaller than 1.