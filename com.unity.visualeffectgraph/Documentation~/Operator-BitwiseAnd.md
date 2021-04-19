# And (Bitwise)

Menu Path : **Operator > Bitwise > And**

The **And** Operator takes two inputs and outputs the result of a bitwise *And* operation between them. For each bit in **A** and **B**, if both are **1**, the output is **1**. Otherwise, the output is **0**.

For example, an input of 26 in the **A** input and 19 in the **B** input outputs 18. This is because, in binary, 26 is represented as 11010, and 19 is represented as 10011. Therefore the result is 10010, the binary representation of 18.

## Operator properties

| **Input** | **Type** | **Description**    |
| --------- | -------- | ------------------ |
| **A**     | uint     | The left operand.  |
| **B**     | uint     | The right operand. |

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| **o**      | uint     | The result of a bitwise *And* operation between **A** and **B.** |