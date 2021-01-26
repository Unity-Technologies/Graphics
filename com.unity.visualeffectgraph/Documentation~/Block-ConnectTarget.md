# Connect Target

Menu Path : **Orientation > Connect Target**

The **Connect Target** Block scales and orients particles so they connect to a specified target position.

![](Images/Block-ConnectTargetExample.gif)

The Block also allows you to specify the particle pivot in relation to its position and target position. This is useful for example to specify a custom point of rotation along the particle:

![](Images/Block-ConnectTargetPivotShift.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- Any output Context

## Block settings

| **Setting**     | **Type** | **Description**                                              |
| --------------- | -------- | ------------------------------------------------------------ |
| **Orientation** | Enum     | Specifies how the particle orients itself. The options are:<br/>&#8226;  **Camera**: The particle faces the camera.<br/>&#8226;  **Direction**: The particle faces a particular direction.<br/>&#8226;  **Look At Position**: The particle faces a position in the scene. |

## Block properties

| **Input**            | **Type**                       | **Description**                                              |
| -------------------- | ------------------------------ | ------------------------------------------------------------ |
| **Target Position**  | [Position](Type-Position.md)   | The position with which the particle is connecting.          |
| **Look Direction**   | [Direction](Type-Direction.md) | The direction the particle faces.<br/>This property only appears if you set **Orientation** to **Direction**. |
| **Look At Position** | [Position](Type-Position.md)   | The position the particle orients itself to look towards.<br/>This property only appears if you set **Orientation** to **Look At Position**. |
| **Pivot Shift**      | float                          | The pivot relative to the length of the particle. A value of 0 sets it at the particle position, while a value of 1 sets it at the **Target Position**. |
