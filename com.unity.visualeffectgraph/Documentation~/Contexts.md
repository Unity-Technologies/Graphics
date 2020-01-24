<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Draft:</b> The content on this page is complete, but it has not been reviewed yet.</div>
# Contexts

Contexts are the main elements of the Graph Workflow logic (vertical) and define the succession and the relationships of operations and simulations. Every context defines one stage of computing, for example computing how many particles need to be spawned, creating new particles or updating all living particles. 

Context connect to each other when there is meaning : After creating new particles, an Initialize context can connect to a Update Particle context, or directly to a Output Particle Context to render the particles without simulating them.

## Creating and Connecting Contexts

Contexts are Graph elements, so they can be created using the Right Click > Add Node Menu, Spacebar Menu or by making a workflow (vertical) connection from another context (providing only compatible contexts)

Contexts connect to each other using the Ports at the top and the bottom.

## Configuring Contexts

Adjusting Context [Settings](GraphLogicAndPhilosophy.md#settings) in the Node UI or the Inspector can change the way the Operator looks and behaves. 

> For instance, Changing the UV Mode of a `Quad Output` Context, from *Simple* to *FlipbookMotionBlend* will add Extra *Flipbook Size*, *Motion Vector Map* and *Motion Vector Scale* Properties to the Context Header.

## Flow Compatibility

Not all contexts can be connected altogether, in any order. Some rules apply to keep a consistent workflow:

* Contexts connect by compatible input/output data type.
* Events can connect to one or many events / initialize contexts.
* Initialize contexts can have one or many SpawnEvent source or one or many GPUSpawnEvent source, but these data type are mutually exclusive.
* Only One Initialize can be connected to one Update Context
* You can connect any Output Contexts to a Initialize / Update context.

 Here is a recap table of the context compatibility:

| Context            | Input Data Type                      | Output Data Type | Specific Comments                                            |
| ------------------ | ------------------------------------ | ---------------- | ------------------------------------------------------------ |
| Event              | None                                 | SpawnEvent (1+)  |                                                              |
| Spawn              | SpawnEvent (1+)                      | SpawnEvent (1+)  | Two input pins, start and stop the spawn context             |
| GPU Event          | None                                 | SpawnEvent       | Outputs to Initialize Context                                |
| Initialize         | SpawnEvent (1+) / GPUSpawnEvent (1+) | Particle (1)     | Can output to Particle Update or Particle Output. Input types SpawnEvent/GPUSpawnEvent are mutually exclusive. |
| Update             | Particle (1)                         | Particle (1+)    | Can output to a Particle Update or Particle Output           |
| Particle Output    | Particle (1)                         | None             | Can either have input from an Initialize or Update           |
| Static Mesh Output | None                                 | None             | Standalone Context                                           |

# Context Type Overview

This section covers all the common settings of every kind of context. For more details about specific contexts, see [Context Library]()

## Event

Event Contexts only display a Name as a string that need to be called on the Component API in order to Send this event to the graph and activate a workflow from this Node.

## Spawn

Spawn Contexts are standalone systems that have three States : Playing, Stopped and Delayed. 

* **Looping** (Running) state means that the Blocks are computed and will perform spawn of new particles
* **Finished** (Idle) state means that the spawn machine is off and will not spawn particles
* **DelayingBeforeLoop/DelayingAfterLoop** (Waiting) state stops spawning particles until the end of a user-set delay, then restarts spawning particles.

Spawn contexts can be customized using compatible **Blocks**.

You can find Spawn Context API Reference [here](https://docs.unity3d.com/2019.3/Documentation/ScriptReference/VFX.VFXSpawnerLoopState.html).

### Turning On and Off 

Spawn Contexts expose two [Flow Input Slots](GraphLogicAndPhilosophy.md#processing-workflow-vertical-logic): Start and Stop:

- Start input **Resets** and/or **Start** the Spawn System : if not connected, it is implicitly bound to the `OnPlay` [Event](Events.md) . Hitting Start many times has the same effect as pushing it once.
- Stop input **Stops** the Spawn System : if not connected, it is implicitly bound to the `OnStop` [Event](Events.md) 

### Looping and Delaying

Spawn contexts contains a state and will perform spawning particles based on a looping system.

* The spawn context can emit during **loops of defined duration** (meaning the internal spawn time will reset at each loop's beginning) . By default the duration is **infinite**.
  * In order to set the loop mode, select the context in the graph and change the loop duration popup in the Inspector. (Possible Values : Infinite, Constant, Random)
* Spawn contexts can perform **one**, **many** or an **infinity** of **loops**. 
  * In order to set this setting, select the spawn context in the graph and change the Loop count popup in the Inspector (Possible Values : Infinite, Constant, Random)
* Spawn contexts can perform a **delay** **before** and/or a**delay after** each loop. During a delay, the spawn time elapses normally but no spawn is performed.
  * In order to set these setting, select the spawn context in the graph and change the Delay Before Loop and Delay After Loop popups in the Inspector (Possible Values: None, Constant, Random)

Here is a visual illustration of the Looping and Delay System.

![Figure explaining the Loop/Delay System](Images/LoopDelaySystem.png)

Setting a loop count, loop duration and / or delays will display new connectable properties on the context's header. Evaluation of these values will follow these rules:

* If set : **Loop Count** is evaluated when the Start workflow input of the context is hit.
* If set : **Loop Duration** is evaluated every time a loop starts
* If set : **Loop Delay** (Before/After) is evaluated every time a delay starts.

## GPU Event

GPU Event contexts are experimental contexts that connect inputs to output GPU Events from other systems. They differ from Traditional Spawn as they are computed by the GPU.  Only one kind of Spawn can be connected to an Initialize Context (GPU Event and Spawn/Events are mutually Exclusive) 

> GPU Event contexts cannot be customized with Blocks.
>

## Initialize

Initialize Contexts will generate new particles based on **SpawnEvent** Data, computed from Events, Spawn or GPU Event contexts.

> For example: upon receiving an order of creation of 200 new particles from a spawn context, the context will be processed and will result in executing the context's Blocks for all 200 new particles.

Initialize contexts can be customized using compatible **Blocks**.

Initialize contexts are the entry point of new systems. As such, they display information and configuration in their header:

| Property/Setting   | Description                                 |
| ------------------ | ------------------------------------------- |
| Bounds (Property)  | Controls the Bounding box of the System     |
| Capacity (Setting) | Controls the allocation count of the System |



## Update

Update contexts update all living particles based on **Particle** Data computed from Initialize and Update Contexts. These contexts are executed every frame and will update every particle.

Particle Update Contexts also process automatically some computations for particles in order to simplify common editing tasks.

Update contexts can be customized using compatible **Blocks**.


| Setting             | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| Integration         | None : No velocity Integration <br/>Euler : Applies simple Euler velocity integration to the particles positions every frame. |
| Angular Integration | None : No velocity Integration <br/>Euler : Applies simple Euler angular velocity integration to the particles angles every frame. |
| Age Particles       | If Age attribute is used, Controls whether update will make particles age over time |
| Reap Particles      | If Age and Lifetime attributes are used, Control whether update will kill all particles which age is greater than its lifetime. |


## Output

Output Contexts renders a system with different modes and settings depending on Particle Data incoming from an **Initialize** or **Update** context. Every element will be rendered using a specific configuration as a specific primitive.

Output contexts can be customized using compatible **Blocks**.

For more information, and a comprehensive list of all output contexts and their settings, see [Output Contexts Reference]()