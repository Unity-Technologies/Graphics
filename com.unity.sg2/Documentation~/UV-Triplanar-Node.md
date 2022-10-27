# UV Triplanar Node

The UV Triplanar node generates UV coordinates for projecting a texture in the XYZ, XZ, or Y directions with a single texture sample.  Unlike using the Triplanar node, these coordinates produce projections with seams because there’s no blending between the projections. However, some textures contain patterns that are complex enough that the resulting seems don’t show. In these cases, it’s much cheaper to project using this method than to use the Triplanar node since this method accomplishes projection with a single texture sample instead of with three.

Note: This projection method is not suitable for all textures or materials.  If you use this projection method and the seams are called out as an eye-sore, you should switch to using the Triplanar node that smoothly blends out the seams.


![](images/)

## Create Node menu category

The UV Triplanar Node is under the **UV** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
</ul> 


## Inputs 

[!include[nodes-inputs](./snippets/nodes-inputs.md)] <!-- MULTIPLE INPUT PORTS INCLUDE -->
| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Position**  | Vector 3 | defaults to using the Position node set to Absolute World Space.  Defines the position values that are used for the projection. |
|  **Normal**  | Vector 3 | defaults to using the Normal Vector node set to World Space.  Defines the normal to use to determine the direction masks for the projection. |
|  **Tile**  | Vector 3 | The number of times the projection should be tiled per meter on the X, Y, and Z axes. |
|  **Invert Backsides**  | Boolean | Determines whether or not to flip the projection on the back side of each axis. If you are using a texture with a definite direction (such as an image with readable text) this should be true to allow the texture to face the correct direction on all sides of the projection. Turning this on also eliminates mirroring artifacts along the projection borders. |


## Outputs

[!include[nodes-outputs](./snippets/nodes-outputs.md)] <!-- MULTIPLE OUTPUT PORTS INCLUDE -->
| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **XYZ**   | Vector 2 | UV coordinates projected in the X, Y, and Z directions |
|  **XZ**   | Vector 2 | UV coordinates projected in the X and Z (front, back, and sides) directions |
|  **Y**   | Vector 2 | UV coordinates projected in the Y (top) direction |


## Example graph usage 

In the following example, we use the UV Triplanar node to generate UV coordinates.  Then we use those projection coordinates fromn the XYZ output port to sample a texture. The result is a texture projected from the top, sides, and front.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]
This node is a subgraph, so you can double-click the node itself to open it and see how it works.
```
float TopBottom, LeftRight, FrontBack;
float3 Signs;
TriplanarDirectionMasks(WorldSpaceNormalVector, 4, false, TopBottom, LeftRight, FrontBack, Signs);
float3 position = AbsoluteWorldSpacePosition * Tile;
float2 projectionX = position.bg * float2(Signs.x, 1);
float2 projectionY = position.rg * float2(-Signs.z, 1);
float2 projectionZ = position.br * float2(-Signs.y, 1);
float2 Y = projectionZ;
float XZ = lerp(projectionX, projectionY, FrontBack);
float XYZ = lerp(XZ, projectionZ, TopBottom);
```

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[Triplanar Node](Triplanar-Node.md)
