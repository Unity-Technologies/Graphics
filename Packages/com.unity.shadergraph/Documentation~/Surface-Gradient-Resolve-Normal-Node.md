# Surface Gradient Resolve Normal node

The Surface Gradient Resolve Normal node resolves a surface gradient into a world-space normal. It subtracts the surface gradient from the base normal and normalizes the result.

## Render pipeline compatibility

| **Node**                              | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------------------------------- | ------------------------------------ | ------------------------------------------- |
| **Surface Gradient Resolve Normal**   | No                                   | Yes                                         |

## Ports

| **Name**             | **Direction** | **Type** | **Binding** | **Description**                                                                 |
| -------------------- | ------------- | -------- | ----------- | -------------------------------------------------------------------------------- |
| **Normal**           | Input         | Vector 3 | None        | The base normal vector to perturb, typically the vertex or geometric normal.    |
| **SurfaceGradient**  | Input         | Vector 3 | None        | The surface gradient that encodes the normal perturbation.                      |
| **NormalOutput**     | Output        | Vector 3 | None        | The resolved perturbed normal, computed as `SafeNormalize(Normal - SurfaceGradient)`. |

## Generated code example

```hlsl
float3 NormalOutput = SafeNormalize(Normal - SurfaceGradient);
```

## Additional resources

- [Surface Gradient-Based Normal Mapping Framework](https://jcgt.org/published/0009/03/04/)