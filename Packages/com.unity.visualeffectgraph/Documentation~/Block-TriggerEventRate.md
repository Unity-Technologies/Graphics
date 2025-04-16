# Trigger Event Rate

Menu Path : **GPU Event > Trigger Event Rate**

The **Trigger Event Rate** Block triggers the creation of particles via a GPU Event using a specified rate. You can set the rate to spawn particles over time (per second) or over distance (parent particle distance change).

<video src="Images/Block-TriggerEventRateExample.mp4" title="Top: A circle divided into quadrants features a green pointer moving vertically upward and a red pointer extending horizontally to the right. As the red pointer sweeps from left to right, smaller circles spawn in its path, remaining stationary and shrinking until they disappear. Bottom: A circle moves from left to right, followed by twelve smaller, evenly spaced versions of itself, decreasing in size from right to left. The smallest, on the far left, is just a dot. All circles move at the same speed and in the same direction." width="320" height="auto" autoplay="true" loop="true" controls></video>

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
