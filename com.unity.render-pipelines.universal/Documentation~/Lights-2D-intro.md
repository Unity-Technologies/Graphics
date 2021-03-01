# Introduction to 2D Lighting

The 2D Lighting system included with URP consists of a set of artist friendly tools and runtime components that help you quickly create a lit 2D Scene through core Unity components such as the Sprite Renderer, and 2D Light components that act as 2D counterparts to familiar 3D Light components.

These tools are designed to integrate seamlessly with 2D Renderers such as the Sprite Renderer, Tilemap Renderer, and Sprite Shape Renderer. This system of tools and components are optimized for mobile systems, and for running on multiple platforms.

### Differences from 3D Lights

There are a number of key differences between the implementation and behavior of 2D lights and 3D lights. These consists of the following:

#### New 2D specific components and render pass
The 2D Graphics systems includes its own set of 2D Light components, Shader Graph sub-targets and a custom 2D render pass that are specifically designed for 2D lighting and rendering. Editor tooling for the 2D Lights and pass configuration are also included.

#### Coplanar
The 2D Graphics lighting model was designed specifically to work with 2D worlds that are coplanar and multilayered. 2D Lights do not require depth separation between it and the object it is lighting. The 2D shadow system also works in coplanar and does not require depth separation.

#### Not physically based
The lighting calculation in 2D Lights is not physics based as it is with 3D Lights. The details of the lighting model calculation can be found here.

#### No interoperability with 3D Lights and 3D Renderers

Currently both 3D and 2D Lights can only affect 3D and 2D Renderers respectively. 2D Lighting does not work on or effect 3D Renderers such as the Mesh Renderer, while 3D Lighting will similarly have no effect on 2D Renderers such as the Sprite Renderer. While interoperability between the respective Lights and Renderers may be developed in the future, currently a combination of 2D and 3D Lights and 2D and 3D Renderers in a single Scene can be achieved by using the camera stacking technique.

### 2D Graphics Pipeline technical details
The 2D Graphics pipeline rendering process can be broken down into 2 distinct phases:
1) Draw Light Render Textures
2) Draw Renderers

Light Render Textures are Render Textures that contain information about the Light’s color and shape in screen space.

These two phases are only repeated  for each distinctly lit set of Light Layers. In other words, if Sorting Layer 1-4 has the exact same set of Lights, it will only perform the above set of operations once.

The default setup allows a number of batches to be drawn ahead of time before drawing the Renderers to reduce target switching. The ideal setup would allow the pipeline to render the For example, if the setup allows it, the pipeline will render the Light Render Textures for all the batches and only then move on to draw the Renderers. This prevents loading and unloading of the color target. See <Optimization> for more detailed information.
Pre-phase: Calculate Sorting Layer batching
Before proceeding with the  any rendering phases,actually takes place, the 2D Graphics pipeline firstwill analyses the Scene to assess which Layers can be batched together in a single draw operation. The following is the criteria that determine whether Layers are batched together:
They are consecutive Layers.
They share the exact same set of Lights.

It is highly recommended to batch as many Layers as possible to minimize the number of Light Render Textures draw operations and improve performance.


#### Phase 1: Draw Light Render Textures
After the batching , the pipeline then draws the Light Textures for that batch. This essentially draws the Light’s shape onto a Render Texture. The light’s color and shape can be blended onto the target Light Render Texture using Additive or Alpha Blended depending on the light’s setup.

![](Images/2D/introduction_phase1.png)

It is worth noting that a Light Render Texture is only created whenever there is at least 1 2D Lights that are targeting it. For example, if all the lights of a layer only uses Blend Style #1, then there is only 1 Light Render Texture created.

#### Phase 2: Draw Renderers
Once all the Light Render Textures has been drawn, it is now the turn to draw the Renderers. The system will keep track which set of Light Render Textures should be used to draw which set of renderers. They are associated during the batching process in Phase 0.

When the Renderers are being drawn, it will have access to all (one for each blend style) the available Light Render Textures. In the shader, the final color is calculated by combining the input color with colors from the Light Render Texture using the specified operation.

![](Images/2D/introduction_phase2.png)

An example of a setup with 4 active blend styles illustrating how multiple blend styles come together. In most cases, you would only need 2 blend styles.

### Optimization
In addition to the standard optimization techniques such as reducing draw calls, culling and optimizing Shaders, there are several techniques and considerations that are unique to the 2D Graphics pipeline.

#### Number of Blend Styles
The easiest way to increase rendering performance is to reduce the number of blend styles used. Each blend style is a Render Texture that needs to be rendered and subsequently uploaded.

Reducing the number of blend styles has a direct impact on the performance. For simple scenes a single blend style could suffice. It is also common to use up to 2 blend styles in a scene.
Light Render Texture Scale
The 2D Graphics system relies on screen space Light Render Texture to capture light contribution. This means there are a lot of Render Texture drawing subsequent uploading. Choosing the right Render Texture size directly impacts the performance.

By default it is set at 0.5x of screen resolution. Smaller Light Render Texture size will give better performance at the cost of visual artifact. Half screen size resolution provides a good performance with almost no noticeable artifact in most situations.

Experiment and find a scale suitable for your project.

#### Layer Batching
To further reduce the number of Light Render Textures, it is crucial to make the sorting layer batchable. Layers that are batched together share the same set of Light Render Textures. Uniquely lit layers will have its own set thus increasing the amount of work needed.

Layers can be batch together if they share the same set of lights.

#### Pre-rendering of Light Render Texture
Multiple sets of Light Render Textures can be rendered a head of drawing the renderers. In an ideal situation, all the Light Render Textures will be rendered upfront and only then the pipeline moves on to drawing the renderers onto the final color output. This reduces the need to load/unload/reload of final color output.

In a very complex setup with a lot distinctly lit layers, it may not be practical to prerender all Light Render Textures. The limit can be configured in the 2D Renderer Data inspector.

#### Normal Maps
Using normalps to simulate depth is currently a very expensive operation. If it is enabled, a full size Render Texture is created during a depth prepass and the renderers are drawn onto it. This is done for each layer batch.

If normal mapping effect to simulate depth perception is not needed, ensure that all lights have the normal map option disabled.
