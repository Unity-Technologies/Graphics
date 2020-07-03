<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Draft:</b> The content on this page is complete, but it has not been reviewed yet.</div>
# Visual Effect Project Settings

Visual Effect Graph Project Settings is a Section in Unity Project Settings Window. You can access these settings using the **Edit/Project Settings** menu, then selecting **VFX** section.

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
> N being determined by the current framerate.
>
> In this mode, deltaTime can equal 0 at certain frames.