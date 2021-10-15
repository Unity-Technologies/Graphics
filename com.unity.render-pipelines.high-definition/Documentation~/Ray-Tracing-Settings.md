# Ray Tracing Settings

In the High Definition Render Pipeline (HDRP), various ray-traced effects share common properties. Most of them are constants, but you may find it useful to control some of them. Use this [Volume Override](Volume-Components.md) to change these values.

## Setting the ray-tracing global parameters

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Ray Tracing** and click on **Ray Tracing Settings**.

## Properties

| **Property**             | **Description**                                              |
| ------------------------ | ------------------------------------------------------------ |
| **Ray Bias** | Specifies the bias value HDRP applies when casting rays for all effects. This value should remain unchained unless your scene scale is significantly smaller or larger than average. |
| **Extend Shadow Culling** | Extends the sets of GameObjects that HDRP includes in shadow maps for more accurate shadows in ray traced effects. |
| **Extend Camera Culling** | Extends the sets of GameObjects that HDRP includes in the rendering. This is a way to force skinned mesh animations for GameObjects that are not in the frustum. |
| **Directional Shadow Ray Length** | Controls the maximal ray length for ray traced directional shadows. |
| **Directional Shadow Fallback Intensity** | The shadow intensity value HDRP applies to a point when there is a [Directional Light](Light-Component.md) in the Scene and the point is outside the Light's shadow cascade coverage. This property helps to remove light leaking in certain environments, such as an interior room with a Directional Light outside. |
