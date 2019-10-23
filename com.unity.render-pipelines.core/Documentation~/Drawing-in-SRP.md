# Drawing in the Scriptable Render Pipeline

In the Scriptable Render Pipeline (SRP), you should draw after the [culling](Culling-in-SRP) process. When your SRP has a set of cull results, it can render relevant GameObjects to the screen.

Be aware that there are so many ways that you can configure your render pipeline to render the Scene, so you need to make a number of decisions before implementing it. Many of these decisions are driven by:

* The hardware you are targeting the render pipeline to.
* The specific look and feel you wish to achieve.
* The type of Project you are making.

For example, a 2D mobile sidescroller game and a 3D high-end PC first person game have vastly different constraints so should have vastly different render pipelines. Some concrete examples of real decisions to make include:

* HDR vs LDR
* Linear vs Gamma
* MSAA vs Post Process anti-aliasing
* Physically-based Materials vs Simple Materials
* Lighting vs No Lighting
* Lighting technique
* Shadowing technique

Making these decisions when writing the render pipeline help you determine many of the constraints it should have.

The below example demonstrates a simple renderer with no lights that can render some of the GameObjects as opaque.

## Filtering: Render Buckets and Layers
Generally, GameObjects have a specific classification. They can be opaque, transparent, sub-surface, etc... Unity uses a concept of queues for representing when to render a GameObject. These queues form buckets that Unity places GameObjects into (sourced from the Material on the object). When SRP renders the Scene, you specify which range of buckets to use.

In addition to buckets, you can also use standard Unity Layers for filtering.

This allows for additional filtering when drawing objects with SRP.

```c#
// Get the opaque rendering filter settings
var opaqueRange = new FilterRenderersSettings();
 
//Set the range to be the opaque queues
opaqueRange.renderQueueRange = new RenderQueueRange()
{
    min = 0,
    max = (int)UnityEngine.Rendering.RenderQueue.GeometryLast,
};
 
//Include all layers
opaqueRange.layerMask = ~0;
```

## Draw Settings: How things should be drawn
Using filtering and culling determines which GameObjects the SRP should render, but then you need to determine how it SRP renders them. SRP provides a variety of options to configure how to render GameObject that pass filtering. The structure that you use to configure this data is `DrawRenderSettings`. This structure allows you to configure a number of things:

* Sorting – The order in which to render GameObjects, examples include back-to-front and front-to-back.
* Per-Renderer flags – What ‘built in’ settings should Unity pass to the Shader, this includes things like per-GameObject Light Probes and per-GameObject Light maps.
* Rendering flags – What algorithm should SRP use for batching, like instancing or non-instancing.
* Shader Pass – Which Shader pass should SRP use for the current draw call.

```c#
// Create the draw render settings
// note that it takes a shader pass name
var drs = new DrawRendererSettings(myCamera, new ShaderPassName("Opaque"));
 
// enable instancing for the draw call
drs.flags = DrawRendererFlags.EnableInstancing;
 
// pass light probe and lightmap data to each renderer
drs.rendererConfiguration = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectLightmaps;
 
// sort the objects like normal opaque objects
drs.sorting.flags = SortFlags.CommonOpaque;
```

## Drawing
Now we have the three things we need to issue a draw call:

* Cull results

* Filtering rules
* Drawing rules

At this point, you can now issue a draw call. Like all things in SRP, you issue a draw call as a call into the [SRP Context](SRP-Context.md). In SRP, you normally don’t render individual Meshes, instead you issue a call that renders a large number of them at once. This reduces script execution overhead as well as allows fast, jobified, execution on the CPU.

To issue a draw call:

```c#
// draw all of the renderers
context.DrawRenderers(cullResults.visibleRenderers, ref drs, opaqueRange);

// submit the context, this will execute all of the queued up commands.
context.Submit();
```

This draws the GameObjects into the current render target. You can use a Command Buffer to switch the render target.