# Introduction to Scriptable Renderer Features

Scriptable Renderer Features are components you can add to a renderer to alter how URP renders a project.

The following sections explain the fundamentals of Scriptable Renderer Features:

* [What is a Scriptable Renderer Feature?](#scriptable-renderer-feature)
* [Scriptable Renderer Feature or Scriptable Render Pass?](#renderer-feature-or-render-pass)

Scriptable Render Passes are a fundamental part of Scriptable Renderer Features. For more information, refer to [Scriptable Render Pass Fundamentals](../intro-to-scriptable-render-passes.md).

## <a name="scriptable-renderer-feature"></a>What is a Scriptable Renderer Feature

A Scriptable Renderer Feature is a customizable type of [Renderer Feature](../../urp-renderer-feature.md), which is a scriptable component you can add to a renderer to alter how Unity renders a scene or the objects within a scene. The Scriptable Renderer Feature manages and applies Scriptable Render Passes to create custom effects.

Scriptable Renderer Features control when and how the Scriptable Render Passes apply to a particular renderer or camera, and can also manage multiple Scriptable Render Passes at once. This makes it easier to create complex effects which require multiple render passes with a Scriptable Renderer Feature than by injecting individual Scriptable Render Passes.

## <a name="renderer-feature-or-render-pass"></a>Scriptable Renderer Feature or Scriptable Render Pass?

Scriptable Renderer Features and Scriptable Render Passes can both achieve similar outcomes but some scenarios suit the use of one over the other. The key difference is in the workflow for the two methods, a Scriptable Renderer Feature must be added to a renderer in order to run, while Scriptable Render Passes offer more flexibility but require additional work to apply across multiple scenes.

Scriptable Renderer Features are useful for effects you want to apply to multiple cameras, scenes, or across your entire project. When you add the Scriptable Renderer Feature to a renderer, everything that uses that renderer uses the Scriptable Renderer Feature. This means you can make a change to the Scriptable Renderer Feature once and apply it everywhere that effect is in use.

Alternately, the injection of individual Scriptable Render Passes offers the ability to add an effect at a single point within a scene or project. This avoids the need for complex scripts such as a renderer feature that works with volumes, and also helps to minimize the possible performance impact of adding such effects. For more information on this, refer to [Scriptable Render Passes in Scenes](../intro-to-scriptable-render-passes.md#scriptable-render-passes-in-scenes).

## Additional resources

* [Introduction to Scriptable Render Passes](../intro-to-scriptable-render-passes.md)
* [How to create a Custom Renderer Feature](../create-custom-renderer-feature.md)
