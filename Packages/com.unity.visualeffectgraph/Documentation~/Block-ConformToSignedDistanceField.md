# Attractor Shape Signed Distance Field

Menu Path : **Force > Attractor Shape Signed Distance Field**

The **Attractor Shape Signed Distance Field** Block attracts particles towards a defined distance field. This is useful for pulling particles towards a specific shape which cannot be easily defined via other force Blocks, and it works best when you use it alongside other forces.

<video src="Images/Block-ConformToSDFExample.mp4" title="Particles dynamically adjusting their positions to align and conform to the surface of a 3D shape defined by a Signed Distance Field." width="320" height="auto" autoplay="true" loop="true" controls></video>

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)

## Block properties

| **Input**            | **Type**                       | **Description**                                              |
| -------------------- | ------------------------------ | ------------------------------------------------------------ |
| **Distance Field**   | Texture3D                      | The signed distance field texture to which particles conform. |
| **Field Transform**  | [Transform](Type-Transform.md) | The transform with which to position, scale, or rotate the field. |
| **Attraction Speed** | Float                          | The speed with which this Block attracts particles towards the signed distance field. |
| **Attraction Force** | Float                          | Sets the strength of the force that pulls particles towards the signed distance field. |
| **Stick Distance**   | Float                          | The distance at which particles attempt to stick to the signed distance field. |
| **Stick Force**      | Float                          | The strength of the force that keeps particles on the signed distance field. |
