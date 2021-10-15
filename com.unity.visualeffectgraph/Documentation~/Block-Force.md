# Force

Menu Path : **Force > Force**

The **Force** Block applies the given force to particles. To do this, it changes the affected particles’ velocity.

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)

## Block settings

| **Setting** | **Type** | **Description**                                              |
| ----------- | -------- | ------------------------------------------------------------ |
| **Mode**    | Enum     | The method this Block uses to apply a force to particles. The options are:<br/>&#8226; **Absolute**: Applies the force directly to the particles.<br/>&#8226; **Relative**: Applies the force in a way that causes the particles' velocity to tend towards the **Velocity** value. The speed at which they do this depends on the difference between the particles' velocity and the target **Velocity**. This transition is faster with a higher **Drag** value and a lower particle mass. This option is useful to simulate a flow, like wind, that particles follow without ever exceeding the target velocity. |

## Block properties

| **Input**    | **Type**                 | **Description**                                              |
| ------------ | ------------------------ | ------------------------------------------------------------ |
| **Force**    | [Vector](Type-Vector.md) | The force vector this Block applies to particles.<br/>This property only appears if you set **Mode** to **Absolute**. |
| **Velocity** | [Vector](Type-Vector.md) | The relative velocity that affected particles tend towards.<br/>This property only appears if you set **Mode** to **Relative**. |
| **Drag**     | float                    | The drag coefficient.<br/>This property only appears if you set **Mode** to **Relative**. |

## Remarks

This Block only affects the particle position if you enable the **Update Position** setting in the [Update Context](Context-Update.md).
