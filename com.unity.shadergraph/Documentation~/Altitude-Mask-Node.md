# Altitude Mask Node 

The Altitude Mask node creates a grayscale (float) mask based on the height or altitude in the world.  The gradient begins with an output value of zero at the Minimum height and ends with an output value of one at the Maximum height.  Heights less than Minimum will always return zero and heights greater than Maximum will always return one.


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
|  **Minimum**  | Float | the altitude in world space where the zero to one gradient begins.  Values lower than Minimum are zero. |
|  **Maximum**  | Float | the altitude in world space where the zero to one gradient ends.  Values higher than Maximum are one. |


## Controls 

[!include[nodes-single-control](./snippets/nodes-single-control.md)]

| **Name** | **Type** | **Options**  | **Description** |
| :------  | :------- | :----------- | :-------------  |
|  **Falloff Type**  | Drop-Down | Smoothstep, Linear | Linear produces a straight gradient from Minimum to Maximum while Smoothstep produces a smooth hermite spline interpolation. |


## Outputs

[!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Out**   | Float | a zero to one gradient between the Minimum and Maximum altitude values. |

## Example graph usage 

In the following example, we use an Altitude Mask node to create a black to white mask that begins at a world height of -0.5 and ends at 1.5 meters.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

```
float Out = smoothstep(Minimum, Maximum, IN.AbsoluteWorldSpacePosition.y);
```

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[Triplanar Direction Mask node](Triplanar-Direction-Masks-Node.md)
[Distance Mask node](Camera-Distance-Mask-Node.md)