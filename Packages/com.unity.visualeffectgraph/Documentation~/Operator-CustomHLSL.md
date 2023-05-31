# Custom HLSL Operator

Menu Path : **Operator > HLSL > Custom HLSL**

The **Custom HLSL Operator** allows you to write an HLSL function that takes **inputs** and produce **outputs**.
See common documentation for *Custom HLSL Operator* and *Custom HLSL Block* [here](CustomHLSL-Common.md).

## Function specific constraints
- Return type cannot be `void`
- Function parameters must not be of type `VFXAttributes`

To each function parameter will match an operator input, and the return value will match the operator output.
If you use the access modifier `out` or `inout` for some input parameters, then they will also generate an output port.

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
