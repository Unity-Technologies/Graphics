<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Draft:</b> The content on this page is complete, but it has not been reviewed yet.</div>


<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This Feature is currently experimental and is subject to change in later major versions.</div>
# Point Caches

Point Caches are a special Asset type that bundles a **Point Count**, and **One or Many AttributeMaps**, that contain every attribute value list, baked into textures.

These point caches can be used in the graph using the **Point Cache** Operator, to access all these values, and so they can be read in **Attribute from Map** Blocks for instance.

## Point Cache Assets

Point cache Assets follow the Open Source [Point Cache](https://github.com/peeweek/pcache/blob/master/README.md) Specification and are imported as bundled Scriptable Objects. Every `.pCache` extension file will import into a bundled Scriptable Object so it can be used with the Point Cache Operator.

![](Images/PointCacheImporter.png)

## Point Cache Operator

Point cache Assets can be referenced in a Point Cache Operator so it displays its point count and the list of Attribute Maps contained in the Point Cache Asset. The Number and the Name of the Outputs will dynamically change depending on the Asset set in the settings field.

![](Images/PointCacheOperator.png)

## Generating Point Caches

You can generate point cache using various methods:

* Using the Houdini pCache Exporter bundled with [VFXToolbox](https://github.com/Unity-Technologies/VFXToolbox) (located in the /DCC~ folder)
* Using the Built-in [Point Cache Bake Tool](PointCacheBakeTool.md)
* By writing your own exporter to write [Point Cache](https://github.com/peeweek/pcache/blob/master/README.md) files that follow the specification.
