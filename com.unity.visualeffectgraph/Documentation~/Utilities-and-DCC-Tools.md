

# Utilities and DCC Tools

## Built-in Utilities

### VF Importer

The vector field importer imports `.vf` files into Unity as 3D Textures. It can store them as Floating point, Half Floating point or Byte data formats. You can then reuse these textures in graphs.

![](Images/vf-importer.png)

Reference of the VF File format is available here : https://github.com/peeweek/VectorFieldFile

### pCache Importer

The point cache importer imports a pCache (Point Cache) file as an asset that references one or many attributes cached in textures.

![](Images/pcache-importer.png)

You can use the Point Cache operator to reference these assets inside the graph and access all attributes as textures (For instance to be used in **Attribute from Map** blocks)

![](Images/pcache-operator.png)

Reference of the *pCache* format is available here : https://github.com/peeweek/pcache

### Parameter Binders

![](Images/parameter-binder.png)

Parameter binders are scripts that can perform automatic binding between a scene object or component and an Exposed parameter. They are written in C# and the library is pretty easy to extend. Uset the *VFXParameterBinder* component as a base then use the + button in the list to add parameter binders from the menu.

### Event Binders

Event Binders are similar to Parameter Binders but they are intended to send events to the effect component instead.

![](Images/event-binders.png)

Event binders are written in C# and can be extended easily.

### Timeline / Animation Tracks and Clips

* Timeline can animate parameters from an effect, and Animations can too.
* The Visual Effect Event track can send Events to a Visual Effect Component

![](Images/timeline-events.gif)

### Point Cache Bake Tool

The point cache bake tool window can help you generate point cache files from geometry or images.

![](Images/pcache-tool-mesh.png)

![](Images/pcache-tool-texture.png)

## DCC Tools

You can find external tools in the [VFX Toolbox GitHub Repository](https://github.com/Unity-Technologies/VFXToolbox)

### VF Exporter for Houdini

The VF Exporter for Houdini lets you export standard houdini volumes as VF files to be imported in unity. 

### pCache Exporter for Houdini

The pCache Exporter for Houdini lets you export attributes from points in your geometry as point caches.