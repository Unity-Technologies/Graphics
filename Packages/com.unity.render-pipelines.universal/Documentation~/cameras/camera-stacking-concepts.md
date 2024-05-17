# Understand camera stacking

In the Universal Render Pipeline (URP), you use camera stacking to layer the output of multiple Cameras and create a single combined output. Camera stacking allows you to create effects such as a 3D model in a 2D UI, or the cockpit of a vehicle.

A camera stack consists of a [Base Camera](../camera-types-and-render-type.md#base-camera) and one or more [Overlay Cameras](../camera-types-and-render-type.md#overlay-camera). A camera stack overrides the output of the Base Camera with the combined output of all the cameras in the camera stack. As a result, anything you can do with the output of a Base Camera, you can do with the output of a camera stack. For example, you can render a camera stack to a render target, or apply post-processing effects.

Refer to [Set up a camera stack](../camera-stacking.md) for more information. To download examples of camera stacking in URP, install the [Camera Stacking samples](../package-sample-urp-package-samples.md#camera-stacking).

## Camera stacking and rendering order

URP performs several optimizations within a camera, including rendering order optimizations to reduce overdraw. However, when you use a camera stack, you define the order in which URP renders the cameras. You must be careful not to order the cameras in a way that causes excessive overdraw. For more information on overdraw in URP, refer to [Rendering order optimizations](../cameras-advanced.md#rendering-order-optimizations).

## Camera stacking and post-processing

You should only apply post-processing to the last camera in the stack, so the following applies:

* URP renders the post-processing effects only once, not repeatedly for each camera.
* The visual effects are consistent, because all the cameras in the stack receive the same post-processing.

## Additional resources

* [Set up a camera stack](../camera-stacking.md)
* [Camera component reference](../camera-component-reference.md)
