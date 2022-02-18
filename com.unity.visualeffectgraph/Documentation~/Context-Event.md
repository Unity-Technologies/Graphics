# Event

Menu Path : **Context > Event**

The **Event** Context defines input event names. It doesn’t necessarily require unique naming; you can duplicate the same event name in different places within the graph.

## Context settings

| **Setting**    | **Type** | **Description**                                              |
| -------------- | -------- | ------------------------------------------------------------ |
| **Event Name** | String   | The name of the event. This name appears in the list of names that [GetOutputEventNames](https://docs.unity3d.com/2020.2/Documentation/ScriptReference/VFX.VisualEffect.GetOutputEventNames.html) returns. The default value, **OnPlay**, is the name of the event that the VFX Graph sends to the [Spawn](Context-Spawn.md) Context by default.<br/>The **Send** button gets every active Visual Effect component within the currently open scene and uses [VisualEffect.SendEvent](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.SendEvent.html) to send this event to them all. This means **Send** also affects components which don't use the Visual Effect Asset you are editing. |

## Flow

| **Port**   | **Description**                                    |
| ---------- | -------------------------------------------------- |
| **Output** | Connection to a [Spawn](Context-Spawn.md) Context. |
