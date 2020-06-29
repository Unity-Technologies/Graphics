# Shader Quality

In this setting you select the Shader Quality tier, __Low__, __Medium__, or  __High__. The quality tier defines the available features and behavior of shaders.

Shader Graph shaders and shaders installed with the URP package support Shader Quality tiers. To add support for Quality Tiers in your custom shader, see section [Adding support for Shader Quality tiers in custom shaders](#support-for-shader-quality).

Select the Shader Quality tier on the [render pipeline asset](universalrp-asset.md), in __Quality > Shader Quality__.

## Shader feature support:

The following table shows the shader features that different Shader Quality settings support.

| __Feature__         | __Description__           | __Shader define__ | __Low__    | __Medium__ | __High__   |
| ------------------- | ------------------------- | ----------------- | ---------- | ---------- | ---------- |
| __Smooth Shadow Falloff__   | Enabling this setting makes shadows fade smoothly when the shadows are father than the maximum shadow rendering distance (__Shadows > Distance__ in the render pipeline asset).              | FADE_SHADOWS |   | Yes | Yes |

## Normalization of normals:

The following list shows how Unity normalizes the normals with different Shader Quality settings.

* __Low__: Use the per-pixel normalization if the normal map is defined, otherwise use the per-vertex normalization.
* __Medium__: Always use the per-vertex normalization. Use the per-pixel normalization only if the normal map is defined.
* __High__: Always use the per-vertex and the per-pixel normalization.

<a name="support-for-shader-quality"></a>

## Adding support for Shader Quality tiers in custom shaders

To use the Shader Quality setting from the URP asset, a shader must have the following keyword definition:

```
#pragma multi_compile _SHADER_QUALITY_LOW _SHADER_QUALITY_MEDIUM _SHADER_QUALITY_HIGH
```

If such definition is missing in a shader, Unity uses the following default settings depending on the target platform:

* OpenGL ES 2.0/WebGL 1.0: __Low__.
* Mobile platforms: __Medium__.
* Other platforms: __High__.
