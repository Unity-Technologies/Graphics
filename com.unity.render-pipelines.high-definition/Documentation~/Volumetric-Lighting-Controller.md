# Volumetric Lighting Controller

The High Definition Render Pipeline evaluates volumetric lighting on a 3D grid mapped to the volumetric section of the frustum. The resolution of the grid is quite low (it is 240x135x64 using the default quality setting at 1080p), so it's important to keep the dimensions of the frustum as small as possible to maintain high quality. Use [Distant Fog](https://github.com/Unity-Technologies/ScriptableRenderPipeline/wiki/Volumetric-Fog) for the less visually important background areas.

## Controlling the range of Volumetric Fog

To control the size of the volumetric section of the frustum, add a **Volumetric Lighting Controller** component to your Scene. Find or create a GameObject with a Volume component, and add the Volumetric Lighting Controller override.

Adjust the **Distance Range** on the Volumetric Lighting Controller component to define the maximum and minimum range for the volumetric fog relative to the Camera’s frustum. 

![](Images/SceneSettingsVolumetricLightingController1.png)

The Volumetric Lighting Controller has two properties:
|**Property**| **Description**|
|:----------------------------- |:------------------------------------------------------------ |
| **Distance Range**            | Determines the distance (in Unity Units) from the Camera that the volumetric fog section of the frustum begins and ends. |
| **Depth Distribution Uniformity** | Controls the uniformity of the distribution of slices along the Camera's focal axis. A value of 0 makes the distribution exponential (the spacing between the slices increases with the distance from the Camera), and the value of 1 results in a uniform distribution. |

