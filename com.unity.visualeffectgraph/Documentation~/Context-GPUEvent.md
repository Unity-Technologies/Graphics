<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This feature is currently experimental and is subject to change in later major versions. To use this feature, enable <b>Experimental Operators/Blocks</b> in the <b>Visual Effects</b> tab of your Project's Preferences.</div>

# GPU Event

Menu Path : **Context > GPUEvent**

The **GPU Event** Context allows you to spawn new particles from particular Blocks in Update or Initialize Contexts.

## Context settings

| **Settings** | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Evt**      | GPUEvent | Connection from a [Block](Blocks.md) that triggers a GPU Event. The Block that trigger a GPU Event are:<br/>&#8226; **Trigger Event Always**.<br/>&#8226; **Trigger Event On Die**.<br/>&#8226; **Trigger Event Rate** |

## Flow

| **Port**       | **Description**                                              |
| -------------- | ------------------------------------------------------------ |
| **SpawnEvent** | Connection to an [Initialize](Context-Initialize.md) Context.  Flow anchors from a Spawn Context and a GPU Event cannot mix. |
