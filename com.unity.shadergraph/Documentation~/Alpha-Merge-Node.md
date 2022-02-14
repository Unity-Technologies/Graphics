# Alpha Merge Node

The Alpha Merge node takes a Vector 3 and a Float and merges them to form a Vector 4 where the float value is appended as the last component. This node is useful for joining an RGB color and an alpha channel.

![](images/)

## Create Node menu category

The Alpha Merge Node is under the **Channel** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
</ul> 


## Inputs 

[!include[nodes-inputs](./snippets/nodes-inputs.md)] <!-- MULTIPLE INPUT PORTS INCLUDE -->
| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **RGB**  | Vector 3 | an RGB color to be combined with an alpha channel |
|  **Alpha**  | Float | an alpha channel to be combined with a color |


## Outputs

[!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **RGBA**   | Vector 4 | an RGBA color with the input RGB in the first three components and the input alpha in the forth component |

## Example graph usage 

In the following example, we use an Alpha Merge node to combine a color and a float value into an RGBA value.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]

```
float4 RGBA = float4(RGB, Alpha);
```
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[Split Node](Split-Node.md)
[Combine Node](Combine-Node.md)
[Alpha Split Node](Alpha-Split-Node.md)
[Swizzle Node](Swizzle-Node.md)
