# Trigger Event On Die

Menu Path : **GPU Event > Trigger Event On Die**

The **Trigger Event On Die** Block triggers the creation of a specified number of particles via a [GPU Event](Context-GPUEvent.md) when a particle dies. Trigger Blocks always execute at the end of Update, regardless of where the Block is in the Context.

You can also use the Trigger Block with various conditions to create more complex spawning behavior:

![img](Images/Block-TriggerEventOnDieExample.png)

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)

## Block properties

| **Input** | **Type** | **Description**                                              |
| --------- | -------- | ------------------------------------------------------------ |
| **Count** | Uint     | The number of GPU Event particles to spawn once a particle dies. |

| **Output** | **Type** | **Description**                                              |
| --------- | -------- | ------------------------------------------------------------ |
| **Evt** | [GPU Event](Context-GPUEvent.md)     | The GPU Event to trigger. |
