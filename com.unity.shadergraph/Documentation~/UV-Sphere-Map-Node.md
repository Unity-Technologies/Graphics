# UV Sphere Map Node

The UV Sphere Map node creates UV coordinates for sampling a texture in sphere map format.  This is useful for creating cheap reflection effects and MatCap materials.

![](images/)

## Create Node menu category

The UV Sphere Map Node is under the **UV** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
</ul> 


## Inputs 

[!include[nodes-single-input](./snippets/nodes-single-input.md)] <!-- SINGLE INPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Normal**  | Vector 3 | the direction or normal to be converted to UV coordinates.  This could be a reflection vector (the default) or a normal depending on the desired effect |

## Controls 

[!include[nodes-single-control](./snippets/nodes-single-control.md)]

| **Name** | **Type** | **Options**  | **Description** |
| :------  | :------- | :----------- | :-------------  |
|  **Input Space**  | Drop-Down | View, World, Tangent | set this drop-down to the space that your input Normal is in.  This defaults to Tangent space so in most cases you can simply connect a tangent space normal map, but you could also connect a normal in View space or World space. |


## Outputs

[!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **UV**   | Vector 2 | UV coordinates that can be used for sampling a sphere map projected texture map. |

## Example graph usage 

In the following example, we pass the Normal Vector into the UV Sphere Map node.  This creates the UVs required to sample the sphere map texture.  Note that the Normal Vector is set to View space and so the Input Space of the UV Sphere Map node is also set to View space. If you were to pass a regular normal map into the UV Sphere Map node, you would set the Input Space to Tangent instead.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]

```
float3 cross = cross(normalize(IN.ViewSpacePosition), IN.ViewSpaceNormal)
float2 UV = float2(-cross.y, cross.x) * 0.5 + 0.5;
```
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[UV Lat Long](UV-Lat-Long-Node.md)
[Polar Coordinates](Polar-Coordinates-Node.md)
