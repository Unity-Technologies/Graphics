# Fade Dither Node

## Description

Fade Dither is a method of adding noise to transition a texture to/from on and off. This node takes in a noise texture and remaps the texture values. When FadeValue is 0, the output is always 0, and when FadeValue is 1, the output is always exactly 1. In between 0 and 1 the transition will follow the pattern in the texture.

This [Node](Node.md) is commonly used as an input to **Alpha** on a [Master Node](Master-Node.md) to provide an LOD transition.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture      | Input | Texture 2D | None | Input value |
| UV      | Input | Vector 2 | None | UV coordinate of the texture lookup, which is multiplied with PixelRatio |
| FadeValue      | Input | Float | None | The amount of transition to apply |
| FadeSpeed      | Input | Float | None | The speed at which a texel goes from fully transparent to fully opaque. Higher values cause sharper edges in the transition |
| PixelRatio      | Input | Float | None | Scale for the texture resolution |
| Fade | Output      |    Float | None | The resulting fade value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```

float Unity_FadeDitherNode_ApplyFade_float(float noise, float fadeValue, float fadeSpeed)
{
    float ret = saturate(fadeValue*(fadeSpeed+1)+(noise-1)*fadeSpeed);
    return ret;
}

float result = Unity_FadeDitherNode_ApplyFade_float(
        SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(_FadeDither_Texture).tex,
        UnityBuildTexture2DStructNoScale(_FadeDither_Texture).samplerstate,
        UnityBuildTexture2DStructNoScale(_FadeDither_Texture).GetTransformedUV((_InputUv.xy))*_PixelRatio).x,
        _FadeValue,
        _FadeSpeed);
```
