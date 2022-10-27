# UV Flow Map Node

Given a flow map directional input, the UV Flow Map node generates UV coordinates that can be used to create a flowing effect. This effect creates the illusion of directional movement in an otherwise static image and can be used for flowing water or to make static images appear to be alive with movement.  The effect is achieved by sampling an image two times.  The first sample uses the UV0 output of the UV Flow Map node and the second sample uses the UV1 output of the UV Flow Map node.  The two samples are then blended together using a Lerp node where the Lerp node’s T input port is connected from the Lerp output of the UV Flow Map node.

![](images/)

## Create Node menu category

The UV Flow Map Node is under the **UV** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
    [!include[nodes-fragment-only](./snippets/nodes-fragment-only.md)]       <!-- FRAGMENT ONLY INCLUDE  -->
</ul> 


## Inputs 

[!include[nodes-inputs](./snippets/nodes-inputs.md)] <!-- MULTIPLE INPUT PORTS INCLUDE -->
| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Flow Map**  | Vector 2 | x and y values that define a direction in which the flow should occur.  This data is generally stored in a flow map texture and should be expanded to the -1 to 1 range before passing it into the UV Flow Map node’s Flow Map input port. |
|  **Strength**  | Float | a multiplier for the flow map data that controls how far the UVs are pushed in the flow direction. |
|  **Flow Time**  | Float | the timing phase for the flow effect.  By default, this uses an internal Flow Map Time node to generate a spatially-varying phase offset for time.  For a more efficient effect, you could simply connect a time node to a frac node and pass that into Flow Time. Using the Flow Map Time node breaks up the uniform pulsing artifacts that can otherwise occur without it. |
|  **UV**  | Vector 2 | the UV coordinates to be used for the flow map effect. |


## Outputs

[!include[nodes-outputs](./snippets/nodes-outputs.md)] <!-- MULTIPLE OUTPUT PORTS INCLUDE -->
| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **UV0**   | Vector 2 | UV coordinates for the first texture sample |
|  **UV1**   | Vector 2 | UV coordinates for the second texture sample |
|  **Lerp**   | Float | a value used to blend between the first and second texture samples. |


## Example graph usage 

In the following example, we use the UV Flow Map node to generate UV0 and UV1. These two sets of UV are warped in the direction of the flow map over time until the warp goes too far.  At that point, the warp is reset. The second set of UVs is half of a phase offset from the first.  We use these texture coordinates to make two texture samples.  And we blend between the two samples using a lerp node whose T port is connected to the Lerp output of the UV Flow Map node. The lerp blends back and forth between the two samples so that one sample is displayed while the other resets. The result is the illusion of continously flowing movement.

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]

```
float2 flowDirection = -FlowMap * Strength;
float2 UV0 = (frac(FlowTime) * flowDirection) + UV;
float2 UV1 = (frac(FlowTime + 0.5) * flowDirection) + UV;
float Lerp = abs(frac(FlowTime) * 2 - 1)
```
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[Flow Map Time Node](Flow-Map-Time-Node.md)

