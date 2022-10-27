# Face Normal Node

The Face Normal node generates a normal for each of the faces of the mesh.  Unlike vertex normals, these normals are not blended across face edges so they produce faceted-looking results.  These normals are useful in situations where the vertex normals have been bent to achieve specific effects (as with foliage, for example) but where accurate normals are still required (as with measuring the angles of cards/billboards). This node could also be used to achieve a stylized/faceted look on your model without paying the high cost of breaking up all of the edges/vertices in the mesh.

![](images/)

## Create Node menu category

The Face Normal Node is under the **Input** &gt; **Geometry** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
    [!include[nodes-fragment-only](./snippets/nodes-fragment-only.md)]       <!-- FRAGMENT ONLY INCLUDE  -->
</ul> 


## Controls 

[!include[nodes-single-control](./snippets/nodes-single-control.md)]

| **Name** | **Type** | **Options**  | **Description** |
| :------  | :------- | :----------- | :-------------  |
|  **Space**  | Drop-Down | Object, View, World, Tangent | determines the space for the resulting normal |


## Outputs

[!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Out**   | Vector 3 | the face normal in the space selected with the drop-down |

## Example graph usage 

In the following example, we use the Face Normal node to generate a normal in World space.  The result is connected to the Normal input node in the master stack.  This results in a faceted looking sphere even though the vertex normals on the mesh are smooth.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]

```
float3 Out = normalize(cross(ddy(IN.AbsoluteWorldSpacePosition), ddx(IN.AbsoluteWorldSpacePosition)));
```
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[Normal Vector](Normal-Vector-Node.md)

