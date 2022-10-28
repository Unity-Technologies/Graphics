# Color To Grayscale Node

The Color To Grayscale node converts an RGB color (Vector 3) to a grayscale value (Float) using the method specified with the Method drop-down menu. The conversion is done by multiplying each component of the color by a weight and then adding the results together. If you want to use your own weights, select "Custom" from the Method drop-down. This node is useful when you have color data but only need the luminance of that data or only need to perform operations on part of the data.  Performing operations on a single channel is cheaper than performing them on the full, 3-channel color.

![](images/)

## Create Node menu category

The Color To Grayscale Node is under the **Artistic** &gt; **Adjustment** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
</ul> 


## Inputs 

[!include[nodes-single-input](./snippets/nodes-single-input.md)] <!-- SINGLE INPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Color**  | Vector 3 | an RGB color value to be converted to grayscale                |

## Controls 

[!include[nodes-single-control](./snippets/nodes-single-control.md)]

<table>
    <thead>
        <tr>
            <th><strong>Name</strong></th>
            <th><strong>Type</strong></th>
            <th><strong>Options</strong></th>
            <th><strong>Description</strong></th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td><strong>Method</strong></td>
            <td>Drop-down</td>
            <td>Luminosity<br>Gray<br>Red<br>Green<br>Blue<br>Cyan<br>Magenta<br>Yellow<br>Custom</td>
            <td>Choose the method used to convert the color to a grayscale value:
				</br>
				<ul>
					<li><strong>Luminosity</strong>: returns the luminosity or apparent brightness of the input color assuming a color in the BT709 color space. (0.2126, 0.7152, 0.0722) </li>
					<li><strong>Gray</strong>: returns a mid-gray blend of all three color values. (0.333, 0.333, 0.333)</li>
					<li><strong>Red</strong>: returns only the red contribution (1,0,0)</li>
					<li><strong>Green</strong>: returns only the green contribution (0,1,0)</li>
					<li><strong>Blue</strong>: returns only the blue contribution (0,1,0)</li>
					<li><strong>Cyan</strong>: returns a half blend between green and blue (0,0.5,0.5)</li>
					<li><strong>Magenta</strong>: returns a half blend between red and blue (0.5,0,0.5)</li>
					<li><strong>Yellow</strong>: returns a half blend between red and green (0.5,0.5,0)</li>
                    <li><strong>Custom</strong>: uses the Custom Weights value to calculate the results</li>
				</ul>
				</td>
        </tr>
        <tr>
            <td><strong>Custom Weights</strong></td>
            <td>Vector 3</td>
            <td></td>
            <td>when Method is set to custom, the node uses these values to know how much of the three channels to blend for the final result.</td>
        </tr>
    </tbody>
</table>

## Outputs

[!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|**Grayscale**|Float|a grayscale value derived from the RGB input using the selected method|

## Example graph usage 

In the following example, we sample a color texture and then pass the values into the Color To Grayscale node.  The node's Method drop-down is set to Luminosity, so the resulting output is a grayscale value that represents the brightness or luminosity of each pixel.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]

```
float Grayscale = dot(Color, float3(0.2126, 0.7152, 0.0722));
```
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[Saturation Node](Saturation-Node.md)
[Dot Product Node](Dot-Product-Node.md)