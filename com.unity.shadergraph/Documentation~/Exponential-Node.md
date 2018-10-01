## Description

Returns the exponential value of input **In**. The exponential base can be switched between base-e and base 2 from the **Base** dropdown on the node. 

* **Base E** : Returns e to the power of input **In**
* **Base 2** : Returns 2 to the power of input **In**

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Base      | Dropdown | BaseE, Base2 | Selects the exponential base |

## Shader Function

**Base E**

`Out = exp(In)`

**Base 2**

`Out = exp2(In)`