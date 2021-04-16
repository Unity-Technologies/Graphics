# Point Caches in the Visual Effect Graph

A Point Cache is an asset that stores a number of points and their attributes baked into textures. You can use Point Caches to create particle effects in the shape of complex geometry.

## Point Cache assets

Unity imports and stores Point Caches as an asset. Point Cache assets follow the open-source [Point Cache](https://github.com/peeweek/pcache/blob/master/README.md) specification and use the `.pCache` file extension. They have no public properties to edit in the Inspector, but they do display read-only information such as the number of particles and the textures that represent the particle properties. For more information about Point Cache assets and a description of the properties they display in the Inspector, see [Point Cache asset](point-cache-asset.md).

![](Images/PointCacheImporter.png)

## Using Point Caches

The [Point Cache Operator](Operator-PointCache.md) enables you to use Point Caches in visual effects. This operator extracts the number of particles and their attributes from the Point Cache asset and exposes them as output ports in the Operator. You can then connect the ports to other Nodes, such as the [Set \<attribute> from Map](Block-SetAttributeFromMap.md) Block.

![](Images/PointCacheOperator.png)

## Generating Point Caches

There are multiple ways to generate a Point Cache to use in a visual effect:

- The built-in [Point Cache Bake Tool](point-cache-bake-tool.md)
- The Houdini pCache Exporter bundled with [*VFXToolbox*](https://github.com/Unity-Technologies/VFXToolbox) (located in the /DCC~ folder) enables you to bake Point Caches.

- You can write your own exporter to write Point Cache files. For information on the Point Cache asset format and specification, see the [pCache README](https://github.com/peeweek/pcache/blob/master/README.md).

## Limitations and Caveats

Currently, only the `float` and `uchar` property types are supported by the Importer. Any property of other types returns an error.
