# Sprite Skinning Node

## Description

This node lets you apply Vertex Skinning, and only works with the [2D Animation](https://docs.unity3d.com/Packages/com.unity.2d.animation@latest/). You must 
use the [SpriteSkin](https://docs.unity3d.com/Packages/com.unity.2d.animation@latest?subfolder=/manual/SpriteSkin.html) component provided with the 2D Animation Package.  
Please ensure the following settings are enabled:      
    1. GPU Skinning is enabled in Player/Rendering/GPU Skinning in Project Settings.  
    2. SRP-Batcher enabled in RenderpipelineAsset.  

## Ports
| Name      | Direction  | Type    | Stage  | Description |
|:--------- |:-----------|:--------|:-------|:------------|
| Position  | Input      | Vector3 | Vertex | Position of the vertex in object space. |
| Normal    | Input      | Vector3 | Vertex | Normal of the vertex in object space. |
| Tangent   | Input      | Vector3 | Vertex | Tangent of the vertex in object space. |
| Position  | Output     | Vector3 | Vertex | Outputs the skinned vertex position. |
| Normal    | Output     | Vector3 | Vertex | Outputs the skinned vertex normal. |
| Tangent   | Output     | Vector3 | Vertex | Outputs the skinned vertex tangent. |
