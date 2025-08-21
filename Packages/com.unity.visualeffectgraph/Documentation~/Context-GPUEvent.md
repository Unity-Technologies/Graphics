# GPU Event

Menu Path : **Context > GPUEvent**

The **GPU Event** Context allows you to spawn new particles from particular Blocks in Update or Initialize Contexts.

## Context settings

| **Settings** | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Evt**      | GPUEvent | Connection from a [Block](Blocks.md) that triggers a GPU Event. The [Trigger Event Block](Block-Trigger-Event.md) triggers GPU Events. |

## Flow

| **Port**       | **Description**                                              |
| -------------- | ------------------------------------------------------------ |
| **SpawnEvent** | Connection to an [Initialize](Context-Initialize.md) Context.  Flow anchors from a Spawn Context and a GPU Event cannot mix. |
