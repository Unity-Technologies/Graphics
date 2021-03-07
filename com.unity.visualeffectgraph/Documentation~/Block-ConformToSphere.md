# Conform to Sphere

Menu Path : **Force > Conform to Sphere**

The **Conform to Sphere** Block attracts particles towards a defined sphere. This is useful for a range of cases, such as simulating a “charging” energy effect, and works best when you use it alongside other forces.

![](Images/Block-ConformToSphereExample.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)

## Block properties

| **Input**            | **Type**                 | **Description**                                              |
| -------------------- | ------------------------ | ------------------------------------------------------------ |
| **Sphere**           | [Sphere](Type-Sphere.md) | The sphere the particles conform to.                         |
| **Attraction Speed** | Float                    | The speed with which this Block attracts particles towards the surface of the sphere. |
| **Attraction Force** | Float                    | The strength of the force that pulls particles towards the sphere. |
| **Stick Distance**   | Float                    | The distance at which particles attempt to stick to the sphere. |
| **Stick Force**      | Float                    | The strength of the force that keeps particles on the sphere. |
