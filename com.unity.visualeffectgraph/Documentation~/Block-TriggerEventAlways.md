# Trigger Event Always

Menu Path : **GPU Event > Trigger Event Always**

The **Trigger Event Always** Block triggers the continual creation of a specified number of particles each frame via a [GPU Event](Context-GPUEvent.md). Trigger blocks always execute at the end of Update, regardless of where the block is on the [blackboard](Blackboard.md).



You can also use the Trigger block with various conditions to create more complex spawning behavior. For example:

![](Images/Block-TriggerEventAlwaysExample.png)

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)

## Block properties

| **Input** | **Type** | **Description**                                         |
| --------- | -------- | ------------------------------------------------------- |
| **Count** | Uint     | The number of GPU Event particles to spawn every frame. |

| **Output** | **Type**                         | **Description**           |
| ---------- | -------------------------------- | ------------------------- |
| **Evt**    | [GPU Event](Context-GPUEvent.md) | The GPU Event to trigger. |
