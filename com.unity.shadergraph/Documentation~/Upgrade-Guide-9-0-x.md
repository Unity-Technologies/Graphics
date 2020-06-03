# Upgrading to version 9.0.x of Shader Graph

## Master Stack Graph Output

Shader Graph has removed the Master Nodes and introduced a more flexible [Master Stack](Master-Stack) solution for graph output definition in 9.0. All graphs created in previous versions of Shader Graph can be opened and upgraded automatically in 9.0. See below for expected behavior and manual upgrade suggestions.

### Automatic Upgrade from One Master Node to the Master Stack 
If your graph only has one Master Node present, all of the data from that Master Node is automatically upgraded to the [Master Stack](Master-Stack) output. The following behavior is expected: 
The correct [Target(s)](Graph-Target) is added to the Graph Settings tab of the [Graph Inspector](Internal-Inspector) and all settings from the Master Node cog menu to describe surface options are copied from the Master Node to the Target Settings. 
A [Block](Block-Node) node is added to the [Master Stack](Master-Stack) for each port that existed on the Master Node. Any nodes that were connected to the Master Node ports are now connected to the corresponding [Block](Block-Node) Node. 
Any values that were entered into the default value inputs of the ports on the Master Node are now copied to the corresponding [Block](Block-Node) Node. 
The final shader is identical in appearance after upgrading. 

### Automatic Upgrade from Multiple Master Nodes to the [Master Stack](Master-Stack)
If your graph has more than one Master Node present, all of the above applies to the currently selected Active Master Node. 

Any Master Nodes present on the graph that are inactive will be removed from the graph when upgrading to the [Master Stack](Master-Stack) format. This data may be lost. When upgrading a graph with multiple Master Nodes, it is recommended that you make notes of the Master Node ports, connected nodes, and any non-default settings in the cog wheel menu of the inactive Master Nodes. 

After upgrading, you can add any required [Block](Block-Node) nodes that may be missing and reconnect the nodes to the Master Stack. The cog wheel settings from the inactive Master Nodes will need to be manually entered into the corresponding target setting via the Graph Settings tab of the [Graph Inspector](Internal-Inspector). 

### Automatic Upgrade of Cross-Pipeline Master Nodes to the Master Stack 
If your graph has either the PBR or Unlit Master Nodes that are compatible with both the Universal and the High Definition Render Pipelines, they will automatically upgrade to the [Master Stack](Master-Stack) based on the pipeline currently available in the project. 

In Universal, all settings from the PBR and Unlit Master Nodes should be the same as the Universal Lit and Unlit targets. See above for the expected behavior. 

In High Definition, the settings from the PBR and Unlit Master Nodes are not the same as the High Definition Lit and Unlit targets. As such, there may be some unexpected behavior when upgrading the PBR or Unlit Master Nodes to Lit and Unlit [Master Stack](Master-Stack). The final shader may not be visually the same as before the upgrade. Please report any upgrade issues via the Bug Reporter, but keep in mind some upgrade paths may not have immediate automated solutions and will need manual adjustment.

### “View Generated Shader” has moved 
Previously, users could preview the generated shader by right clicking the Master Node and selecting “View Generated Shader” from the context menu. 
In 9.0, users can now find the “View Generated Shader” button on the Shader Graph Asset via the Unity Inspector. 

![image](images/GeneratedShaderButton.png)

## Settings in Graph Inspector 
Shader Graph has introduced an internal [Graph Inspector](Internal-Inspector) in 9.0. It is a floating window to display settings related to selected objects in the graph. 

### Graph Settings
Graph-wide settings are now available in the Graph Settings tab of the [Graph Inspector](Internal-Inspector). 
Notably, the Precision toggle previously located on the Shader Graph Toolbar is now accessible via the Graph Settings tab.
No data has been changed, and as such the previous Precision setting of the graph will stay the same. 

Any settings from the Master Node cog menu that describe surface options are now available on a per-target basis via the Graph Settings tab of the [Graph Inspector](Internal-Inspector). See above for more details on the automatic upgrade of this data. 

### Property Settings 
Property settings have been moved from the Blackboard foldouts to the [Graph Inspector](Internal-Inspector). Multiple properties from the Blackboard can be selected at once and edited via the [Graph Inspector](Internal-Inspector). 
No data has been changed, and as such all settings previously set on properties of the graph will stay the same. 

### Per-Node Settings 
All per-node settings that were previously managed by opening a cog wheel sub-menu are now accessible through the [Graph Inspector](Internal-Inspector). 
No data has been changed, and as such all settings previously set on nodes in the graph (for example: precision settings, Custom Function Node settings) will stay the same. 

Any settings that were present on the Master Node to define the surface options are now present in the Graph Settings tab of the [Graph Inspector](Internal-Inspector). See above for more details. 

## Custom Function Nodes and Shader Graph Preview 
Custom Function Nodes may require the use of keywords for the in-graph preview rendering to avoid errors in the preview shader compilation. 
If you have any Custom Function Nodes with custom Shader Graph Preview code using `#if SHADERGAPH_PREVIEW`, you need to upgrade it to use an `#ifdef` declaration, like so: 

```
#ifdef SHADERGAPH_PREVIEW
	Out = 1;
#else
	Out = MainLight;
#endif
```
