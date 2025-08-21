# Branch node

The Branch node adds a dynamic branch to the shader, which outputs a different value depending on whether the input is true or false. 

Both sides of the branch are evaluated in the shader, and the output from the unused path is discarded.

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:----------|:----------|:---------------|:--------|:------------|
| **Predicate** | Input | Boolean | None | The input to test the value of. If you input a float, all values are evaluated as `true` except `0`. |
| **True** | Input | Dynamic Vector | None | The value to output as **Out** if **Predicate** is true. |
| **False** | Input | Dynamic Vector | None | The value to output as **Out** if **Predicate** is false. |
| **Out** | Output | Dynamic Vector | None | Outputs either **True** or **False**. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}
```
