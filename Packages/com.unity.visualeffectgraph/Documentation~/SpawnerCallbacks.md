# Spawner Callbacks

**Note:** This feature is currently experimental and is subject to change in later major versions. To use this feature, enable **Experimental Operators/Blocks** in the **Visual Effects** tab of your Project's Preferences.

Spawner Callbacks is a C# API that allows you to define custom runtime behavior and create new Blocks for use in Spawn Contexts.

Spawner Callbacks allow you to:

* Control the Spawn Context state (Playing, Stopped, Delayed).
* Read/Write the Output Spawn Count.
* Read/Write SpawnEvent Attributes.

## Writing Spawner Callbacks

The full Spawner Callbacks API reference is available [here](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXSpawnerCallbacks.html).
