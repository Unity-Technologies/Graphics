# Flow Map Time Node

The Flow Map Time node is a support node intended for use with the UV Flow Map node.  It creates the data required by the UV Flow Map node’s Flow Time input.  Its purpose is to create a spatially-varying phase offset so that the Flow Map node’s flow effect isn’t phase synchronized. Using the Flow Map Time node with the UV Flow Map node breaks up the uniform pulsing artifacts that can otherwise occur without it.

![](images/)

## Create Node menu category

The Flow Map Time Node is under the **UV** category in the Create Node menu.

## Compatibility 

<ul>
    [!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->
    [!include[nodes-fragment-only](./snippets/nodes-fragment-only.md)]       <!-- FRAGMENT ONLY INCLUDE  -->
</ul> 


## Inputs 

[!include[nodes-inputs](./snippets/nodes-inputs.md)] <!-- MULTIPLE INPUT PORTS INCLUDE -->
| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **Phase Offset Mask**  | Texture | a mask texture used to offset the time phase of the flow effect |
|  **Phase Offset UVs**  | Vector 2 | the UVs used to sample the Phase Offset Mask. Defaults to UV0 |
|  **Phase Offset Strength**  | Float | a multiplier for the phase offset mask to control the strength of the effect |
|  **Speed**  | Float | controls the speed of the flow effect or frequency of the phase. |


## Controls 

[!include[nodes-single-control](./snippets/nodes-single-control.md)]

| **Name** | **Type** | **Options**  | **Description** |
| :------  | :------- | :----------- | :-------------  |
|  **Mask Channel**  | Drop-Down | Red, Green, Blue, Alpha | selects the channel of the Phase Offset Mask texture that contains the mask data. |


## Outputs

[!include[nodes-single-output](./snippets/nodes-single-output.md)] <!-- SINGLE OUTPUT PORT INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
|  **FlowTime**   | Float | a spatially varying phase offset value that can be connected to the Flow Time port of the UV Flow Map node to break up the pulsing artifacts that are typical with standard flow mapping. |

## Example graph usage 

In the following example, we use the FlowTime node to generate a spacially varrying phase offset.  This is then used as the input to the FlowTime port on the UV Flow Map node.  

![](images/)

## Generated code example

[!include[nodes-generated-code](./snippets/nodes-generated-code.md)]

```
float FlowTime = frac((SAMPLE_TEXTURE2D(_Phase_Offset_Mask, samplerstate, PhaseOffsetUVs).r * PhaseOffsetStrength) + (Time * Speed));
```
This node is a subgraph, so you can double-click the node itself to open it and see how it works.

## Related nodes 
[!include[nodes-related](./snippets/nodes-related.md)]
[UV Flow Map Node](UV-Flow-Map-Node.md)

