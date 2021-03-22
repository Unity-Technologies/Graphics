# Comparison Node

## Description

Compares the two input values **A** and **B** based on the condition selected on the dropdown. This is often used as an input to the [Branch Node](Branch-Node.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| A      | Input | Float    | None | First input value |
| B      | Input | Float    | None | Second input value |
| Out  | Output  | Boolean  | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|      | Dropdown | Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual | Condition for comparison |

## Generated Code Example

The following example code represents one possible outcome of this node per comparison type.

**Equal**

```
void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ? 1 : 0;
}
```

**NotEqual**

```
void Unity_Comparison_NotEqual_float(float A, float B, out float Out)
{
    Out = A != B ? 1 : 0;
}
```

**Less**

```
void Unity_Comparison_Less_float(float A, float B, out float Out)
{
    Out = A < B ? 1 : 0;
}
```

**LessOrEqual**

```
void Unity_Comparison_LessOrEqual_float(float A, float B, out float Out)
{
    Out = A <= B ? 1 : 0;
}
```

**Greater**

```
void Unity_Comparison_Greater_float(float A, float B, out float Out)
{
    Out = A > B ? 1 : 0;
}
```

**GreaterOrEqual**

```
void Unity_Comparison_GreaterOrEqual_float(float A, float B, out float Out)
{
    Out = A >= B ? 1 : 0;
}
```
