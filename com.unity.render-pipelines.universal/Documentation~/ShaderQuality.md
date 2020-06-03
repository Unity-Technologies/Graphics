# Shader Quality

This setting defines the available features and behavior of shaders in the Player.

Available values: __Low__, __Medium__, __High__.

## Shader feature support:

The following table shows the shader features that different Shader Quality settings support.

| __Feature__         | __Description__           | __Shader define__ | __Low__    | __Medium__ | __High__   |
| ------------------- | ------------------------- | ----------------- | ---------- | ---------- | ---------- |
| __Shadow fading__   | Enables blending between the visible shadows and the invisible shadows when the shadows are too far away to be rendered.              | FADE_SHADOWS |   | Yes | Yes |

## Normalization of normals:

The following list shows how Unity normalizes the normals with different Shader Quality settings.

* __Low__: Use the per-pixel normalization if the normal map is defined, otherwise use the per-vertex normalization.
* __Medium__: Always use the per-vertex normalization. Use the per-pixel normalization only if the normal map is defined.
* __High__: Always use the per-vertex and the per-pixel normalization.

## Default values for platforms

To use the Shader Quality setting from the URP asset, a shader must have the following keyword definition:

```
#pragma multi_compile _SHADER_QUALITY_LOW _SHADER_QUALITY_MEDIUM _SHADER_QUALITY_HIGH
```

If such definition is missing in a shader, Unity uses the following default settings depending on the target platform:

* OpenGL ES 2.0/WebGL 1.0: __Low__.
* Mobile platforms: __Medium__.
* Other platforms: __High__.
