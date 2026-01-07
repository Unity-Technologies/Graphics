# Fraction node

The Fraction node returns the fractional part of an input, also known as the decimal part. The range is 0 to 1. For example, the fractional part of 3.75 is 0.75.

The Fraction node calculates the result using the following formula:

```
fractional_part = input - floor(input)
```

As a result, the node returns a positive value if you input a negative number. For example, the fractional part of -3.75 is 0.25.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Fraction_float4(float4 In, out float4 Out)
{
    Out = frac(In);
}
```
