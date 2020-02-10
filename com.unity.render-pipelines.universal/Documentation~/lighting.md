# Lighting in the Universal Render Pipeline

Using the Universal Render Pipeline (URP), you can achieve realistic lighting that is suitable for a range of art styles.

All of Unity's render pipelines share common lighting functionality, but each render pipeline has some important differences.

Areas where the Universal Render Pipeline (URP) differs from Unity's common lighting functionality are:

* The [Light component inspector](light-component.md), which displays some URP-specific controls.
* The [Universal Additional Light Data](universal-additional-light-data.md) component, which allows Unity to store Light-related data that is specific to URP.
* Realtime Global Illumination using Enlighten is not supported in URP. Enlighten is deprecated, and 2020 LTS is the last version of Unity that will support any Enlighten functionality. For more information on Enlighten support, see this [Unity blog post](https://blogs.unity3d.com/2019/07/03/enlighten-will-be-replaced-with-a-robust-solution-for-baked-and-real-time-giobal-illumination/?_ga=2.246542978.783393071.1580122240-359214009.1520341967).

For a full comparison of lighting features between Unity's Built-in Render Pipeline and URP, and an up to date list of lighting features that are currently under research, see [this feature comparison chart](universalrp-builtin-feature-comparison).

For a general introduction to lighting in Unity and examples of common lighting workflows, see [the Lighting section of the Unity Manual](https://docs.unity3d.com/Manual/LightingOverview.html).
