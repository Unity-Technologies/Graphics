# Left Shift (Bitwise)

Menu Path : **Operator > Bitwise > Left Shift**

The **Left Shift** Operator shifts the first input's value left by the number of bits defined in the second input. During the shift, this Operator discards the most-significant bit and inserts a 0 on the right.

For example, if the first input is 21, which is 10101 in binary representation, and the number of bits to left shift it by is 3, the result is 168, which is 10101**000** in binary representation. Shifting left produces the same result as multiplying the input value by 2<sup>n</sup>, so the shift result is the same as:

21 \* 2<sup>3</sup>
21 \* 8
168

## Operator properties

| **Input** | **Type** | **Description**                       |
| --------- | -------- | ------------------------------------- |
| **A**     | uint     | The value to shift left.              |
| **B**     | uint     | The number of bits to shift **A** by. |

| **Output** | **Type** | **Description**                                       |
| ---------- | -------- | ----------------------------------------------------- |
| **o**      | uint     | The result of the left shift by **B** number of bits. |
