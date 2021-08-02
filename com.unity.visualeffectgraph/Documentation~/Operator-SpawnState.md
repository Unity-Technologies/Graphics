# Spawn State

Menu Path : **Operator > Spawn > Spawn Context State**

The **Spawn State** Operator contains information about the [state](https://docs.unity3d.com/ScriptReference/VFX.VFXSpawnerState.html) of a [Spawn](Context-Spawn.md) system. It contains information such as: the number of particles spawned in the current frame, the duration of the spawn loop, and the current delta time.

You can only connect this Operator's outputs to Blocks in a [Spawn Context](Context-Spawn.md). If you connect an output to a Block in another Context type, Unity throws an exception.

## Operator properties

| **Output**          | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **NewLoop**         | Boolean  | Indicates whether or not a new spawn loop has started this frame. If a new loop has started, this outputs `true`. Otherwise, outputs `false`. |
| **LoopState**       | Uint     | The current state of the spawn loop. Each value refers to a different state. The values are:<br/>&#8226; **0**: Not looping.<br/>&#8226; **1**: Delaying before a loop.<br/>&#8226; **2**: Is looping.<br/>&#8226; **3**: Delaying after a loop.<br/>For more information on spawn loop states, see [VFXSpawnerLoopState](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXSpawnerLoopState.html). |
| **LoopIndex**       | int      | The current index of the loop. Unity increments this number every time a new spawn loop starts. |
| **SpawnCount**      | float    | The number of particles the system spawned in the current frame. |
| **SpawnDeltaTime**  | float    | The delta time of the current frame. You can modify this value using a custom spawner. |
| **SpawnTotalTime**  | float    | The total time since the application started.                |
| **LoopDuration**    | float    | The loop duration specified in the connected system's [Spawn Context](Context-Spawn.md). |
| **LoopCount**       | int      | The loop count specified in the connected system's [Spawn Context](Context-Spawn.md). |
| **DelayBeforeLoop** | float    | The time the VFXSpawner waits for before it starts a new loop. This value is specified in the connected system's [Spawn Context](Context-Spawn.md). |
| **DelayAfterLoop**  | float    | The time the VFXSpawner waits for after it finishes a loop. This value is specified in the connected system's [Spawn Context](Context-Spawn.md). |