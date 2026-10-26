# Master Stack

## Description

The Master Stack is the end point of a Shader Graph that defines the final surface appearance of a shader. Your Shader Graph should always contain only one Master Stack.

The content of the Master Stack might change depending on the [Graph Settings](Graph-Settings-Tab.md) you select. The Master Stack is made up of Contexts, which contain [Block nodes](Block-Node.md).

![The Master Stack display, showing the Vertex and Fragment contexts populated with Block nodes.](images/MasterStack_Populated.png)

## Contexts

The Master Stack contains two Contexts: Vertex and Fragment. These represent the two stages of a shader. Nodes that you connect to Blocks in the Vertex Context become part of the final shader's vertex function. Nodes that you connect to Blocks in the Fragment Context become part of the final shader's fragment (or pixel) function. If you connect any nodes to both Contexts, they are executed twice, once in the vertex function and then again in the fragment function. You can't cut, copy, or paste Contexts.

## Block Node

A Block is a specific type of node for the Master Stack. A Block represents a single piece of the surface (or vertex) description data that Shader Graph uses in the final shader output. [Built In Block nodes](Built-In-Blocks.md) are always available, but nodes that are specific to a certain render pipeline are only available for that pipeline.

Some blocks are only compatible with specific [Graph Settings](Graph-Settings-Tab.md), and might become active or inactive based on the graph settings you select. You can't cut, copy, or paste Blocks.

### Add and Remove Block Nodes

To add a new Block node to a Context in the Master Stack:

1. Place the cursor over an empty area in the Context.
2. Press the Spacebar, or right-click and select **Create Node**.

    This brings up the Create Node menu, which displays only Block nodes that are valid for the Context.

3. Select a Block node from the menu to add it to the Context.

To remove a Block from the Context:

1. Select the Block node in the Context.
2. Press the Delete key, or right-click and select **Delete**.

> [!NOTE]
> You can configure Shader Graph to automatically add and remove Block nodes. For more information, refer to [Shader Graph preferences](Shader-Graph-Preferences.md).

### Active and Inactive Blocks

Active Block nodes are Blocks that contribute to the final shader. Inactive Block nodes are Blocks that are present in the Shader Graph, but don't contribute to the final shader.

When you change the graph settings, certain Blocks might become active or inactive. Inactive Block nodes and any node streams that are connected only to Inactive Block nodes appear grayed out.
