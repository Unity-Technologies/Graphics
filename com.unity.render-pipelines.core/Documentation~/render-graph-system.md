# The render graph system

The render graph system is a system that sits on top of Unity's Scriptable Render Pipeline (SRP). It allows you to author a custom SRP in a maintainable and modular way. Unity's High Definition Render Pipeline (HDRP) uses the render graph system.

You use the [RenderGraph](../api/UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph.html) API to create a render graph. A render graph is a high-level representation of the custom SRP's render passes, which explicitly states how they use resources.

Describing render passes in this way has two benefits: it simplifies render pipeline configuration, and it allows the render graph system to efficiently manage various parts of the render pipeline, which can result in improved runtime performance. For more information on the benefits of the render graph system, see [benefits of the render graph system](render-graph-benefits.md).

To use the render graph system, you need to write your code in a different way to a regular custom SRP. For more information on how to write code for the render graph system, see [writing a render pipeline](render-graph-writing-a-render-pipeline.md).

For information on the technical principles behind the render graph system, see [render graph fundamentals](render-graph-fundamentals.md).

**Note**: Render graph is currently experimental which means Unity may change its API during future development.

This section contains the following pages:

- [Render graph benefits](render-graph-benefits.md)
- [Render graph fundamentals](render-graph-fundamentals.md)
- [Writing a render pipeline](render-graph-writing-a-render-pipeline.md)