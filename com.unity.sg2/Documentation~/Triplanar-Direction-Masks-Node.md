# Triplanar Direction Masks Node

Given an input direction or Normal, the Direction Masks node creates masks in the Top/Bottom, Left/Right, and Front/Back directions. The model is shaded white in the mask if the normal is facing one of these cardinal directions.  It also outputs the sign (positive or negative) of the normal in those directions. This node is useful for masking world space texture projections and other effects that need cardinal direction masks.

![](images/)

## Create Node menu category

The Levels Node is under the **Artistic** &gt; **Mask** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
</ul> 


## Inputs 

[!include[nodes-inputs](./snippets/nodes-inputs.md)] <!-- MULTIPLE INPUT PORTS INCLUDE -->
| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Normal**  |  Vector 3 | the surface normal or direction to be used to create the mask.  Defaults to using the Vertex Normal in World Space.  If this direction is facing the top/bottom, Left/Right, or Front/Back, the resulting mask will be white.  Otherwise it will be black. |
|  **Sharpness**  |  Float | Controls the blurriness or sharpness of the edges of the mask - or how wide the blending borders are between the masks higher values create small blending borders. |
|  **Smooth Edges**  |  Boolean |  when false, the masks are binary with no blending borders at all and the Sharpness input is ignored. |



## Outputs

[!include[nodes-outputs](./snippets/nodes-outputs.md)] <!-- MULTIPLE OUTPUT PORTS INCLUDE -->
| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **TopBottom**   |  Float | this mask is white where the object is facing up or down (the positive or negative Y direction). |
|  **LeftRight**   |  Float | this mask is white where the object is facing left or right (the positive or negative X direction). |
|  **FrontBack**   |  Float | this mask is white where the object is facing forward or backward (the positive or negative Z direction). |
|  **Signs**   |  Vector 3 | this three channel value returns -1 or 1 in each of the three channels, which denotes whether the input normal is pointing in the positive or negative direction along each of the three axes. |


## Example graph usage 

In the following example, we connect a Normal Vector node to the Normal input of the Direction Masks node. This produces TopBottom, LeftRight, and FrontBack masks in areas of the model where the normals line up with those directions.  With the Binary input on, the masks have hard edges with no blending.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]

```
float3 edges = pow(abs(IN.AbsoluteWorldSpaceNormal), EdgeSharpness);
float3 masks = edges / (dot(edges, float3(1,1,1)));
float TopBottom = masks.y;
float LeftRight = masks.x;
float FrontBack = masks.z;
float3 Signs = sign(IN.AbsoluteWorldSpaceNormal)
```
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[Distance Mask node](Camera-Distance-Mask-Node.md)
[Altitude Mask node](Altitude-Mask-Node.md)

