# Systems

A System refers to one or many [Contexts](Contexts.md) that define a standalone part of a visual effect. A System can be a Particle System, a Particle Strip System, a Mesh, or a Spawn machine. In the graph view, a System draws a dashed line box around the Contexts that it consists of.

![A Visual Effect Graph featuring a System enclosed within a green dashed box containing three labeled boxes: Initialize Particle (top), Update Particle (middle), and Output Particle Quad (bottom), each labeled Local. Connections flow sequentially between them: Initialize Particle → Update Particle → Output Particle Quad. Additionally, the Initialize Particle box is connected to a Spawn box located outside the green dashed boundary.](Images/SystemDrawBox.png)

Multiple Systems can interact with each other within a Visual Effect Graph:

* A **Spawn** System can **spawn particles** in one or many Particle Systems. This is the main method for spawning particles.

* **Particle Systems** can use GPU Events to **spawn particles** in **other particle systems**. This alternate method can spawn particles from other particles based on simulation events such as the death of particle.

* A **Spawn** System can **enable and disable** other **Spawn Systems**. This allows you to use a master Spawn System that manages other Spawn Systems to synchronize particle emission.

## System simulation spaces

Some Systems use a simulation space property to define the reference space that it uses to simulate its contents:

* **Local space** Systems simulate the effect locally to the GameObject that holds the [Visual Effect component](VisualEffectComponent.md).
* **World space** Systems simulate the effect independently of the GameObject that holds the [Visual Effect component](VisualEffectComponent.md).

A VFX System runs in either World or Local space. You set this coordinate space in the Inspector of any context in the system. Changing the space on one context updates it for the whole system. VFX Graph interprets all attributes and operations in this space.

### Setting a System simulation space

A System displays its simulation space in the top-right corner of each Context it consists of. This is the System's **simulation space identifier**. The word "Local" in a rounded box stands for "Local space", and the word "World" in a rounded box stands for "World space". If a Context does not use process anything that depends on simulation space, it does not display the simulation space identifier.

To change the simulation space for a System, click the System's simulation space identifier to cycle through the compatible spaces. Because the System space is shared, changing the space on any of its Contexts updates the space for the entire System.

### Simulation space identifiers in Properties

You can use [Spaceable Properties](Properties.md#spaceable-properties) to select the coordinate space in which VFX Graph interprets the inputs. You can [set spaceable block inputs](Blocks.md) to **Local**, **World**, or **None**:

- If you select **Local** or **World**, the VFX Graph converts the value from its input space to the System’s space.
- If you select **None**, the value is used as‑is, relative to the System’s current space.
