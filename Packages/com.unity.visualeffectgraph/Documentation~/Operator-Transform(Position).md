# Transform (Position)

Menu Path : **Operator > Math > Geometry > Transform (Position)**

The **Transform (Position)** Operator applies a transformation to a position to offset, rotate, or scale it.

For example, this is useful if you want to spawn particles in a circle and then rotate the circle:

![Two examples of a VFX Graph. In each graph, the System starts with a Position: Circle Block. The graph has a Get Attribute Position (Current) Operator that inputs into a Transform Position Operator, then the Position input of a Set Position Block. In the first graph, the x-axis of Angles is set to 0, and the ring of particles faces the camera. In the second graph, the x-axis is set to 90, and the ring of particles faces up.](Images/Operator-Transform(Position)Example.png)

## Operator properties

| **Input**     | **Type**  | **Description**                                      |
| ------------- | --------- | ---------------------------------------------------- |
| **Transform** | Transform | The Transform this Operator applies to the Position. |
| **Position**  | Position  | The Position this Operator transforms.               |

| **Output** | **Type** | **Description**                         |
| ---------- | -------- | --------------------------------------- |
| **pos**    | Position | The result of the transformed Position. |
