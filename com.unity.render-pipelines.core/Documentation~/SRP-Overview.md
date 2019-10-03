# Overview

From a high level point of view, you can divide SRP into two parts, the [SRP Asset](SRP-Asset.md), and the [SRP Instance](SRP-Instance.md). When you create a custom render pipeline, you need to implement both. 

## SRP Asset
The SRP Asset is a Unity Asset that represents a specific configuration for the pipeline. It stores information such as:
* Whether GameObjects should cast shadows.
* What Shader quality level to use.
* The shadow distance.
* The default Material configuration.

Things that you want to control and save as part of a configuration; anything that Unity needs to serialise. The SRP Asset represents the _type_ of SRP and the settings that you can configure for it.

## SRP Instance
The SRP Instance is the class that actually performs the rendering. When Unity sees that the Project uses SRP, it looks at the currently SRP Asset and asks it to provide a *rendering instance*. The Asset must return an instance that contains a **Render** function. Normally the instance also caches a number of settings from the SRP Asset.

The instance represents a pipeline configuration. From the render call, Unity can perform actions like:
* Clearing the framebuffer.
* Performing Scene culling.
* Rendering sets of GameObjects.
* Doing blits from one frame buffer to another.
* Rendering shadows.
* Applying post-processing effects.

The instance represents the _actual_ rendering that Unity performs.