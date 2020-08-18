# Update

Menu Path : **Context > Update Particle**

The Update [Context](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/Contexts.html) processes [Initialized](Context-Initialize.md) particles or particle strips for a given System.


The Visual Effect Graph executes this Context every frame, according to the culling state of the effect in the Scene and the Culling Flags specified in the Visual Effect Graph Asset. Each Update Context executes the Blocks it contains and can process additional implicit behavior upon certain conditions. For information about implicit behaviors, see the [Details section](#details).

## Context settings

| **Setting**         | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **Space**           | Enum     | (**Inspector**) The simulation space for this system. The options are:<br/>&#8226; **Local**: The system simulates in local space.<br/>&#8226; **World**: The system simulates in world space. |
| **Update Position** | Bool     | (**Inspector**) Specifies whether or not to update particle positions based on their velocity. |
| **Update Rotation** | Bool     | (**Inspector**) Specifies whether or not to apply implicit angular velocity to the particle rotation. |
| **Age Particles**   | Bool     | (**Inspector**) Specifies whether or not to age particles automatically. |
| **Reap Particles**  | Bool     | (**Inspector**) Specifies whether or not to reap particles automatically. When enabled, if a particle's age exceeds its' lifetime, this Context removes it from simulation. |

## Flow

| **Port**   | **Description**                                              |
| ---------- | ------------------------------------------------------------ |
| **Input**  | Connection from an [Initialize](Context-Initialize.md) Context. |
| **Output** | Connection to an Update (Single) or Output (Single/Multiple) Context. |

## Details

### Implicit Behaviors

Depending on the attributes present in the system, this Context performs extra behaviors implicitly: 

- **Velocity Integration**: If the Velocity attribute is in the system, this Context performs Euler velocity integration using the equation: `position += velocity * deltaTime`. This moves particles according to their velocity. Prior to Velocity integration, this Context backs up the position attribute into the oldPosition attribute.

- **Angular Velocity Integration**: If the AngularVelocity attribute is in the system, this Context performs Euler angle velocity integration using the equation: `angle.xyz += angularVelocity.xyz * deltaTime`. This rotates particles according to their angular velocity.

- **Aging**: If the Age attribute is in the system, this Context performs automatic aging of the particles following the equation: `age += deltaTime`

- **Reaping**: If bot Age and Lifetime attributes are in the system, this Context kills a particle (sets its alive attribute to false) if the particle's lifetime exceeds its age using the equation:`alive = (age <= lifetime)`

All implicit behaviors are enabled by default and can be disabled in the context’s inspector.

All implicit behaviors happen after the execution of all the Update Context's Blocks.

### Update Timing

The Visual Effect Graph executes this Context every frame depending on the [Update Mode](<https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/index.html?subfolder=/manual/VisualEffectGraphAsset.html%23creating-visual-effect-graph-assets) set on the Visual Effect Graph Asset:

- In **Delta Time** mode, the update uses the frame’s delta time and happens once every frame. In this mode, delta time is variable and a change in frame rate impacts the simulation significantly.

- In **Fixed Delta Time** mode, the update uses either a fixed delta time value, or a zero delta time value if the frame does not need to update yet. In this mode, the simulation occurs at a fixed rate which impacts the simulation quality less. However, in this mode, deltaTime can sometimes equal zero, and consequently oldPosition can equal a position in the frame following a zero-deltaTime update.