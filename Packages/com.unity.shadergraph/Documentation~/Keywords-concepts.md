# Introduction to keywords in Shader Graph

Keywords enable you to create shader variants that address multiple contexts, for example:

* Features that you can turn on or off for each material instance.
* Features that behave differently on certain platforms.
* Shaders that scale in complexity based on conditions you set.

For more information about keywords and how they affect the final shader, refer to [Changing how shaders work using keywords](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html).

## Keyword settings and shader variants

Unity compiles shader variants according to the branches and keywords you defined in your Shader Graph. If you declare many different keywords, you can end up with millions or trillions of shader variants.

However, Unity can determine at build time which shader variants the project uses at runtime. To optimize build time and runtime memory usage, Unity can strip unused variants based on the keyword settings. Refer to [Common parameters](Keywords-reference.md#common-parameters) for more details.

> [!NOTE] When Unity strips out a variant in the build process, any attempt to use this variant at runtime results in a pink shader error.

## Conditions and keyword types

There are three types of keywords you can use depending on the type of conditions you need to set up:

| Keyword type | Description |
| :--- | :--- |
| **Boolean** | Boolean keywords are either on or off, which results in two shader variants. |
| **Enum** | Enum keywords can have two or more states according to an entry list you define, which results in two or more shader variants. |
| **Built-in** | Built-in keywords are always of either the Boolean or Enum type, but they behave slightly differently from Boolean or Enum keywords that you create. The Unity Editor or active Render Pipeline sets their values, and you cannot edit these. |


## Additional resources

* [Manage keywords in Shader Graph](Keywords-manage.md)
* [Keyword parameter reference](Keywords-reference.md)
