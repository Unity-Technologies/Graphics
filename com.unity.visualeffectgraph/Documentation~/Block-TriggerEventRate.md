# Trigger Event Rate

Menu Path : **GPU Event > Trigger Event Rate**

The **Trigger Event Rate** Block triggers the creation of particles via a GPU Event using a specified rate. You can set the rate to spawn particles over time (per second) or over distance (parent particle distance change).

![](Images/Block-TriggerEventRateExample.gif)

Trigger Blocks always execute at the end of the [Update Context](Context-Update.md), regardless of where the Block is situated within the Context.

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)

## Block settings

| **Setting** | **Type** | **Description**                                              |
| ----------- | -------- | ------------------------------------------------------------ |
| **Mode**    | Enum     | The method this Block uses to apply the **Rate**. The options are:<br/>&#8226; **Over Time**: Applies the **Rate** over time.<br/>&#8226; **Over Distance**: Applies the **Rate** over distance. |

## Block properties

| **Input** | **Type** | **Description**                                              |
| --------- | -------- | ------------------------------------------------------------ |
| **Rate**  | Float    | The number of GPU Event particles to spawn based on the **Mode**.<br/>If you set **Mode** to **Over Time**, this is the number of GPU Event particles to spawn per second.<br/>If you set **Mode** to **Over Distance**, this is the number of GPU Event particles to spawn as the parent particle moves. |

| **Output** | **Type**                         | **Description**           |
| ---------- | -------------------------------- | ------------------------- |
| **Evt**    | [GPU Event](Context-GPUEvent.md) | The GPU Event to trigger. |
