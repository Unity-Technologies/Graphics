# Multiply Node

## Description

Returns the result of input **A** multiplied by input **B**. If both inputs are a vector type, the output type will be a vector type with the same dimension as the evaluated type of those inputs. If both inputs are a matrix type, the output type will be a matrix type with the same dimension as the evaluated type of those inputs. If one input is a vector type and the other is a matrix type, then output type will be a vector with the same dimension as the vector type input. 

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic | First input value |
| B      | Input      |   Dynamic | Second input value |
| Out | Output      |    Dynamic | Output value |

## Generated Code Example

The following example code represents different possible outcomes of this node.

**Vector * Vector**

```
void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
    Out = A * B;
}
```

**Vector * Matrix** 

```
void Unity_Multiply_float4_float4x4(float4 A, float4x4 B, out float4 Out)
{
    Out = mul(A, B);
}
```

**Matrix * Matrix** 

```
void Unity_Multiply_float4x4_float4x4(float4x4 A, float4x4 B, out float4x4 Out)
{
    Out = mul(A, B);
}
```