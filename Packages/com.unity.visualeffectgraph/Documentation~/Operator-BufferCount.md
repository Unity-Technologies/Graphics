# Buffer Count

**Menu Path : Operator > Sampling > Buffer Count**

The Buffer Count Operator enables you to get the number of element in the a [GraphicsBuffer](https://docs.unity3d.com/ScriptReference/GraphicsBuffer.html).

## Operator Properties

| **Input**  | **Type**        | **Description**                                              |
| ---------- | --------------- | ------------------------------------------------------------ |
| **Buffer** | GraphicsBuffer | The source GraphicsBuffer to get the number of elements in. You can only connect an exposed property to this input port. |


| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| **Count**  | UInt     | The number of element in the buffer. For more information, see [GraphicsBuffer.count](https://docs.unity3d.com/ScriptReference/GraphicsBuffer-count.html) |
