# Custom HLSL Block

Menu Path : **HLSL > Custom HLSL**

The **Custom HLSL** Block allows you to write an HLSL function that takes inputs and can read and write to particle attributes.
For general information about Custom HLSL nodes, refer to [Custom HLSL Nodes](CustomHLSL-Common.md).

## Block compatibility
This Block is compatible with the following Contexts:

- [Init](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

## Custom HLSL requirements
A Custom HLSL Block must meet the following requirements:
- The return type is `void`.
- There must be one parameter of type `VFXAttributes` with access modifier `inout`

Here is an example of a valid function declaration:
```csharp
void Move(inout VFXAttributes attributes, in float3 offset)
{
  attributes.position += offset;
}
```

Another sample with documentation
```csharp
/// a: The gradient to sample
/// b: The sampling interpolation parameter
void ApplyGradient(inout VFXAttributes attributes, in VFXGradient gradient, in float t)
{
  attributes.color = SampleGradient(gradient, t);
}
```

## Particle attributes
Use the Custom HLSL block to alter any writable particle attribute with a custom algorithm.    
For performance reasons it's important for VFX Graph to detect which attributes are read and which are written.    
By convention the `VFXAttributes` function's parameter is named `attributes` (like in the example above) but you can name it as you wish.

To find all available attributes and their access rights, refer to [reference attributes](Reference-Attributes.md).

## Use macros to generate random numbers
VFX Graph exposes the following macros that you can use to generate random numbers:

| **Macro**      | **Type** | **Description**                                              |
|----------------|----------|--------------------------------------------------------------|
| VFXRAND        | float    | Generate a random scalar value for each particle.            |
| VFXRAND2       | float2   | Generate a random 2D vector value for each particle.         |
| VFXRAND3       | float3   | Generate a random 3D vector value for each particle.         |
| VFXRAND4       | float4   | Generate a random 4D vector value for each particle.         |


To generate a random scalar value (range from 0 to 1) for each particle, use the following syntax:
```csharp
float randomValue = VFXRAND;
```


## Use the same HLSL code in multiple VFX Graph systems
If you share the same HLSL code in multiple systems you may have compilation errors because the `VFXAttributes` may have different definition (depending on the particle layout).
To overcome this compilation error you can wrap the function with conditional compilation macro specifying which attribute is required.
For example, the following code uses `VFX_USE_COLOR_CURRENT` and `VFX_USE_VELOCITY_CURRENT` to check for the `velocity` and `color` attributes:
````csharp
#if defined(VFX_USE_COLOR_CURRENT) && defined(VFX_USE_VELOCITY_CURRENT)
void Speed(inout VFXAttributes attributes, in float speedFactor, in VFXGradient grad)
{
  float coef = min(speedFactor * length(attributes.velocity), 1);
  attributes.color = SampleGradient(grad, coef);
}
#endif
````

## Include other HLSL files
You can include any valid HLSL file with the standard `#include` directive.
The path to the included file can be:
- **Relative** to the VFX asset where the block will be used
- **Absolute** starting from the `Assets/` folder.
- **Absolute** starting from the `Packages/` folder.

For the Custom HLSL block, you can use the `#include` directive in both cases, embedded code or hlsl file.

```c++
#include "HLSL/common.hlsl"

void SomeFunctionName(inout VFXAttributes attributes, in float someValue)
{
...
}
```
