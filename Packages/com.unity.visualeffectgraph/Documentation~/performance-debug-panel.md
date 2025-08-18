# Performance and optimization

To optimize performance, use the **Profiling** and **Debug** panels.

The **Profiling** and **Debug** panels provide useful information about your running Visual Effects, such as CPU and GPU timings, memory usage, texture usage, and various states. These allow you to keep the performance of your effects under control while you author them.

To enable the **Profiling** and **Debug** panels, follow these steps:

1. Attach the **Visual Effect Graph** window to a GameObject that has a **Visual Effect** component. For more information, refer to [Attaching a Visual Effect](GettingStarted.md#attaching-a-visual-effect-from-the-scene-to-the-current-graph).
2. Select the debug icon in the top-right of the **Visual Effect Graph** window.

All the information displayed in the **Profiling** and **Debug** panels refers to the attached GameObject.

## Graph Debug Information

The Graph Debug Information panel provides information relative to the entire graph.

| Section               | Description                                                                                                                                                                                                                                                                                             |
|-----------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **CPU Information**   | <li>Full Update: Indicates the update time of the entire graph on CPU in milliseconds. <li>Evaluate Expressions: Indicates the time spent evaluating the parameters of the graph that are computed on the CPU.</li></li> <li>System name: Update time of a specific system on CPU in milliseconds.</li> |
| **GPU Information**   | <li> GPU Time: Execution time of the Visual Effect on GPU in milliseconds. </li>   <li> GPU Memory: GPU memory usage of the Visual Effect. </li>                                                                                                                                                        |
| **Texture Usage**     | For each system, lists the textures used along with their dimension and memory size.                                                                                                                                                                                                                    |
| **Heatmap parameter** | <li>GPU Time Threshold (ms): This controls the value, in milliseconds, above which the execution times in the panels will turn red. Adjust this value to easily identify expensive parts for your graph.     </li>                                                                                      |

Shortcuts to the **Rendering Debugger**  [in URP](https://docs.unity3d.com/Manual/urp/features/rendering-debugger.html) or [in HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/rendering-debugger-window-reference.html) and to the [Unity Profiler](https://docs.unity3d.com/Manual/Profiler.html) are available through the menu on the top-right of the **Graph Debug Information** panel. 
## Particle System Info

 The **Particle System Info** panel is attached to the Initialize Context of each system. This panel provides information relative a specific system. 

| Property              | Description                                                                                                                                                                                                                                                                          |
|-----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Playing** or **Paused**        | Indicates the play state of the system.                                                                                                                                                                                       |
| **Awake** or **Asleep**       | Indicates the sleep state of the system.                                                                                                                                                                           |
| **Visible** or **Culled**     | Indicates the culling state of the system. The culling state depends on the camera or the scene view, and is based on the bounds. You can set bounds can be set in the Initialize Context, or record and apply them via the control panel. |
| **Alive/Capacity**    | Indicates the number of particles alive and the capacity set by the user in initialize context.  Optimizing the capacity to fit the maximum number of particles alive saves memory allocation space.                                                                                 |
| **System CPU Update** | Indicates the update time of the system on CPU in milliseconds.                                                                                                                                                                                                                      |
| **GPU System Time**   | Total execution time of the system on GPU. It aggregates the execution times of all the contexts in the system.                                                                                                                                                                      |
| **GPU Memory**        | Particle Attributes Size : Memory used for storing the particles attributes for this system. This value scales with the capacity and the number of stored attributes in the system.                                                                                                  |

## Context Panels

Contexts debug panels are attached to each context of a system. They are displayed when a context is selected and can be locked to be kept on screen even when the context is unselected.

Each context will display information that are relevant to its use:

### Spawn Context

| Property          | Description                                                                                                                                                                          |
|-------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Spawner State** | Indicates if the last received event was on the Start port (Playing) or on the Stop port (Stopped). See [Enabling and disabling spawn contexts](Contexts.md#enabling-and-disabling). |

### Initialize, Update, and Output Contexts

|Property                           | Description                                                                                                                                                                                                                                                                          |
|-----------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Implicitly updated attributes** | (Update context only) Displays the attributes that are implicitly updated, such as Position, Rotation or Age. See [Update Settings](Contexts.md#update).                                                                                                                             |
| **Execution time (GPU)**          | Execution time of the context on GPU, in milliseconds. Opening the dropdown will show a breakdown of the different tasks performed by the context. Each entry corresponds to a compute dispatch or a draw call. The number of dispatch or draw call for each task is in parenthesis. |
| **Texture Usage**                 | Lists the textures used in the context, along with their dimension and memory size.                                                                                                                                                                                                  |


Note that GPU execution timings are not available on Apple Silicon. 

When using the Visual Effect profiling and debugging tools, take the results as general indicators of performance. Actual performance may vary based on the target devices. You should still profile your scenes on your target devices, using the Unity Profiler.

While profiling panels are enabled, [Instancing](Instancing.md) is disabled for the attached Visual Effect. 
