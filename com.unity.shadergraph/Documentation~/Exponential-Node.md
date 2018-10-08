# Exponential Node

## Description

Returns the exponential value of input **In**. The exponential base can be switched between base-e and base 2 from the **Base** dropdown on the node. 

* **Base E** : Returns e to the power of input **In**
* **Base 2** : Returns 2 to the power of input **In**

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Base      | Dropdown | BaseE, Base2 | Selects the exponential base |

## Generated Code Example

The following example code represents one possible outcome of this node per **Base** mode.

**Base E**

```
void Unity_Exponential_float4(float4 In, out float4 Out)
{
    Out = exp(In);
}
```

**Base 2**

```
void Unity_Exponential2_float4(float4 In, out float4 Out)
{
    Out = exp2(In);
}
```