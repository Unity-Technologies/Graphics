# Reflection Proxy Volume

The Reflection Proxy Volume is the reprojection volume that objects use when they apply a reflection from a Reflection Probe or a Planar Probe. For more information about reprojection and the theory behind reflection, see the documentation on [Reflection in HDRP](Reflection-in-HDRP.md).

You can assign a Reflection Proxy Volume to each Probe in your Scene, as long as they have compatible **Shapes**, and even reuse the same Reflection Proxy Volume with multiple Reflection Probes. For a Reflection Proxy Volume and a Probe to be compatible, they must either both use the same **Shape**, or one use **Infinite**. For example:

- **Box** is compatible with **Box**.
- **Sphere** is compatible with **Sphere**.
- **Infinite** is compatible with both other **Shapes**.

## Properties

![](Images/ReflectionProxyVolume1.png)

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Shape**    | Defines the shape of the Proxy Volume. The possible values are:<br />&#8226;**Box**. The Proxy Volume is box shaped and you can change the size of each axis to set its bounds.<br />&#8226; **Sphere**. The Proxy Volume is sphere shaped and you can change its radius to set its bounds.<br />&#8226; **Infinite**. The Proxy Volume is not bounded. This **Shape** provides the same result not specifying a Proxy Volume for a Reflection Probe. It is useful for easily changing a Scene setup.<br />The **Shape** of the Proxy Volume must match the shape of the Reflection Probe or Planar Reflection Probe using it. Except for infinite which can match any shape. |
| **Box Size** | Defines the scale of each axis of the box that represents the Proxy Volume. Only available with a **Box Shape**. |
| **Radius**   | Defines the radius of the sphere that represents the Proxy Volume. Only available with a **Sphere Shape**. |



## Gizmo

You can use the Scene view gizmo to visually modify the size of the **Box** and **Sphere** shapes. Click and drag the handles to move the boundaries of the Proxy Volume.

![](Images/ReflectionProxyVolume2.png)
