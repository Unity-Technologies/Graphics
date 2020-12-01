# Sample Curve

Menu Path : **Operator > Sampling > Sample Curve**

The **Sample Curve** Operator samples a curve at a specified time. When it samples a curve, it takes left and right repeat modes of the input curve into account.

## Operator properties

| **Input** | **Type** | **Description**                                  |
| --------- | -------- | ------------------------------------------------ |
| **Curve** | Curve    | The curve this Operator samples from.            |
| **Time**  | float    | The time this Operator uses to sample the curve. |

| **Output** | **Type** | **Description**                         |
| ---------- | -------- | --------------------------------------- |
| **s**      | float    | The sampled value from the input curve. |

## Limitations

When Unity evaluates this Operator on the GPU, it doesn’t take ping pong mode into account. In this case, the sampling mode defaults to clamp.
