# Distance (Sphere)

Menu Path : **Operator > Math > Geometry > Distance (Sphere)**

The **Distance (Sphere)** Operator takes a sphere and a position and calculates:

- The closest point on the sphere to the position.
- The distance between the closest point and the position.

## Operator properties

| **Input**    | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Sphere**   | Sphere   | The Sphere this Operator evaluates. The point this Operator returns (**closestPosition**) is the closest point on this Sphere to **Position**. |
| **Position** | Position | The Position this Operator evaluates. The point this Operator returns (**closestPosition**) is the closest point to **Sphere** from this position. |

| **Output**          | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **closestPosition** | Position | The position of the closest point on **Sphere** to **Position**. |
| **distance**        | float    | The distance from **Position** to **closestPosition**. If **Position** is outside **Sphere**, this value is positive. If **Position** is inside **Sphere**, this value is negative. |
