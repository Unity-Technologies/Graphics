# Ray Tracing Settings

Various parameters are shared between all ray-traced effects. Most of them are constants, but you may find useful to have control over some of them. Use this [Volume Override](Volume-Components.html) to change these values.

## Setting the ray-tracing global parameters

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Ray Tracing** and click on **Ray Tracing Settings**.

## Properties

| **Property**             | **Description**                                              |
| ------------------------ | ------------------------------------------------------------ |
| **Ray Bias** | Defined the bias value that is applied when casting the rays for all effects. This value should remain unchained unless your scene scale is significantly smaller or bigger than average. |
| **Extend Shadow Culling** | Extends the sets of objects that are included in shadow maps for more accurate shadows in ray traced effects. |
| **Extend Camera Culling** | Extends the sets of objects that are included in the rendering. This is a way to force skinned mesh animations for objects that are not in the frustum. |