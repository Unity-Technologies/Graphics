# Set Spawn Time

Menu Path: **Spawn > Custom > Set Spawn Time**

The **Set Spawn Time** Block allows following Initialize Contexts to use the time since the spawn Context’s last play event (see [totalTime](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXSpawnerState-totalTime.html)).

![img](Images/Block-SetSpawnTimeExample.gif)

In this example, the left system uses source spawnTime, which resets for each start event of the previous spawn context. The right system uses VFX total time which is simply the accumulation of deltaTime since Visual Effect component activation.

## Block compatibility

This Block is compatible with the following Contexts:

- [Spawn](Context-Spawn.md)

## Remarks

This Block uses a VFXSpawnerCallback interface and you can use it as a reference to create your own implementation. The implementation for this Block is in **com.unity.visualeffectgraph > Runtime > CustomSpawners > SetSpawnTime.cs**.