# Xor (Bitwise)

Menu Path : **Operator > Bitwise > Xor**

The Xor Operator takes two inputs and outputs the result of a bitwise logical Xor operation to each bit of a number in its binary form. For each bit in **A** and **B**, if only one of them is **1**, the output is **1**. If both of them are **1** or neither of them are **1**, the output is **0**.

For example, an input of 26 in the **A** input and 19 in the **B** input outputs 9. This is because, in binary, 26 is represented as 11010, and 19 is represented as 10011. Therefore the result is 01001, the binary representation of 9.

## Operator properties

| **Input** | **Type** | **Description**    |
| --------- | -------- | ------------------ |
| **A**     | uint     | The left operand.  |
| **B**     | uint     | The right operand. |

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| **o**      | uint     | The result of a bitwise *Xor* operation between **A** and **B**. |

## Details

The Xor operation is exclusive. This means that the binary form of both input values must either be the same length or the equivalent of adding two bits and discarding the carry.
