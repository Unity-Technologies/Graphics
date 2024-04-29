## Custom HLSL Nodes (block and operator)

These Custom HLSL nodes let you execute custom HLSL code during particle simulation.
You can use an [operator](Operator-CustomHLSL.md) for horizontal flow or [block](Block-CustomHLSL.md) for vertical flow (in contexts).
To be valid and correctly interpreted by the VFX Graph some conventions must be adopted.

## Node settings

| **Setting name**       | UI         | Location   | Action                         |
|------------------------|------------|------------|--------------------------------|
| **Name**               | Text field | Inspector  | Choose the name of the block   |
| **HLSL Code**          | Button     | Graph node | Opens a code editor window     |
| **Available Function** | Drop down  | Graph node | Pick which function to execute |


## HLSL Code
The HLSL code can be either embedded in the node or an HLSL file can be used.
In both cases you are allowed to include any other valid HLSL file (using #include "<path-to-the-file>").
You can also provide multiple functions in the same HLSL source (embedded or file), in this case, you'll have to pick the desired one in a choice list in the node.

## Function declaration
To be properly recognized by VFX Graph the function must fulfill the following requirements:
- Return a supported type [Supported types](#Supported-types)
- Each function parameter must have the `in`, `out` or `inout` access modifier
- Each function parameter must be of a [Supported types](#Supported-types)
- If you declare multiple functions, they must have unique names.
- **Function can take a maximum of 4 parameters**

## Inline documentation
You can specify a tooltip for each function parameter using the three slash comment notation as shown below:
```csharp
/// <parameter-name>: the tooltip's text
```

These comments must be right above the function declaration.
```csharp
/// a: the tooltip for parameter a
/// b: the tooltip for parameter b
float Distance(in float3 a, in float3 b)
{
	return distance(a, b);
}
```

You may want to write some helper function that you don't want to be exposed in the node's choice list. In that case, simply put this special comment:
```csharp
/// Hidden
```

## Supported types

| **HLSL Type**         | **Port Type**  | **Description**                                       |
|-----------------------|----------------|-------------------------------------------------------|
| **bool**              | bool           | A scalar value represented as a boolean.         |
| **uint**              | uint           | A scalar value represented as an unsigned integer.           |
| **int**               | int            | A scalar value represented as a integer.             |
| **float**             | float          | A scalar value represented as a float.           |
| **float2**            | Vector2        | A structure containing two float.                     |
| **float3**            | Vector3        | A structure containing three float.                   |
| **float4**            | Vector4        | A structure containing four float.                    |
| **float4x4**          | Matrix4x4      | A structure representing a matrix.                    |
| **VFXSampler2D**      | Texture2D      | A two-dimensional texture.                             |
| **VFXSampler3D**      | Texture3D      | A three-dimensional texture.                           |
| **VFXGradient**       | Gradient       | A structure that describes a gradient that can be sampled.                           |
| **VFXCurve**          | AnimationCurve | A structure that describes a curve that can be sampled. |
| **StructuredBuffer**  | GraphicsBuffer | A read-only buffer for storing an array of structures or basic HLSL data types. |
| **ByteAddressBuffer** | GraphicsBuffer | A read-only raw buffer. |

## Sampling

### Textures
To sample a texture you must use the VFX Graph structure called VFXSampler2D (or VFXSample3D) which is defined as shown below:
```csharp
struct VFXSampler2D
{
    Texture2D t;
    SamplerState s;
};
```
The easiest way to sample a texture is to use a function provided by the VFX Graph common HLSL code: `SampleTexture(VFXSampler2D texure, float2 coordinates)`.    
But you can also use HLSL built-in functions to sample a texture using the VFXSampler2D fields.    
In that case, since this is used in a compute shader you must specify which mipmap level to sample (use `SampleLevel` for instance).

### Buffers
You can use two types of buffers: `ByteAddressBuffer` and  `StructuredBuffer<>`.
In both cases the usage is the same as in any HLSL code:
- `ByteAddressBuffer`: use the `Load` function
````csharp
uint char = buffer.Load(attributes.particleId % count);
````
- `StructuredBuffer<>`: use classic index accessor
````csharp
float angle = phase + freq * buffer[attributes.particleId % bufferSize];
````

### Gradient
Gradients are handled specifically in VFX Graph (they are packed in a single texture) so you must use a dedicated function to sample them.    
Here is the function definition: `SampleGradient(VFXGradient gradient, float t)`
````csharp
float3 color = SampleGradient(grad, t);
````

### Curve
Sampling a curve is really similar to sampling a gradient.    
Here is the function definition: `SampleCurve(VFXCurve curve, float t)`
````csharp
float r = SampleCurve(curve, t);
````
