# Collide with Plane

Menu Path : **Collision > Collide with Plane**

The **Collide with Plane** Block defines a flat plane with infinite extends for particles to collide with.

![](Images/Block-CollideWithPlaneMain.png)

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)

## Block settings

| **Setting**       | **Type** | **Description**                                              |
| ----------------- | -------- | ------------------------------------------------------------ |
| **Mode**          | Enum     | The collision shape mode. The options are:<br/>&#8226; **Solid**: Particles collide with the plane when travelling in an opposing direction to the plane's normal.<br/>&#8226; **Inverted**: Inverts the normal direction of the plane for the purpose of collisions. This means particles collide with the plane when travelling in a direction not opposing the plane's normal. |
| **Radius Mode**   | Enum     | The mode that determines the collision radius of each particle. The options are:<br/>&#8226; **None**: Particles have a radius of zero.<br/>&#8226; **From Size**: Particles inherit their radius from their individual sizes.<br/>&#8226; **Custom**: Allows you to set the radius of the particles to a specific value. |
| **Rough Surface** | Bool     | Toggles whether or not the collider simulates a rough surface. When enabled, Unity adds randomness to the direction in which particles bounce back to simulate collision with a rough surface. |

## Block properties

| **Input**         | **Type**               | **Description**                                              |
| ----------------- | ---------------------- | ------------------------------------------------------------ |
| **Plane**         | [Plane](Type-Plane.md) | The plane that specifies the center position and normal of the collision plane. |
| **Bounce**        | Float                  | The amount of bounce to apply to particles after a collision. A value of 0 means the particles do not bounce. A value of 1 means particles bounce away with the same speed they impacted with. |
| **Friction**      | Float                  | The speed that particles lose during collision. The minimum value is 0. |
| **Lifetime Loss** | Float                  | The proportion of life a particle loses after collision.     |
| **Roughness**     | Float                  | The amount to randomly adjust the direction of a particle after it collides with the surface.<br/>This property only appears when you enable **Rough Surface**. |
| **Radius**        | Float                  | The radius of the particle this Block uses for collision detection.<br/>This property only appears when **Radius Mode** is set to **Custom**. |