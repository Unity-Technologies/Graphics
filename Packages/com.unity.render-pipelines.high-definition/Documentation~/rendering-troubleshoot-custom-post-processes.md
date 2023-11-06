# Troubleshoot custom post-processes

If a custom post-processing effect doesn't display correctly:

* In your Project Settings, make sure this effect is listed under one of the post process order lists (see [Effect Ordering](custom-post-processing-create-apply.md#EffectOrdering)).

* Check that your effect's Shader compiles and that the reference to the Material in your post process Volume isn't null.

* In the Volume that contains your post process, make sure that it has a high enough priority and that your Camera is inside its bounds.

* Check that your shader doesn't contain any **clip()** instructions, that the blend mode is **Off** and the output alpha has a value of 1.

* If your effect doesn't work with dynamic resolution, use the `_PostProcessScreenSize` constant to make it fit the size of the screen. You only need to do this when  you also need normal or velocity and color.

