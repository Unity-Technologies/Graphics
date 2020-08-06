# Visual Effect Graph Logic

The Visual Effect Graph uses two distinct workflows:

* A **processing** (vertical) logic which links customizable stages together to define the lifetime of a system.

* A **property** (horizontal) logic which connects different [Contexts](Contexts.md) to define how the particles look and behave.

## Processing workflow (vertical logic)
The processing workflow links together a succession of customizable stages to define the complete system logic. This is where you can determine when the spawn, initialization, updating, and rendering of the particles happen during the effect.

The processing workflow connects Contexts using their **flow slots** located at the top and the bottom of the Context Node.

The processing logic defines the different stages of processing of a visual effect. Each stage consists of a large colored container called a [Contexts](Contexts.md). Each Context connects to another compatible Context, which defines how the next stage of processing uses the current Context.

Contexts can contain elements called [Blocks](Blocks.md). Every Block is a stackable Node that is in charge of one operation. You can reorder Blocks to change the order in which Unity processes a visual effect. Unity executes the Blocks in a Context from top to bottom.
## Property workflow (horizontal logic)
In the horizontal property workflow, you can define mathematical operations to enhance the visual effect. This affects how the particles look and behave.

The property workflow connects Contexts using the **Property Slots** of their Blocks. The left side is the input and the right side is the output.

The Visual Effect Graph comes with a large Block and Node library that you can to define the behavior of your visual effect. The network of Nodes that you create controls the horizontal flow of data that the render pipeline passes to the Blocks within your graph's Contexts.

To customize how particles behave, you can connect horizontal Nodes to a Block to create a custom a mathematical expression. To do this, use the **Create Node** context menu to add Nodes, change their values, then connect the Nodes to Block properties.

## Graph elements

A Visual Effect Graph provides a workspace where you can create graph elements and connect them together to define effect behaviors. The Visual Effect Graph comes with many different types of graph elements that fit into the workspace.

### Workspace

A Visual Effect Graph provides a **Workspace** where you can create graph elements and connect them together to define effect behaviors.

![The vertical workflow contains Systems, which then contain Contexts, which then contain Blocks. Together, they determine when something happens during the “lifecycle” of the visual effect.](Images/SystemVisual.png)

### Systems

[Systems](Systems.md) are the main components of a Visual Effect. Every system defines one distinct part that the render pipeline simulates and renders alongside other systems. In the graph, systems that are defined by a succession of Contexts appear as dashed outlines (see the image above).

* A **Spawn System** consists of a single Spawn Context.
* A **Particle System** consists  of a succession of an Initialize, then Update, then Output context.
* A **Mesh Output System** consists of a single Mesh Output Context.

### Contexts
[Contexts](Contexts.md) are the parts of a System that define a stage of processing. Contexts connect together to define a system.

The four most common Contexts in a Visual Effect Graph are:

* **Spawn**. If active, Unity calls this every Frame, and computes the amount of particles to spawn.
* **Initialize**. Unity calls this at the “birth” of every particle, This defines the initial state of the particle.
* **Update**. Unity calls this every frame for all particles, and uses this to perform simulations, for example Forces and Collisions.
* **Output**. Unity calls this every frame for every particle. This determines the shape of a particle, and performs pre-render transformations.

**Note:** Some Contexts, for example the Output Mesh, do not connect to any other Contexts as they do not relate to other systems.

### Blocks
[Blocks](Blocks.md) are Nodes that you can stack into a Context. Every Block is in charge of one operation. For example, it can apply a force to the velocity, collide with a sphere, or set a random color.

When you create a Block, you can reorder it within it current Context, or move it to another compatible Context.

To customize a Block, you can:

* Adjust a property. To do this, connect a property Port to another Node with an Edge.


* Adjust the settings of a property. Settings are editable values without ports that you cannot connect to other Nodes.

### Operators
[Operators](Operators.md) are Nodes that compose the low-level operations of the **property workflow**. You can connect Nodes together to generate custom behaviors. Node networks connect to Ports that belong to Blocks or Contexts.

### Graph Common Elements

While the graph elements are different, their contents and behavior tend to be the same. Graph elements share the following features and layout items:

#### Settings

Settings are Fields that you cannot connect to using the property workflow. Every graph element displays settings:

* In the **Graph** : Between the Title and the property container in the Graph.
* In the **Inspector** : When you select a Node, the Inspector displays additional, advanced settings.

If you change the value of a setting, you need to recompile the Graph to see the effect.

#### Properties

[Properties](Properties.md) are Fields that you can edit and connect to using the property workflow. You can connect them to other properties contained in other graph elements.

## Other graph elements

### Groups

You can group Nodes together to organize your graphs. You can drag grouped Nodes around together and even give them a title to describe what the group does. To add a Group, select multiple Nodes, right-click, and select **Group Selection**.

### Sticky Notes

Sticky Notes are draggable comment elements you can add to leave explanations or reminders for co-workers or yourself.
