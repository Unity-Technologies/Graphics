# Blocks

Blocks are Nodes that define the behavior of a [Context](Contexts.md). You can create and reorder Blocks within a Context and, when Unity plays a visual effect, Blocks execute from top to bottom.

You can use Blocks for many purposes, from simple value storage (for example, a random Color) to high-level complex operations such as Noise Turbulence, Forces, or Collisions.

## Adding Blocks

To add a Block to a Context, either:

* Right click the Context and select **Create Block** from the context menu.
* With the cursor above a Context, press the spacebar.

**Note**: Unity places the Block that you create at the closest position to the cursor. Use this behavior to place Blocks in the correct position.

## Manipulating Blocks

Blocks are essentially Nodes that have a different workflow logic. Blocks are always stacked within a container, called a [Context](Contexts.md) and their workflow logic connects vertically without visible links.

* To move a Block, click the Block's header and drag it to another compatible Context.

* To reorder a Block, click the Block's header and drag it to a different position in the same Context.

- You can perform various actions on Blocks, such as cutting, copying, pasting, and duplicating. To do this:

  - Right-click on the Block and select the command from the context menu.
  
  - Select the Block and use the following keyboard shortcuts:

    - **On Windows:**
      - **Copy:** Ctrl+C.
      - **Cut:** Ctrl+X.
      - **Paste:** Ctrl+V.
      - **Duplicate:** Ctrl+D.

    - **On macOS:**
      - **Copy:** Cmd+C.
      - **Cut:** Cmd+X.
      - **Paste:** Cmd+V.
      - **Duplicate:** Cmd+D.

* To disable a Block, disable the checkbox to the left of the Block's header. This requires you to recompile the graph.

## Configuring Blocks

To change the way that the Block looks and behaves, adjust the Block's [Settings](GraphLogicAndPhilosophy.md#settings) in the Node UI or the Inspector.

For example, if, in the Inspector, you change the Composition Settings of a **Set Velocity** Block from **Overwrite** to **Blend**, this changes the title of the Node to **Blend Velocity** and adds a **Blend** property to the Node UI.

## Activation port

In addition to its [property](Properties.md) ports, a Block has a special port called the activation port. It is located on the top left of a Block, next to its name.

The activation port is linked to a boolean property and allows the user to control if a Block is active.

You can use the toggle next to the port to manually activate or deactivate a Block.

You can connect a graph logic to the activation port to accurately control under which conditions a Block is active per invocation. This allows you to implement different behaviors or states per particle within the same system.

Unity is able to determine if a Block is statically inactive. An inactive Block appears greyed out, and Unity removes it during compilation so it has zero runtime cost.

**Note**: Subgraph Blocks don't have activation ports. To emulate an activation port, you can expose a boolean exposed property from the subgraph, and connect the property to the activation ports of the subgraph's internal Blocks.
