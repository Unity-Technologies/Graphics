# Output Event Context

Menu Path: **Context** > **Output Event**

The **Output Event** Context sends a CPU Spawn Event back to the Visual Effect component whenever one or more connected [Spawn](Context-Spawn.md) Contexts trigger it. Unlike other Output Contexts, it doesn't render particles. Use it to run script logic in response to a graph event, such as spawning a prefab, playing a sound, or applying a physics impulse.

To consume Output Events in a script, use an [Output Event Handler](OutputEventHandlers.md).

This Context doesn't support Blocks. To attach Attributes to the event it sends, add [Set SpawnEvent \<Attribute>](Block-SetSpawnEvent.md) Blocks to the Spawn Contexts connected to it.

## Context settings

| **Setting**    | **Type** | **Description**                                              |
| -------------- | -------- | ------------------------------------------------------------ |
| **Event Name** | String   | The name of the event this Context sends to the Visual Effect component. The default value is **On Received Event**. |

## Flow

| **Port**  | **Description**                                                    |
| --------- | -------------------------------------------------------------------- |
| **Input** | Connection from one or more [Spawn](Context-Spawn.md) Contexts. |
