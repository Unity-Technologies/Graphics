# Alpha Split Node

The Alpha Split node takes a vector 4 and splits it into a vec3 and a float where the vec3 contains the first three components of the vec4 and the float contains the last component.  This node is useful for separating the alpha channel from a color.

![](images/)

## Create Node menu category

The Alpha Split Node is under the **Channel** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
</ul> 


## Inputs 

[!include[nodes-single-input](./snippets/nodes-single-input.md)] <!-- SINGLE INPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **RGBA**  | Vector 4 | a vec4 to be split into RGA and Alpha |


## Outputs

[!include[nodes-outputs](./snippets/nodes-outputs.md)] <!-- MULTIPLE OUTPUT PORTS INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **RGB**   | Vector 3 | the first three components of the incoming vec4. |
|  **Alpha**   | Float | the last component of the incoming vec4. |


## Example graph usage 

In the following example, we use the Alpha Split node to split out the RGB color value and the Alpha channel from a texture sample.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]

```
float3 RGB = RGBA.xyz;
float Alpha = RGBA.w;
```
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[Split Node](Split-Node.md)
[Combine Node](Combine-Node.md)
[Alpha Merge Node](Alpha-Merge-Node.md)
[Swizzle Node](Swizzle-Node.md)
