# Right Shift (Bitwise)

Menu Path : **Operator > Bitwise > Right Shift**

The **Right Shift** Operator shifts the first input's value right by the number of bits defined in the second input. During the shift, this Operator discards the least-significant bit and inserts a 0 on the left.

For example, if the first input is 83, which is 1010011 in binary representation, and the number of bits to right shift it by is 2, the result is 20 , which is 10100 in binary representation.  Shifting right produces the same result as dividing the input value by 2<sup>n</sup>, so the shift result is the same as:

83 / 2<sup>2</sup>
83 / 4
20


## Operator properties

| **Input** | **Type** | **Description**                       |
| --------- | -------- | ------------------------------------- |
| **A**     | uint     | The value to shift right.             |
| **B**     | uint     | The number of bits to shift **A** by. |

| **Output** | **Type** | **Description**                                        |
| ---------- | -------- | ------------------------------------------------------ |
| **o**      | uint     | The result of the right shift by **B** number of bits. |
