# Reciprocal Node

## Description

Returns the result of dividing 1 by the input **In**. This can be calculated by a fast approximation on Shader Model 5 by setting **Method** to **Fast**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Method      | Dropdown | Default, Fast | Selects the method used |

## Generated Code Example

The following example code represents one possible outcome of this node per **Method** mode.

**Default**

```
void Unity_Reciprocal_float4(float4 In, out float4 Out)
{
    Out = 1.0/In;
}
```

**Fast** (Requires Shader Model 5)

```
void Unity_Reciprocal_Fast_float4(float4 In, out float4 Out)
{
    Out = rcp(In);
}
```