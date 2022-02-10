# Levels Node

The Levels node allows the user to adjust the input using the standard level adjustments similar to those found in image manipulation software. It is useful for adjusting the black point, contrast, and white point of the input and the darkness and brightness of the output.

To increase the contrast of the input color, increase the Black Point value to match the darkest value in the input data and decrease the White Point value to match the brightest value in the input data. Then increase or decrease the Contrast value to preference.

Note: Performing adjustments on textures has a performance cost when done in a real-time shader. These types of adjustments should only be done in the shader when performed on dynamic data. It is always more efficient to make these types of adjustments off-line in the texture itself in image editing software where the math can be done once instead of for every pixel on every frame.  


![](images/)

## Create Node menu category

The Levels Node is under the **Artistic** &gt; **Adjustment** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
</ul> 


## Inputs 

[!include[nodes-inputs](./snippets/nodes-inputs.md)] <!-- MULTIPLE INPUT PORTS INCLUDE -->
| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **In** | Vector 3 |  the input color to be adjusted |
|  **Black Point** | Float |  (range 0-1) adjusting this value higher than zero moves the black point up so that all values become darker. For maximum contrast, this value should be set to the darkest value in the data. |
|  **Contrast** | Float |  (range 0-2) adjusting this value up or down changes the gamma curve or contrast of the image so that mid-range values become brighter or darker. |
|  **White Point** | Float |  (range 0-1) adjusting this value lower than one moves the white point down so that all values become brighter. For maximum contrast, this value should be set to the brightest value in the data. |
|  **Darkness** | Float |  (range 0-1) sets the minimum dark value |
|  **Brightness** | Float |  (range 0-1) sets the maximum brightness value |

## Outputs


[!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Out** | Vector 3 |  the resulting adjusted color |

## Example graph usage 

In the following example, we adjust the levels on a sampled texture.  The Sample Texture 2D is connected to the In port on the Levels node.  We then adjusted the input values in the levels node to increase the contrast in the image.  Finally, we pass the updated color from the Out port on the levels node to the Base Color port of the Master Stack.

Note - this is a good example of what not to do with the Levels node.  The texture sample and levels settings are both static, which means that this operation could be performed offline and baked into the texture itself rather than performed in real-time.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]


```
float3 Out = pow((In - InBlack) / (InWhite - InBlack), InGamma) * (OutWhite - OutBlack) + OutBlack;
```
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[Contrast Node](Contrast-Node.md)
