# Introduction to Scriptable Render Passes

Scriptable Render Passes are a way to alter how Unity renders a scene or the objects within a scene. They allow you to fine tune how Unity renders each scene in your project on a scene-by-scene basis.

The following sections explain the fundamentals of Scriptable Render Passes:

* [What is a Scriptable Render Pass?](#scriptable-render-pass)
* [Scriptable Render Passes in Scenes](#scriptable-render-passes-in-scenes)

You can use Scriptable Renderer Features to inject Scriptable Render Passes into a renderer. For more information, refer to [Scriptable Render Passes in Scenes](#scriptable-render-passes-in-scenes).

## <a name="scriptable-render-pass"></a>What is a Scriptable Render Pass?

You inject a Scriptable Render Pass into the render pipeline to achieve a custom visual effect. To do this, you add the Scriptable Render Pass via a MonoBehavior script with the `EnqueuePass` method and add this script as a component to a renderer, camera, or GameObject.

A Scriptable Render Pass lets you to do the following:

* Change the properties of materials in your scene.
* Change the order that Unity renders GameObjects in.
* Lets Unity read camera buffers and use them in shaders.

For example, you can use a Scriptable Render Pass to blur a camera’s view when showing the in-game menu.

Unity injects Scriptable Render Passes at certain points during the URP render loop. These points are called injection points. You can change the injection point Unity inserts your pass at to control how the Scriptable Render Pass affects the appearance of your scene. For more information on injection points, refer to [Injection Points](../customize/custom-pass-injection-points.md).

## Scriptable Render Passes in Scenes

You can inject a Scriptable Render Pass into a scene via any GameObject present in the scene. This gives you more precise control over when the render pass is active. But this means you must have a GameObject inject the render pass at every point you want to use it. As a result, it's better to inject any common effects in your project via a Scriptable Renderer Feature instead.

When you inject a Scriptable Render Pass into a scene via any GameObject, it's important to consider how URP uses this script. The first Camera to render the Scriptable Render Pass uses up the render pass, and is the only Camera the render pass applies to. Any Cameras that the Scriptable Render Pass would apply that render after the first Camera don't render the effect.

For example, if you have two Cameras and you add the Scriptable Render Pass in the `Update` method, only the first Camera to render uses the Scriptable Render Pass effect. This is because the first camera uses up the instance of the effect. As the second Camera renders before the next call of the `Update` method, a second instance of the Scriptable Render Pass isn't available to use. As a result, the second Camera doesn't apply the effect from the Scriptable Render Pass to its output.

## Additional resources

* [How to create a Custom Renderer Feature](create-custom-renderer-feature.md)
* [Scriptable Renderer Feature Reference](scriptable-renderer-features/scriptable-renderer-feature-reference.md)
* [How to inject a Custom Render Pass via scripting](../customize/custom-pass-injection-points.md)
