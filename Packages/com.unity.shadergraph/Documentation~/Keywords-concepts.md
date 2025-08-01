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

Each keyword you add in your shader graph implies the addition of one or more branches in the graph. When you add multiple keywords, the number of branch combinations for a single shader graph increases extremely fast, which might result in significant performance impact at build time or at runtime.

To minimize or balance performance impacts according to your project needs and your production context, you can define how Unity addresses the underlying behavior of each keyword at build time and at runtime. This mostly involves the use of either [dynamic branching](#dynamic-branching) or [shader variants](#shader-variants).

For example, you might consider setting your keywords for dynamic branching during development, to speed up build and iteration time, and then set them to shader variants toward the end of the project to improve runtime performance.

For more information about keyword declarations and behaviors, refer to [How Unity compiles branching shaders](https://docs.unity3d.com/Manual/shader-conditionals-choose-a-type.html).

### Keyword Definition

Each keyword has its own [**Definition** parameter](Keywords-reference.md#common-parameters) which lets you define the keyword behavior at build time and at runtime.

<a name="dynamic-branching"></a>

### Dynamic branching and build time optimization

When you set up a keyword in a dynamic branching configuration, Unity does the following:

* At build time, Unity compiles a single shader program that includes all the branching code.
* At runtime, Unity sends all keyword states to the GPU for each draw call.

The impacts are the following:

* It keeps build times low and file sizes small.
* If your code has a lot of condition checks and complex branches, it might negatively impact GPU performance.

To configure a keyword for dynamic branching, set its [**Definition** parameter](Keywords-reference.md#common-parameters) to **Dynamic Branch**.

<a name="shader-variants"></a>

### Shader variants and runtime optimization

When you set up a keyword in a shader variant configuration:

* At build time, Unity compiles multiple shader variants according to the number of branches and keywords you define in your shader graph. 
* At runtime, Unity sends the GPU the shader variant that matches the evaluation results.

The impacts are the following:

* It improves GPU performance at runtime compared to a dynamic branching configuration.
* It mostly negatively affects the build time, especially if you use many keywords, which might require Unity to generate millions or trillions of shader variants.

By default, Unity sets the keyword's [**Definition** parameter](Keywords-reference.md#common-parameters) to **Shader Feature** to only compile shader variants you use in your build and reduce build time. If you need all the shader variants in your build, for example because you need to change shader branches at runtime, use **Multi Compile** instead. However, this might significantly increase build time and file sizes.

> [!NOTE]
> When Unity strips out a variant in the build process, any attempt to use this variant at runtime results in a pink shader error.

## Additional resources

* [Manage keywords in Shader Graph](Keywords-manage.md)
* [Keyword parameter reference](Keywords-reference.md)
