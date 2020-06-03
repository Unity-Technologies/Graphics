# Block Node

## Description

A Block is a specific type of Node for the Master Stack. A block represents a single piece of the surface (or vertex) description data used in the final shader output. Some Block nodes are always available, while some may only be available with certain pipelines.  

Certain blocks are only compatible with specific Graph Settings. Blocks may become Active or Inactive based on the graph settings. 

Blocks cannot be cut, copied, or pasted. 

## Adding and Removing Block Nodes

New Block nodes can be added to a Context in the Master Stack by hovering over empty space in the Context and pressing space bar or right clicking and selecting “Create Node”. This will bring up the Create Node menu. 

This menu will contain only Block nodes valid for that context. Vertex blocks will not appear in the Create Node menu of the Fragment context. 

Selecting a block node from this menu will add it to the context. 

Selecting a block node in the context and pressing “Delete” or right clicking and selecting “Delete” will remove the block from the context. 

### Automatically Add or Remove Blocks

Blocks can also be added or removed from a context automatically based on the user’s Shader Graph Preferences. If Automatically Add or Remove Blocks is enabled, the required Block nodes for a certain Target or Material type will be added automatically. Any incompatible block nodes that have no connections and default values will be removed from the context automatically. 

If Automatically Add or Remove Blocks is disabled, no block nodes will ever be automatically added or removed. All required Block nodes must be added manually by the user based on the selected target and material settings. 

## Active and Inactive Blocks

Active block nodes are blocks that are being generated and contributing to the final shader. 
Inactive block nodes are blocks that are present in the shader graph, but are not being generated or contributing to the final shader. 

![image](images/Active-Inactive-Blocks.png)

Certain configurations of the graph settings may cause blocks to become active or inactive. This state will be displayed by greying-out the block nodes that are inactive, as well as any node stream that is _only_ connected to the inactive block node. 
