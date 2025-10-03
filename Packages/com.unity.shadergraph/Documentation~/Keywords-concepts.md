# Introduction to keywords in Shader Graph

Keywords enable you to create shader variants that address multiple contexts, for example:

* Features that you can turn on or off for each material instance.
* Features that behave differently on certain platforms.
* Shaders that scale in complexity based on conditions you set.

For more information about keywords and how they affect the final shader, refer to [Changing how shaders work using keywords](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html).

## Conditions and keyword types

There are two types of keywords you can use depending on the type of conditions you need to set up:

* Boolean keywords, either on or off, to enable switching between two shader branches.
* Enum keywords, with two or more states, to enable switching between two or more shader branches.

## Keyword impact optimization

Each keyword you add in your shader graph implies the addition of one or more branches in the graph. When you add multiple keywords, the number of branch combinations for a single shader graph increases extremely fast, which might result in significant performance impact at build time.

**Note**: This version of Shader Graph doesn't support dynamic branching for runtime optimization.

For more information about keyword declarations and behaviors, refer to [How Unity compiles branching shaders](https://docs.unity3d.com/Manual/shader-conditionals-choose-a-type.html).

### Keyword Definition

Each keyword has its own [**Definition** parameter](Keywords-reference.md#common-parameters) which lets you define the keyword behavior at build time and at runtime.

### Shader variants and build time impacts

* At build time, Unity compiles multiple shader variants according to the number of branches and keywords you define in your shader graph. 
* At runtime, Unity sends the GPU the shader variant that matches the evaluation results.

**Note**: If you use many keywords, it negatively affects the build time as it requires Unity to generate millions or trillions of shader variants.

By default, Unity sets the keyword's [**Definition** parameter](Keywords-reference.md#common-parameters) to **Shader Feature** to only compile shader variants you use in your build and reduce build time. If you need all the shader variants in your build, for example because you need to change shader branches at runtime, use **Multi Compile** instead. However, this might significantly increase build time and file sizes.

> [!NOTE]
> When Unity strips out a variant in the build process, any attempt to use this variant at runtime results in a pink shader error.

## Additional resources

* [Manage keywords in Shader Graph](Keywords-manage.md)
* [Keyword parameter reference](Keywords-reference.md)
