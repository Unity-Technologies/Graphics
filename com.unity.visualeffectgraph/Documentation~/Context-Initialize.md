# Initialize

Menu Path : **Context > Initialize Particle**

The **Initialize** Context processes a [Spawn Event](Context-Spawn.md) or a [GPU Event](Context-GPUEvent) and initializes new elements for a Particle or ParticleStrip simulation.

## Context settings

| **Setting**                  | **Type** | **Description**                                              |
| ---------------------------- | -------- | ------------------------------------------------------------ |
| **Space**                    | Enum     | **(Inspector)** The [simulation space](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/Systems.html%23system-simulation-spaces) for the system. |
| **Data Type**                | Enum     | **(Inspector)** The data type for the elements in the system. The options are:<br/>&#8226; **Particle**: The system spawns particles.<br/>&#8226; **Particle Strip**: The system spawns particle strips. |
| **Capacity**                 | UInt     | The fixed amount of elements in the simulation. This count scales the memory allocation of the particle system. |
| **Particle Per Strip Count** | Uint     | The fixed amount of particles per particle strip.<br/>This setting only appears if you set **Data Type** to **Particle Strip**. |

## Context input properties

| **Property** | **Type**               | **Description**                                              |
| ------------ | ---------------------- | ------------------------------------------------------------ |
| **Bounds**   | [AABox](Type-AABox.md) | The bounding box defined for the system. This property is evaluated accordingly to the **Culling Flags** property defined in the [Visual Effect Asset](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/VisualEffectGraphAsset.html). |

## Flow

| **Port**   | **Description**                                              |
| ---------- | ------------------------------------------------------------ |
| **Input**  | Connection from a [Spawn](Context-Spawn.md), [GPU Event](Context-GPUEvent.md), or [Event](Context-Event.md) Context. For more information on input flow compatibility, see [Input flow compatibility](#input-flow-compatibility). |
| **Output** | Connection to an [Update](Context-Update.md) (Single) or Output (Single/Multiple) Context. |

## Details

### Overspawn

To create new elements, you can add [Blocks](Blocks.md) to the Context's body. The Visual Effect Graph adds these Blocks to the simulation if there is memory left to create them. After execution, Unity discards all elements that can not be injected this way.

#### The Alive attribute

Setting the [Alive attribute](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/Reference-Attributes.html%23attribute-usage-and-implicit-behavior) to false in the Initialize context creates a dead particle.

While doing this allows you to discard particles at their birth, overspawn still applies. The particle is considered dead only in the next update call. This means that you cannot create more particles (alive or dead) than the remaining count allows.

### Call order

The Visual Effect Graph executes the Initialize Context only once per new element, prior to its first Update. At the frame of execution, the Visual Effect Graph initializes the new element, executes the element's first update, and finally renders the element.

### Source attribute availability

In an Initialize Context, Blocks and Operators can read from [source attributes](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/Attributes.html%23source), by using a Get Attribute (Source) Operator, or an Inherit \<Attribute\> Block.

### Input flow compatibility

Initialize Contexts can connect from one or many SpawnEvent outputting contexts with the following rules:

- Initialize Contexts can connect from any number of Spawn and/or Event Contexts.
- Initialize Contexts can connect from a single GPU Event Context.
- You can not mix GPU and CPU Event/Spawn Contexts to the input port. If you connect both GPU Event and Spawn Contexts, the Console shows the following error: `Exception while compiling expression graph: System.InvalidOperationException: Cannot mix GPU & CPU spawners in init`
