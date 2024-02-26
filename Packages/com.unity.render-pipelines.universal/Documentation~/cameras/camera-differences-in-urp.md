# Cameras in URP

Cameras in the Universal Render Pipeline (URP) are based on Unity's standard camera functionality, but with some significant differences. For example, URP cameras use the following:

- The [Universal Additional Camera Data](../universal-additional-camera-data.md) component, which extends the Camera component's functionality and allows URP to store additional camera-related data.
- The [Render Type](../camera-types-and-render-type.md) setting, which defines the two types of camera in URP: Base and Overlay.
- The [Camera Stacking](../camera-stacking.md) system, which allows you to layer the output of multiple Cameras into a single combined output.
- The [Volume](../Volumes.md) system, which allows you to apply [post-processing effects](../integration-with-post-processing.md) to a camera based on the position of a Transform in your scene.
- The [Camera component](../camera-component-reference.md), which exposes URP-specific properties in the Inspector.

For a general introduction to how cameras work in Unity, and examples of common Camera workflows, refer to [the Unity manual section on Cameras](https://docs.unity3d.com/Manual/CamerasOverview.html).
