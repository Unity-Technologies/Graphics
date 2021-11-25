# Linear Drag

Menu Path : **Force >** **Linear Drag**

The **Linear Drag** Block applies a force to particles that slows them down without affecting their direction.

Block settings

| **Setting**         | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **UseParticleSize** | bool     | Toggles whether larger particles experience more drag. When enabled, the amount drag that particles experience depends on the particles' size. When disabled, all particles experience the same drag. |

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)

## Block properties

| **Input**            | **Type** | **Description**       |
| -------------------- | -------- | --------------------- |
| **Drag Coefficient** | float    | The drag coefficient. |

## Remarks

To make this Block affect particle position, enable **Update Position** in the [Update Context](Context-Update.md) Particle.
