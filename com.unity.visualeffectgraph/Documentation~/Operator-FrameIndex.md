# Frame Index

Menu Path : **Operator > BuiltIn > Frame Index**

The **Frame Index** Operator outputs the internal Visual Effect manager frame index. This is equivalent to [Time.frameCount](https://docs.unity3d.com/ScriptReference/Time-frameCount.html). The VFX Graph uses this value internally to check the validity of motion vectors.

## Operator properties

| **Output**     | **Type** | **Description**          |
| -------------- | -------- | ------------------------ |
| **frameIndex** | uint     | The current frame index. |