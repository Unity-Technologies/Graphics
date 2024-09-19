# Custom HLSL Operator

Menu Path : **Operator > HLSL > Custom HLSL**

The **Custom HLSL Operator** allows you to write an HLSL function that takes **inputs** and produce **outputs**.
For general information about Custom HLSL nodes, refer to [Custom HLSL Nodes](CustomHLSL-Common.md).

## Specific constraints
- Function must either return a value or have at least one out/inout parameter
- Function do not support parameters of type `VFXAttributes`
- **Function can take a maximum of 4 parameters**

Each function parameter with no access modifier or `in`/`inout` access modifier will match an input port.    
The return value will match the operator output.
If you use the access modifier `out` or `inout` for some function parameters, then they will  generate an output port.

Here is an example of a valid function declaration:
```csharp
float Distance(in float3 a, in float3 b)
{
  return distance(a, b);
}
```

Another example with an `out` modifier:
```csharp
bool IsClose(in float3 a, in float3 b, float threshold, out float d)
{
  d = distance(a, b);
  if (d >= threshold)
    return true;
  return false;
}
```

A sample with comments
```csharp
/// a: the tooltip for parameter a
/// b: the tooltip for parameter b
float Distance(in float3 a, in float3 b)
{
  return distance(a, b);
}
```

You can also give a name to the output for the return value.    
In the sample below, the output port in the graph will be named `sqr`
```csharp
/// return: sqr
float Square(in float t)
{
  return t * t;
}
```

## Include other HLSL files
You can include any valid HLSL file with the standard `#include` directive.
The path to the included file can be:
- **Relative** to the VFX asset where the block will be used
- **Absolute** starting from the `Assets/` folder.
- **Absolute** starting from the `Packages/` folder.

For the Custom HLSL operator, the `#include` directive is only supported when used with hlsl file (not embedded code).

```c++
#include "HLSL/common.hlsl"

float SomeFunctionName(in float someValue)
{
    // use any function or variable declared in the included file
}
```
