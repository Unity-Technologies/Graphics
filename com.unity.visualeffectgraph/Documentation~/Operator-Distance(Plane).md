# Distance (Plane)

Menu Path : **Operator > Math > Geometry > Distance (Plane)**

The **Distance (Plane)** Operator Operator takes a plane and a position and calculates:

- The closest point on the plane to the position.
- The distance between the closest point and the position.

## Operator properties

| **Input**    | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Plane**    | Plane    | The Plane this Operator evaluates. The point this Operator returns (**closestPosition**) is the closest point on this Plane to **Position**. |
| **Position** | Position | The Plane this Operator evaluates. The point this Operator returns (**closestPosition**) is the closest point to **Plane** from this position. |

| **Output**          | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **closestPosition** | Position | The position of the closest point on **Plane** to **Position**. |
| **distance**        | float    | The distance from **Position** to **closestPosition**.  If **Position** is above **Plane**, this value is positive. If **Position** is below **Plane**, this value is negative. |
