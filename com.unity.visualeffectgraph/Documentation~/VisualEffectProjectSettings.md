# Visual Effect Project Settings

Visual Effect Graph Project Settings is a section in Unity Project Settings Window. You can access these settings in **Edit > Project Settings > VFX**.

![](Images/VisualEffectProjectSettings.png)

## Properties:

| Name                               | Description                                                  |
| ---------------------------------- | ------------------------------------------------------------ |
| Current Scriptable Render Pipeline | Displays Currently Used Render Pipeline Asset detected for VFX Graph Shader Compilation. |
| Fixed Time Step                    | Fixed Delay Before Simulation Steps for effects configured in **Fixed Delta Time** Simulation |
| Max Delta Time                     | Maximum Fixed Time Step allowed for simulation.              |
| Indirect Shader                    | (Automatically Set) Master Compute Shader used for Indirect Calls |
| Copy Buffer Shader                 | (Automatically Set) Compute Shader used for Compute Buffer Copy |
| Sort Shader                        | (Automatically Set) Compute Shader used for Particle Sorting |
| Strip Update Shader                | (Automatically Set) Compute Shader used for Particle Strips update |

> **Note:** Fixed Delta time works in asynchronous update with `deltaTime = N * FixedTimeStep` (with `deltaTime = min(deltaTime , MaxDeltaTime)`).
>
> N is determined by the current framerate.
>
> In this mode, deltaTime can equal 0 at certain frames.
