# Geometric Specular Anti-aliasing

The **Geometric Specular AA** property allows you to perform geometric anti-aliasing on this Material. This modifies the smoothness values on surfaces of curved geometry in order to remove specular artifacts. HDRP reduces the smoothness value by an offset depending on the intensity of the geometry curve. This is especially effective for high-density meshes with a high smoothness.. Enabling **Geometric Specular AA** exposes extra properties in your Shader to help you customize the effect.

## Properties

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Screen Space Variance** | Use the slider to set the strength of the geometric specular anti-aliasing effect between 0 and 1. Higher values produce a blurrier result with less aliasing.<br />This property only appears when you enable the **Geometric Specular AA** checkbox. |
| **Threshold**             | Use the slider to set a maximum value for the offset that HDRP subtracts from the smoothness value to reduce artifacts.<br />This property only appears when you enable the **Geometric Specular AA** checkbox. |