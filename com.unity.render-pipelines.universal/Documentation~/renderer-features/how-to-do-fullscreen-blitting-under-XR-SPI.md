# Example: How to create a full screen blitting feature under Single Pass Instanced rendering in XR

The example on this page describes how to create a custom scriptable rendering feature doing full screen blit in XR

## Example overview

This example adds a ScriptableRenderPass that blits ScriptableRenderPassInput.Depth to the CameraColorTarget. It uses the command buffer to draw a full screen mesh for both eyes.
The example also includes a shader used to perform the GPU side of the rendering, which works by sampling the depth buffer using XR sampler macros. 

## Prerequisites

This example requires the following:

* A Unity project with the URP package installed.

* The **Scriptable Render Pipeline Settings** property refers to a URP asset (**Project Settings** > **Graphics** > **Scriptable Render Pipeline Settings**).

## Create example Scene and GameObjects<a name="example-objects"></a>

To follow the steps in this example, create a new Scene with the following GameObjects:

1. Create a Cube. 

    ![Scene Cube](../Images/how-to-blit-in-xr/rendobj-cube.png)

Now you have the setup necessary to follow the steps in this example.

## Example implementation

This section assumes that you created a Scene as described in section [Example Scene and GameObjects](#example-objects).

The example implementation uses Scriptable Renderer Features: to draw depth buffer information on screen.

### Create a Renderer Feature and configure both input and output

Follow these steps to create a Renderer Feature

The example is complete. When running in playmode, depth buffer is displayed.