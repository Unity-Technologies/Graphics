# Trigger Event Always

Menu Path : **GPU Event > Trigger Event Always**

The **Trigger Event Always** Block triggers the continual creation of a specified number of particles each frame via a [GPU Event](Context-GPUEvent.md). Trigger Block always execute at the end of Update, regardless of where the Block is on the [blackboard](Blackboard.md).



You can also use the Trigger Block with various conditions to create more complex spawning behavior. For example:

![A Visual Effect Graph window which shows the Trigger Event Always Block executing under complex conditions. An Age Over Lifetime Operator returns the age of a particle relative to its lifetime, which uses a Compare Operator to compare if the value is less than 0.1. The connected Branch then outputs 5 when the Predicate is true or 0 when the Predicate is false, which connects to the Trigger Event Always Block inside the Update Particle Context.](Images/Block-TriggerEventAlwaysExample.png)

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
