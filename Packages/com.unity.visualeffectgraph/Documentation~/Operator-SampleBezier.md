# Sample Bezier

Menu Path : **Operator > Math > Vector**

The **Sample Bezier** Operator performs a four-point bezier interpolation based on four positions and a value to represent the progress through the curve. It returns the interpolated position and the position's tangent, which is a vector which points along the bezier curve at the position.

<video src="Images/Operator-SampleBezierExample.mp4" title="Dynamic sampling process of a cubic Bézier curve, showing control points, interpolation, and the movement of a marker along the curve as the T parameter changes." width="320" height="auto" autoplay="true" loop="true" controls></video>

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
