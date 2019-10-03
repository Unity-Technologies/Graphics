# Culling in the Scriptable Render Pipeline
Culling is the process of figuring out what to render on the the screen.

In Unity, Culling encompasses:

* **Frustum culling**: Which calculates the GameObjects  that exist between the Camera's near and far plane.
* **Occlusion culling**: Which calculates what GameObjects are hidden behind other GameObjects and then excluding them from rendering. For more information, see [Occlusion Culling](https://docs.unity3d.com/Manual/OcclusionCulling.html).

When Unity starts rendering, the first thing that it needs to calculate is what to render. This involves taking the Camera and performing a cull operation from the perspective of the Camera. The cull operation returns a list of GameObjects and Lights that are valid to render for the Camera. The Scriptable Render Pipeline(SRP) uses these GameObjects later in the render pipeline.

In SRP, you generally perform GameObject rendering from the perspective of a Camera. This is the same Camera GameObject that Unity uses for built-in rendering. SRP provides a number of API’s to begin culling with. Generally the flow looks like this:

```C#
// Create an structure to hold the culling paramaters
ScriptableCullingParameters cullingParams;

//Populate the culling paramaters from the camera
if (!CullResults.GetCullingParameters(camera, stereoEnabled, out cullingParams))
    continue;

// if you like you can modify the culling paramaters here
cullingParams.isOrthographic = true;

// Create a structure to hold the cull results
CullResults cullResults = new CullResults();

// Perform the culling operation
CullResults.Cull(ref cullingParams, context, ref cullResults);
```

Your SRP can now use these cull results to perform [rendering](Drawing-in-SRP.md).