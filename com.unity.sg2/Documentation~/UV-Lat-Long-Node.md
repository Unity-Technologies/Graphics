# UV Lat Long Node

The UV Lat Long node creates UV coordinates for sampling a texture in Latitude/Longitude format.  This is useful for creating reflective environments as an alternative to cube maps.

![](images/)

## Create Node menu category

The UV Lat Long Node is under the **UV** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
</ul> 


## Inputs 

[!include[nodes-single-input](./snippets/nodes-single-input.md)] <!-- SINGLE INPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Dir**  | Vector 3 | the direction or normal to be converted to UV coordinates.  This could be a reflection vector (the default) or a normal depending on the desired effect |


## Outputs

[!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **UV**   | Vector 2 | UV coordinates that can be used for sampling a latitude longitude projected texture map. |

## Example graph usage 

In the following example, we create a reflection vector using the View Direction and Normal Vector node.  We pass the result into the UV Lat Long node.  The UV Lat Long node then generates the UV coordinates for sampling the texture - which is in Lat Long format.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]

```
float2 UV = float2(atan2(Dir.z, Dir.x) + 1.57, acos(Dir.y) + PI) * float2(0.1591549, -0.3183099);
```
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[UV Sphere Map](UV-Sphere-Map-Node.md)
[Polar Coordinates](Polar-Coordinates-Node.md)


