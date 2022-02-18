# Fade Transition Node

## Description

Fade Transition is a method of adding noise to add variation while a function transitions from on to off. This node takes in a fade value and remaps it using the noise value (usually from a texture). When FadeValue is 0, the output is always 0, and when FadeValue is 1, the output is always exactly 1. In between 0 and 1 the transition will follow the pattern in the noise.

This [Node](Node.md) is commonly used as an input to **Alpha** on a [Master Node](Master-Node.md) to provide an LOD transition.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture      | Input | Texture 2D | None | Input value |
| Noise | Input | Float | None | The noise variation to apply to the fade function |
| FadeValue      | Input | Float | None | The amount of transition to apply |
| FadeContrast      | Input | Float | None | The contrast at which a single pixel goes from fully transparent to fully opaque. Higher values cause sharper edges in the transition |
| Fade | Output      |    Float | None | The resulting fade value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float Unity_FadeTransitionNode_ApplyFade_float(float noise, float fadeValue, float fadeContrast)
{
    float ret = saturate(fadeValue*(fadeContrast+1)+(noise-1)*fadeContrast);
    return ret;
}

float Result = Unity_FadeTransitionNode_ApplyFade_float(
        _NoiseValue,
        _FadeValue,
        _FadeContrast);
```
