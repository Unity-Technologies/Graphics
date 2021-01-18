# Spawn Over Distance

Menu Path: **Spawn > Custom > Spawn Over Distance**

The **Spawn Over Distance** Block calculates the displacement of a position relative to the previous frame's value. Depending on this size of the displacement and the **Rate Per Unit** property, the system spawns a particle.

![](Images/Block-SpawnOverDistanceExample.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Spawn](Context-Spawn.md)

## Block properties

| **Input**              | **Type** | **Description**                                              |
| ---------------------- | -------- | ------------------------------------------------------------ |
| **Position**           | Vector3  | The reference position to use to check whether to spawn a particle or not. The system automatically stores this value in the position spawn state attribute. It stores the previous value in the oldPosition spawn state attribute. |
| **Rate per Unit**      | float    | The number of particles to spawn per unit of displacement.   |
| **Velocity Threshold** | float    | The maximum velocity to consider for spawning. If the position moves faster than this threshold, the block does not spawn anymore particles. |

## Remarks

This Block uses a VFXSpawnerCallback interface and you can use it as a reference to create your own implementation. The implementation for this Block is in **com.unity.visualeffectgraph > Runtime > CustomSpawners > SpawnOverDistance.cs**.