# Expression node

Use the Expression node to specify a complex mathematical expression as a string instead of using multiple [Math nodes](Math-Nodes.md).

## Input ports

The number and type of input ports automatically adjust to the values of the [node controls](#controls). Unity adds an input port for each variable in the expression in the text field.

## Output port

| **Name** | **Type** | **Description** |
| :--- | :--- | :--- |
| **Out** | Depends on the selected **Type** in the [node controls](#controls). | The value resulting from the expression specified in the text field. |

## Controls

| **Name** | **Description** |
| :--- | :--- |
| **Type** | Specifies the data type to use for all input and output ports of the node. The options are:<ul><li>**Vector 1**</li><li>**Vector 2**</li><li>**Vector 3**</li><li>**Vector 4**</li></ul> |
| (Text field) | Use this field to specify the mathematical expression. The field supports the following:<ul><li>Math operators: `+`, `-`, `*`, `/`, and parentheses.</li><li>HLSL intrinsic functions: for example, `frac` and `saturate`.</li><li>Vector swizzling: for example, `a.xw`.</li><li>Vector construction: `float2(x, y)`.</li></ul> |

## Example

If you specify the expression:

`a + b * c / 2`

* Unity adds three input ports to the node: **A**, **B**, and **C**.
* If you input 1, 2, and 10, the node calculates `1 + 2 * 10 / 2` and outputs the result to the **Out** port.

**Note**: If you use Shader Graph math nodes instead, the example requires three separate nodes: [Divide](Divide-Node.md), [Multiply](Multiply-Node.md), and [Add](Add-Node.md).