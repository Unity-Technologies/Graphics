# Visual Effect Project Settings

Visual Effect Graph Project Settings is a section in Unity Project Settings Window. You can access these settings in **Edit > Project Settings > VFX**.

## Properties:

| Name                               | Description                                                  |
| ---------------------------------- | ------------------------------------------------------------ |
| Current Scriptable Render Pipeline | Displays the current Render Pipeline Asset that Unity uses to compile VFX Graph shaders. |
| Fixed Time Step                    | Sets the delay before simulation steps for any effect you configure in **Fixed Delta Time** Simulation. |
| Max Delta Time                     | Sets the maximum Fixed Time Step allowed for simulation.              |
| Max Scrub Time                     | Sets the maximum amount of time you can skip when you enable **Scrubbing** in the Timeline. |
| Max Capacity                       | Sets the maximum allocation count allowed by a system. Unity skips any system that requests an allocation count above this value. |
| Indirect Shader                    | Sets the master compute Shader used for Indirect Calls.<br/> Unity sets this value automatically.|
| Copy Buffer Shader                 | Sets the compute Shader used for Compute Buffer Copy.<br/> Unity sets this value automatically. |
| Sort Shader                        | Sets the compute Shader used for Particle Sorting.<br/> Unity sets this value automatically. |
| Strip Update Shader                | Sets the compute Shader used for Particle Strips update.<br/> Unity sets this value automatically. |
| Runtime Resources                  | Determines which runtime resources that VFX uses are available in runtime. For example, the [SDF Bake Tool](sdf-bake-tool.md).<br/> Unity sets this value automatically. |
| Batch Empty Lifetime           | Keeps an empty batch for a specified number of frames after deleting its last instance, allowing the creation of new instances. If no instances are created within this timeframe, the empty batch gets deleted. |

> **Note:** Fixed Delta time works in asynchronous update with `deltaTime = N * FixedTimeStep` (with `deltaTime = min(deltaTime , MaxDeltaTime)`). N is determined by the current framerate. In this mode, `deltaTime` can equal 0 at certain frames.
