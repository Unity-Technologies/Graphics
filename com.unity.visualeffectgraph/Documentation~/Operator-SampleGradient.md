# Sample Gradient

Menu Path : **Operator > Sampling > Sample Gradient**

The **Sample Gradient** Operator samples a gradient at a specified time between 0 and 1.

## Operator properties

| **Input**    | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Gradient** | Gradient | The gradient this Operator samples.                          |
| **Time**     | float    | The time this Operator uses to sample the gradient. This value is clamped between 0 and 1. |

| **Output** | **Type** | **Description**                      |
| ---------- | -------- | ------------------------------------ |
| **s**      | Vector4  | The sampled value from the gradient. |
