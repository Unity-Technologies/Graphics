# Camera Distance Mask Node

The Camera Distance Mask node creates a grayscale (float) mask based on the distance from the camera where values closer to the camera are black (0) and values further from the camera are white (1).  The Mask Start value is a distance in meters from the camera where the mask’s gradient should begin.  So distances between the camera’s location up to the Mask Start value will always be black (0). The Mask Length value defines how long the gradient is - so the gradient will be between the Mask Start value and the Mask Length value.  All values after the Mask Length value will be white (1).

![](images/)

## Create Node menu category

The Distance Mask Node is under the **Artistic** &gt; **Mask** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
</ul> 


## Inputs 

[!include[nodes-inputs](./snippets/nodes-inputs.md)] <!-- MULTIPLE INPUT PORTS INCLUDE -->
| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Mask Start**  | Float | the distance from the camera to the point where the gradient begins|
|  **Mask Length**  | Float | the length of the black to white gradient |



## Outputs

[!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Out**   | Float | a gradient from zero to one where values between the camera and Mask Start are zero, values between Mask Start and Mask Length are between zero and one, and values greater than Mask Length are one. |

## Example graph usage 

In the following example, we use a Distance Mask node to create a black to white mask that begins 4.22 meters from the camera and extends 1.4 meters from there - so the mask is black at 4.22 meters and white at 5.62 meters.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]

```
float Out = saturate((distance(_WorldSpaceCameraPos, IN.AbsoluteWorldSpacePosition) - MaskStart) / MaskLength);
```
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[Direction Mask node](Triplanar-Direction-Masks-Node.md)
[Altitude Mask node](Altitude-Mask-Node.md)