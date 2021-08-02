# Sample Bezier

Menu Path : **Operator > Math > Vector**

The **Sample Bezier** Operator performs a four-point bezier interpolation based on four positions and a value to represent the progress through the curve. It returns the interpolated position and the position's tangent, which is a vector which points along the bezier curve at the position.

![](Images/Operator-SampleBezierExample.gif)

## Operator properties

| **Input** | **Type** | **Description**                           |
| --------- | -------- | ----------------------------------------- |
| **T**     | float    | The progression through the bezier curve. |
| **A**     | Position | The source point of the Bezier            |
| **B**     | Position | The source tangent point of the bezier    |
| **C**     | Position | The target tangent point of the bezier    |
| **D**     | Position | The target point of the bezier            |

| **Output**   | **Type** | **Description**                                  |
| ------------ | -------- | ------------------------------------------------ |
| **Position** | Position | The interpolated position on the bezier segment. |
| **Tangent**  | Vector   | The interpolated tangent on the bezier segment.  |