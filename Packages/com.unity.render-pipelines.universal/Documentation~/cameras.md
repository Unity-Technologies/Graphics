# Cameras

A Camera in Unity works like a camera in the real world: it captures a view of objects in 3-dimensional space and flattens that view to display it on a 2-dimensional surface.

Cameras in the Universal Render Pipeline (URP) are based on Unity's standard Camera functionality, but with some significant differences. The most notable ways in which Cameras in URP differ from standard Unity Cameras are:

* The [Universal Additional Camera Data](universal-additional-camera-data.md) component, which extends the Camera component's functionality and allows URP to store additional Camera-related data
* The [Render Type](camera-types-and-render-type.md) setting, which defines the two types of Camera in URP: Base and Overlay
* The [Camera Stacking](camera-stacking.md) system, which allows you to layer the output of multiple Cameras into a single combined output
* The [Volume](Volumes.md) system, which allows you to apply [post-processing effects](integration-with-post-processing.md) to a Camera based on a given Transform's position within your scene
* The [Camera component](camera-component-reference.md), which exposes URP-specific properties in the Inspector

For a general introduction to how Cameras work in Unity, and examples of common Camera workflows, refer to the [Unity Manual section on Cameras](https://docs.unity3d.com/Manual/CamerasOverview.html).
