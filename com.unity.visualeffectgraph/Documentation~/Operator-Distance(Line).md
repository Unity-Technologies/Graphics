# Distance (Line)

Menu Path : **Operator > Math > Geometry > Distance (Line)**

The **Distance (Line)** Operator takes a line and a position and calculates:

* The closest point on the line to the position.
* The distance between the closest point and the position.

## Operator properties

| **Input**    | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Line**     | Line     | The Line this Operator evaluates. The point this Operator returns (**closestPosition**) is the closest point on this line to **Position**. |
| **Position** | Position | The Position this Operator evaluates. The point this Operator returns (**closestPosition**) is the closest point to **Line** from this position. |

| **Output**          | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **closestPosition** | Position | The position of the closest point on **Line** to **Position**. |
| **distance**        | float    | The distance from **Position** to **closestPosition**.       |