# The Water System Volume Override

This override makes it possible to reduce the memory consumption of all water surfaces in the same global volume with adjustments to [Levels of Detail](https://docs.unity3d.com/Manual/LevelOfDetail.html) (LODs) and tessellation.

It also provides a dimmer to control the effect of the [Ambient Probe](https://docs.unity3d.com/2022.2/Documentation/ScriptReference/RenderSettings-ambientProbe.html) on water surfaces in the scene.

## Patch and Grid
The Volume Override uses the terms Patch and Grid. The Patch is the size of the area on which Unity runs the simulation for a particular Simulation Band. The Grid is the geometry Unity uses to render the water, which is always a square.

## Use and adjust the Override

See [Use the Water System in your Project](WaterSystem-use.md) for information about how to enable the Volume Override. This is especially important when you upgrade your project from a version of Unity earlier than HDRP 14 (Unity 2022.2).

See [Quality and performance decisions](WaterSystem-QualityPerformance.md) for more information about how you can adjust LOD and tessellation properties to improve performance.

See [Settings and properties related to the Water System](WaterSystem-Properties.md#watervoloverride) for more information about the Volume Override's properties.


