# Spawn Blocks

Spawn Blocks are dedicated to Spawn contexts. They are designed to handle SpawnEvents and do not compile into GPU code. Instead, they execute on CPU at runtime and enable triggering of new GPU particles

## Built-in Blocks

### Constant Rate

Constant Rate emits N particles per second, depending of the Rate inputProperty

### Variable Rate

Variable rate outputs a linearly-interpolated variable rate over time based on a minimum and maximum period of time, and a minimum and maximum rate.

> **How it works :** Every time an interpolation completes, a new period of time will be recomputed with a target random rate. during this period of time, rate will linearly interpolate from previous rate to the newly computed rate.

![](Pages/VFXEditor/img/variable-rate.png)

### Single Burst

Single Bursts provides features to spawn a given amount of particles at a given time, only once.

**Settings:**

* Spawn Mode : *Constant/Random* - Used to control whether the burst will emit a constant or a random count.
* Delay Mode: *Constant/Random* - Used to control whether the burst will happen at the same time or with a random delay.

### Periodic Burst

Periodic burst has slightly the same behavior as Single Burst but will happen repeatedly over time. Random delay and/or count will happen at every burst.

## Set EventAttribute

Set EventAttribute is a Spawn-only block that enables the storage of SpawnEvent Attributes. As attributes can be set from the C# component API, they can also be set from the spawn contexts to compute values at spawn-time instead of initialize time. 

> Example: a Set SpawnEvent random for a periodic burst will compute a random position every time the spawn context will output a value, in our case every burst. This way you can factorize random, per-burst instead of per-particle.

## Custom Spawn Blocks

Custom Spawn Blocks are spawn functions written in C#, enabling the user to perform custom runtime logic at Spawn level. These blocks are executed at every frame and contains special functions executed when a spawn context is activated or deactivated.

<u>These blocks can be used for many purposes:</u>

* Control spawn output (including reading output of previous blocks)
* Control event attribute output
* Control Spawn Context (Play, Stop, access to *DeltaTime* and *TotalTime*)
