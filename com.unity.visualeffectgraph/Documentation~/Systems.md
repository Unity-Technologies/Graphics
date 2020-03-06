# Systems

A System refers to one or many [Contexts](Contexts.md) that define a standalone part of a visual effect. A System can be a Particle System, a Particle Strip System, a Mesh, or a Spawn machine. In the graph view, a System draws a dashed line box around the Contexts that it consists of.

![](Images/SystemDrawBox.png)

Multiple Systems can interact with each other within a Visual Effect Graph:

* A **Spawn** System can **spawn particles** in one or many Particle Systems. This is the main method for spawning particles.

* **Particle Systems** can use GPU Events to **spawn particles** in **other particle systems**. This alternate method can spawn particles from other particles based on simulation events such as the death of particle.

* A **Spawn** System can **enable and disable** other **Spawn Systems**. This allows you to use a master Spawn System that manages other Spawn Systems to synchronize particle emission.


## Creating System from templates

The Visual Effect Graph comes with pre-built System templates that you can add to your graph. To create a System from a template:

1.  Right Click in an empty space of your workspace and select **Create Node**.
2.  In The menu, select **System**.
3.  Select a template from the list.

![](Images/SystemAddTemplate.png)

## System simulation spaces

Some Systems use a simulation space property to define the reference space that it uses to simulate its contents:

* **Local space** Systems simulate the effect locally to the GameObject that holds the [Visual Effect component](VisualEffectComponent.md).
* **World space** Systems simulate the effect independently of the GameObject that holds the [Visual Effect component](VisualEffectComponent.md).

Regardless of the System's simulation space, you can use [Spaceable Properties](Properties.md#spaceable-properties) to access Local or World Values.

### Setting a System simulation space

A System displays its simulation space in the top-right corner of each Context it consists of. This is the System's **simulation space identifier**. If a Context does not use process anything that depends on simulation space, it does not display the simulation space identifier.

![](Images/SystemSpaceIdentifier.png)

To change the simulation space for a System, click the System's simulation space identifier to cycle through the compatible spaces.

![](Images/SystemSpaceLocalWorld.png)

### Simulation space identifiers in Properties

Some [Spaceable Properties](Properties.md) display a smaller version of the simulation space identifier. This does not change the System's simulation space, but instead allows you to express a value in a space that is different from the System's simulation space. For example, a System could simulate in world space but a Property could be a local position.

![](Images/SystemSpaceLocalWorldSmall.png)