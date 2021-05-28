# Contexts

Contexts are the main element of the Visual Effect Graph's **processing** (vertical) workflow and determine how particles spawn and simulate. The way you organize Contexts on the graph defines order of operation for the processing workflow. For information on the processing workflow, see [Visual Effect Graph Logic](GraphLogicAndPhilosophy.md). Every Context defines one stage of computation. For example a Context can:

* Calculate how many particles the effect spawns.
* Create new particles.
* Update all living particles.

Contexts connect to one another sequentially to define the lifecycle of particles. After a graph creates new particles, the **Initialize** Context can connect to an **Update Particle** Context to simulate each particle. Also, the **Initialize** Context can instead connect directly to an **Output Particle** Context to render the particles without simulating any behavior.

## Creating and connecting Contexts

A Context is a type of [graph element](GraphLogicAndPhilosophy.md#graph-elements) so to create one, see [Adding graph elements](VisualEffectGraphWindow.md#adding-graph-elements).

Contexts connect to one another in a vertical, linear order. To achieve this, they use [flow slots](). Depending on which part of the particle lifecycle a Context defines, it may have flow slots on its top, its bottom, or both.

## Configuring Contexts

To change the behavior of the Context, adjust its [settings](GraphLogicAndPhilosophy.md#settings) in the Node UI or the Inspector.

Some settings also change how the Context looks. For example in a **Quad Output** Context, if you set the UV Mode to **FlipbookMotionBlend**, Unity adds the following extra properties to the Context header: **Flipbook Size**, **Motion Vector Map**, and **Motion Vector Scale**.

## Flow compatibility

Not all Contexts can connect to one another. To keep a consistent workflow, the following rules apply:

* Contexts only connect to compatible input/output data types.
* [Events](Events.md) can connect to one or many Events or **Initialize** Contexts.
* **Initialize** Contexts can have one or many **SpawnEvent** sources or one or many **GPUSpawnEvent** sources, but these data type are mutually exclusive.
* Only one **Initialize** Context can connect to one **Update** Context.
* You can connect an **Output** Context to an **Initialize** or **Update** Context.

For a breakdown of context compatibility, see the table below.

| Context            | Input Data Type                      | Output Data Type | Specific Comments                                            |
| ---------------------- | --------------------------------------------- | ------------------- | ------------------------------------------------------------ |
| **Event**              | **None**                                      | **SpawnEvent** (1+) | **None**                                                     |
| **Spawn**              | **SpawnEvent** (1+)                           | **SpawnEvent** (1+) | Has two input flow slots which start and stop the **Spawn** context respectively. |
| **GPU Event**          | **None**                                      | **SpawnEvent**      | Outputs to **Initialize** Context                            |
| **Output Event** | **SpawnEvent (1+)** |  | Outputs a CPU SpawnEvent back to the Visual Effect component. |
| **Initialize**         | **SpawnEvent** (1+) or **GPUSpawnEvent** (1+) | **Particle** (1)    | Input types are either **SpawnEvent** or **GPUSpawnEvent**. These input types are mutually exclusive.<br/>Can output to **Particle Update** or **Particle Output**. |
| **Update**             | **Particle** (1)                              | **Particle** (1+)   | Can output to a **Particle Update** or **Particle Output**.  |
| **Particle Output**    | **Particle** (1)                              | **None**            | Can either have input from an **Initialize** or **Update** Context.<br/>No output. |
| **Static Mesh Output** | **None**                                      | **None**            | Standalone Context.                                          |

# Context type overview

This section covers all the common settings for every kind of Context.

## Event

Event Contexts only display their name, which is a string. To trigger an Event Context and activate a workflow from it, use the Event Context's name in the [component API](ComponentApi.md). For information on how to do this, see [Sending Events](ComponentApi.md#sending-events).

## Spawn

Spawn Contexts are standalone systems that have three States: Running, Idle, and Waiting.

* **Looping** (Running): This state means that Unity computes the Blocks in the Context and spawns new particles.
* **Finished** (Idle): This state means that the spawn machine is off and does not compute Blocks in the Context or spawn particles.
* **DelayingBeforeLoop/DelayingAfterLoop** (Waiting): This state pauses the Context for the duration of a delay time which you can specify. After the delay, the Context resumes, computes Blocks in the Context, and spawns particles.

To customize **Spawn** Contexts, you can add compatible **Blocks** to them. For information on the Spawn Context API, see the [Script Reference](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXSpawnerLoopState.html).

### Enabling and disabling

Spawn Contexts expose two [flow slots](GraphLogicAndPhilosophy.md#processing-workflow-vertical-logic): **Start** and **Stop**:

- The **Start** input resets/starts the Spawn Context. If you do not connect anything to this flow slot, it implicitly uses the **OnPlay** [Event](Events.md). Using **Start** many times has the same effect as using it once.
- The **Stop** input stops the Spawn System. If you do not connect anything to this flow slot, it implicitly uses the **OnStop** [Event](Events.md).

### Looping and delaying

Each Spawn Context contains a state to determine when the Context spawns particles.

* The Spawn Context emits particles during loops of a particular duration. This means the internal spawn time resets when each loop starts. By default, the duration is **infinite**, but you can change this.<br/>To set the loop mode:
  1. Select the Spawn Context in the graph.
  2. In the Inspector, click the **Loop Duration** drop-down.
  3. From the list, click either **Infinite**, **Constant**, or **Random**.
* Spawn Contexts can perform one, many, or an infinite number of loops.<br/>To set the number of loops:
  1. Select the Spawn Context in the graph.
  2. In the Inspector, click the **Loop** drop-down.
  3. From the list, click either **Infinite**, **Constant**, or **Random**.
* Spawn Contexts can perform a delay before and after each loop. During a delay, the spawn time elapses normally but the Spawn Context does not spawn any particles.<br/>To set the delay duration:
  1. Select the Spawn Context in the graph.
  2. In the Inspector, click either the **Delay Before Loop** or **Delay After Loop** drop-down.
  3. From the list, click either **None**, **Constant**, or **Random**.

If you set **Loop Duration**, **Loop**, **Delay Before Loop**, or **Delay After Loop** to either **Constant** or **Random**, the Spawn Context displays extra properties in its header to control each behavior. To evaluates the values you set, Unity uses the following rules:

- If set, Unity evaluates **Loop Count** when the **Start** flow input of the Context triggers.
- If set, Unity evaluates **Loop Duration** every time a loop starts.
- If set, Unity evaluates **Loop Before/After Delay** every time a delay starts.

For a visualization of the looping and delay system, see the following illustration:

![Figure explaining the Loop/Delay System](Images/LoopDelaySystem.png)

## GPU Event

GPU Event Contexts are experimental Contexts that connect inputs to output GPU Events from other systems. They differ from the normal Event Contexts in two ways:

* The GPU computes GPU Events and the CPU computes normal Events.
* You can't customize GPU Event Contexts with Blocks.

**Note**: When you connect Spawn Events to an Initialize Context, be aware that GPU Spawn Events and normal Spawn Events are mutually Exclusive. You can only connect one type of Spawn Event to an **Initialize** Context at the same time.

## Initialize

Initialize Contexts generate new particles based on **SpawnEvent** Data, which Unity computes from Events, Spawn Contexts, or GPU Event Contexts.

For example: If a Spawn Context states that the effect should create 200 new particles, the Initialize Context processes its Blocks for all 200 new particles.

To customize **Initialize **Contexts, you can add compatible **Blocks** to them.

Initialize contexts are the entry point of new systems. As such, they display the following information and configuration details in their header:

| Property/Setting   | Description                                 |
| ---------------------- | -------------------------------------------- |
| **Bounds** (Property)  | Controls the Bounding box of the System.     |
| **Capacity** (Setting) | Controls the allocation count of the System. |

## Update

Update Contexts update all living particles in the system based on **Particle** Data, which Unity computes from Initialize and Update Contexts. Unity executes Update Contexts, and thus updates every particle, every frame.

Particle Update Contexts also automatically process some computations for particles in order to simplify common editing tasks.

To customize **Update** Contexts, you can add compatible **Blocks** to them.


| Setting             | Description                                                  |
| ----------------------- | ------------------------------------------------------------ |
| **Update Position** | Specifies whether Unity applies velocity integration to the particles. When enabled, Unity applies simple Euler velocity integration to each particle's position every frame. When disabled, Unity does not apply any velocity integration. |
| **Update Rotation** | Specifies whether Unity applies angular integration to the particles. When enabled, Unity applies simple Euler integration to each particle's rotation every frame. When disabled, Unity does not apply any angular integration. |
| **Age Particles**       | If the Context uses the Age attribute, this controls whether the Update Context makes particles age over time. |
| **Reap Particles**      | If the Context uses the Age and Lifetime attributes, this control whether the Update Context removes a particles if the particle's age is greater than its lifetime. |


## Output

Output Contexts render the particles in a system. They render the particles with different modes and settings depending on the particle Data from the **Initialize** and **Update** Contexts in the same system. It then renders the configuration as a particular primitive shape.

To customize **Output** Contexts, you can add compatible **Blocks** to them.
