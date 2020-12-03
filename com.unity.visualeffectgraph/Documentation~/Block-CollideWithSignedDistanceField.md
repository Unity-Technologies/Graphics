# Collide with Signed Distance Field

Menu Path : **Collision > Collide with Signed Distance Field**

The **Collide with Signed Distance Field** Block allows you to create more complex collisions by using an SDF (signed distance field) Asset to represent the shape of the object. This is useful for precise complex collision with predetermined Assets.

![](Images/Block-CollideWithSDFMain.png)

To generate a signed distance field Asset, use an external DCC tool.

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)

## Block settings

| **Setting**       | **Type** | **Description**                                              |
| ----------------- | -------- | ------------------------------------------------------------ |
| **Mode**          | Enum     | The collision shape mode. The options are:<br/>&#8226; **Solid**: Particles cannot enter the collider.<br/>&#8226; **Inverted**: Particles cannot leave the collider. The collider becomes a volume that the particles cannot exit. |
| **Radius Mode**   | Enum     | The mode that determines the collision radius of each particle. The options are:<br/>&#8226; **None**: Particles have a radius of zero.<br/>&#8226; **From Size**: Particles inherit their radius from their individual sizes.<br/>&#8226; **Custom**: Allows you to set the radius of the particles to a specific value. |
| **Rough Surface** | Bool     | Toggles whether or not the collider simulates a rough surface. When enabled, Unity adds randomness to the direction in which particles bounce back to simulate collision with a rough surface. |

## Block properties

| **Input**           | **Type**                       | **Description**                                              |
| ------------------- | ------------------------------ | ------------------------------------------------------------ |
| **Distance Field**  | SDF                            | The signed distance field Asset that specifies the shape of the collision volume. |
| **Field Transform** | [Transform](Type-Transform.md) | The Transform which determines the position, size, and rotation of the **Distance Field**. |
| **Bounce**          | Float                          | The amount of bounce to apply to particles after a collision. A value of 0 means the particles do not bounce. A value of 1 means particles bounce away with the same speed they impacted with. |
| **Friction**        | Float                          | The speed that particles lose during collision. The minimum value is 0. |
| **Lifetime Loss**   | Float                          | The proportion of life a particle loses after collision.     |
| **Roughness**       | Float                          | The amount to randomly adjust the direction of a particle after it collides with the surface.<br/>This property only appears when you enable **Rough Surface**. |
| **Radius**          | Float                          | The radius of the particle this Block uses for collision detection.<br/>This property only appears when **Radius Mode** is set to **Custom**. |

|      |      |      |
| ---- | ---- | ---- |
|      |      |      |
|      |      |      |
|      |      |      |
|      |      |      |
|      |      |      |
|      |      |      |