# Proxy Volume

A Proxy Volume is a 3D shape on which HDRP projects a 2D color buffer or cubemap texture that contains a rendered scene. 

In the Unity Editor, this component is called a Reflection Proxy Volume, but HDRP uses the component to calculate more accurate effects in both reflection and refraction.

## Assign a custom Proxy Volume to a Reflection Probe

By default, a Reflection Probe uses the shape and size of its [Influence Volume](Reflection-Probe.md#influence-volume) as its Proxy Volume.

To assign a custom Proxy Volume to a Reflection Probe, do the following:

1. In **Projection Settings**, disable **Use Influence Volume As Proxy Volume**.
2. Select **Add Component** and select **Reflection Proxy Volume**.
3. In the **Reflection Proxy Volume**, set the shape and size of the Proxy Volume.
4. In the probe's **Projection Settings**, use **Proxy Volume** to select the Proxy Volume you created.

The **Shape** of the Proxy Volume must match the **Shape** of the Reflection Probe.

- Use **Box** if the **Shape** of the Reflection Probe is **Box**.
- Use **Sphere** if the **Shape** of the Reflection Probe is **Sphere**.
- Use **Infinite** with either Reflection Probe shape.

If you disable **Use Influence Volume As Proxy Volume** in a probe's **Projection Settings** but leave **Proxy Volume** empty, the Proxy Volume has the **Infinite** shape by default.

You can reuse the same Proxy Volume with other Reflection Probes, as long as the shapes match. This is useful if you use multiple Reflection Probes in a single room, because you can reuse one room-shaped Proxy Volume for all the probes.

## How reflection and refraction use a Proxy Volume 

HDRP projects the color buffer or the cubemap texture from a Reflection Probe or a Planar Probe onto a Proxy Volume. HDRP then uses the Proxy Volume to more accurately calculate the depth of the point a reflected or refracted vector hits. 

HDRP determines this based on a [reflection and refraction hierarchy](reflection-refraction-hierarchy.md).

See the following:

- [Reflection in HDRP](Reflection-in-HDRP.md) for more about projection and the theory behind reflection.
- [Use the appropriate Proxy Volume in refraction](refraction-use.md#use-proxy-volume).

## Properties
| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Shape** | Defines the shape of the Proxy Volume. The possible values are:<br/>&#8226; **Box**. Sets the shape as a box. Use **Box Size** to change the size of the box.<br/>&#8226; **Sphere**. Sets the shape as a sphere. Use **Radius** to change the size of the sphere.<br/>&#8226; **Infinite**. Sets the shape as an infinite volume. You can't adjust the size of this volume. If your Material uses the planar refraction model and the nearest Proxy Volume uses the **Infinite** shape, refraction might not be noticeable.<br/>The **Shape** of a Proxy Volume must match the **Shape** of the Reflection Probe or Planar Reflection Probe that uses it, unless you use **Infinite**.
| **Box Size** | Defines the scale of each axis of the box that represents the Proxy Volume. Only available with a **Box Shape**. |
| **Radius**   | Defines the radius of the sphere that represents the Proxy Volume. Only available with a **Sphere Shape**. |

## Resize a Proxy Volume

You can use the Scene view Gizmo to visually modify the size of the **Box** and **Sphere** shapes. Click and drag the handles to move the boundaries of the Proxy Volume.

![](Images/ReflectionProxyVolume2-gizmo.png)<br/>

