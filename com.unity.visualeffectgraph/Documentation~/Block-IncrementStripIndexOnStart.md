# Increment Strip Index On Start

Menu Path: **Spawn > Custom > Increment Strip Index On Start**

The **Increment Strip Index On Start** Block helps to manage the initialization of Particle Strips. This Block  increments the stripIndex attribute (unsigned integer) each time the start event of the Spawn Context triggers. The stripIndex attribute returns to zero when a stop event triggers or if stripIndex reaches the **Strip Max Count**.

![](Images/Block-IncrementStripIndexOnStartExample.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Spawn](Context-Spawn.md)

## Block properties

| **Input**           | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **Strip Max Count** | uint     | The maximum value stripIndex can be. The range that stripIndex can be is between zero and this value minus one. |

## Remarks
This Block uses a VFXSpawnerCallback interface and you can use it as a reference to create your own implementation. The implementation for this Block is in **com.unity.visualeffectgraph > Runtime > CustomSpawners > IncrementStripIndexOnStart.cs**.