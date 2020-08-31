# Block Node

## Description

A Block is a specific type of node for the Master Stack. A Block represents a single piece of the surface (or vertex) description data that Shader Graph uses in the final shader output. [Built In Block nodes](Built-In-Blocks.md) are always available, but nodes that are specific to a certain render pipeline are only available for that pipeline. For example, Universal Block nodes are only available for the Universal Render Pipeline (URP), and High Definition Block nodes are only available for the High Definition Render Pipeline (HDRP).

Some blocks are only compatible with specific [Graph Settings](Graph-Settings-Menu.md), and might become active or inactive based on the graph settings you select. You can't cut, copy, or paste Blocks.

## Add and Remove Block Nodes

To add a new Block node to a Context in the Master Stack, place the cursor over an empty area in the Context, then press the Spacebar or right-click and select **Create Node**.

This brings up the Create Node menu, which displays only Block nodes that are valid for the Context. For example, Vertex Blocks don't appear in the Create Node menu of a Fragment Context. 

Select a Block node from the menu to add it to the Context. To remove a Block from the Context, select the Block node in the Context, then press the Delete key or right-click and select **Delete**.

### Automatically Add or Remove Blocks

You can also enable or disable an option in the Shader Graph Preferences to automatically add and remove Blocks from a Context. 

If you enable **Automatically Add or Remove Blocks**, Shader Graph automatically adds the required Block nodes for that particular asset's Target or material type. It automatically removes any incompatible Block nodes that have no connections and default values.

If you disable **Automatically Add or Remove Blocks**, Shader Graph doesn't automatically add and remove Block nodes. You must manually add and remove all Block nodes.

## Active and Inactive Blocks

Active Block nodes are Blocks that contribute to the final shader. Inactive Block nodes are Blocks that are present in the Shader Graph, but don't contribute to the final shader.

![image](images/Active-Inactive-Blocks.png)

When you change the graph settings, certain Blocks might become active or inactive. Inactive Block nodes and any node streams that are connected only to Inactive Block nodes appear grayed out.