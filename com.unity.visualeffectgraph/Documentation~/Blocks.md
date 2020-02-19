# Blocks

Blocks are Nodes that define the behavior of a [Context](Contexts.md). You can create and reorder Blocks within a Context and, when Unity plays a visual effect, Blocks execute from top to bottom.

![](Images/BlockUI.png)

You can use Blocks for many purposes, from simple value storage (for example, a random Color) to high-level complex operations such as Noise Turbulence, Forces, or Collisions.

## Adding Blocks

To add a Block to a Context, either:

* Right click the Context and select **Create Block** from the context menu.
* With the cursor above a Context, press the spacebar.

**Note**: Unity places the Block that you create at the closest position to the cursor. Use this behavior to place Blocks in the correct position.

## Manipulating Blocks

Blocks are essentially Nodes that have a different workflow logic. Blocks are always stacked within a container, called a [Context](Contexts.md) and their workflow logic connects vertically without visible links.

* To move a Block, click the Block's header and drag it to another compatible Context.
  
* To reorder a Block, click click the Block's header and drag it to a different position in the same Context.
  
* You can also cut, copy, paste, and duplicate Blocks. To do this:
  * Right click on the Bode and select the command from the context menu.
  * Select the Block and use the following Keyboard shortcuts:
	    * On Windows
          * **Copy**: Ctrl+C.
          * **Cut**: Ctrl+X.
          * **Paste**: Ctrl+V.
          * **Duplicate**: Ctrl+D.
	    * On OSX
          * **Copy**: Cmd+C.
          * **Cut**: Cmd+X.
          * **Paste**: Cmd+V.
          * **Duplicate**: Cmd+D.
  
* To disable a Block, disable the checkbox to the right of the Block's header. This requires you to recompile the graph.

## Configuring Blocks

To change the way that the Block looks and behaves, adjust the Block's [Settings](GraphLogicAndPhilosophy.md#settings) in the Node UI or the Inspector. 

For example, if, in the Inspector, you change the Composition Settings of a **Set Velocity** Block from **Overwrite** to **Blend**, this changes the title of the Node to **Blend Velocity** and adds a **Blend** property to the Node UI.

