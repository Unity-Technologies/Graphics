# Event Contexts reference

Trigger and manage the events that start and stop a visual effect.

These Contexts define where the processing workflow starts and ends in a graph and can also send data back out to the Visual Effect component.

| **Topic** | **Description** |
| --- | --- |
| [Event Context](Context-Event.md) | Define names for events that trigger actions in a graph. |
| [GPU Event Context](Context-GPUEvent.md) | Spawn new particles from specific Blocks in Update or Initialize Contexts. |
| Output Event Context | Outputs a CPU Spawn Event back to the Visual Effect component. For information on consuming these events, see [Output Event Handlers](OutputEventHandlers.md). |

## Additional resources

- [Contexts](Contexts.md)
- [Sending Events](ComponentAPI.md#sending-events)
