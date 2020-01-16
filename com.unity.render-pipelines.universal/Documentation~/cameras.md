# Cameras

A Camera in Unity works like a camera in the real world: it captures a view of objects in 3-dimensional space and flattens that view to display it on a 2-dimensional surface.

The Universal Render Pipeline (URP) uses Unity's standard [Camera component](https://docs.unity3d.com/Manual/class-Camera.html), but it extends and overrides some of its functionality.

The most notable ways in which the Camera system in URP differs from the standard Unity Camera are:

* The [UniversalAdditionalCameraData](universal-additional-camera-data.md) component, which allows URP to store additional Camera-related data
* The [Render Type](camera-types-and-render-type.md) setting, which defines the two types of Camera in URP: Base and Overlay
* The [Camera Stacking](camera-stacking.md) system, which allows you to layer the output of multiple Cameras into a single combined output
* The [Volume](Volumes.md) system, which allows you to apply [post-processing effects](integration-with-post-processing.md) to a Camera based on a given Transform's position within your Scene

For a general introduction to how Cameras work in Unity, and examples of common Camera workflows, see [the Unity Manual section on Cameras](https://docs.unity3d.com/Manual/CamerasOverview.html).
