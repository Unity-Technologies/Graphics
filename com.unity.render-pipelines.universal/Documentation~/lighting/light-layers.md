# Light Layers

The Light Layers feature lets you configure certain Lights to affect only specific GameObjects.

For example, in the following illustration, Light `A` affects Sphere `D`, but not Sphere `C`. Light `B` affects Sphere `C`, but not Sphere `D`.

![Light A affects Sphere D, but not Sphere C. Light B affects Sphere C, but not Sphere D.](../Images/lighting/light-layers/light-layers-example.png)

To read how to implement this example, see section [How to use Light Layers](#how-to-light-layers).

## <a name="enable"></a>How to enable Light Layers

To enable Light Layers in your project:

1. In the [URP Asset](../universalrp-asset.md), in the **Lighting** section, click the vertical ellipsis icon (&vellip;) and select **Show Additional Properties**

    ![Show Additional Properties](../Images/settings-general/show-additional-properties.png)

2. In the [URP Asset](../universalrp-asset.md), in the **Lighting** section, select **Light Layers**.

    ![URP Asset > Lighting > Light Layers](../Images/lighting/light-layers/light-layers-enable.png)<br/>*URP Asset > Lighting > Light Layers*

When you enable Light Layers, Unity shows the following extra properties on each Light:

* General > Light Layer

    ![](../Images/lighting/light-layers/light-layers-prop-light-layer.png)

* Shadows > Custom Shadow Layers

    ![](../Images/lighting/light-layers/light-layers-prop-shadow-layers.png)

Enabling Light Layers disables the **Culling Mask** property on Lights (Rendering > Culling Mask).

![](../Images/lighting/light-layers//light-layers-prop-culling-mask-disabled.png)<br/>*Unity disables Culling Mask when you enable Light Layers.*

## How to edit Light Layer names

To edit the names of Light Layers:

1. Go to **Project Settings** > **Graphics** > **URP Global Settings**.

2. Edit the Light Layer names in the **Light Layer Names (3D)** section.

    ![Graphics > URP Global Settings > Light Layer Names (3D)](../Images/Inspectors/global-settings.png)<br/>*Graphics > URP Global Settings > Light Layer Names (3D)*

## <a name="how-to-light-layers"></a>How to use Light Layers

This section describes how to configure the following application example:

* The Scene contains two Point Lights (marked `A` and `B` in the illustration) and two Sphere GameObjects (`C` and `D` in the illustration).

* Light `A` affects Sphere `D`, but not Sphere `C`. Light `B` affects Sphere `C`, but not Sphere `D`.

The following illustration shows the example:

![Light A affects Sphere D, but not Sphere C. Light B affects Sphere C, but not Sphere D.](../Images/lighting/light-layers/light-layers-example.png)<br/>*Light `A` affects Sphere `D`, but not Sphere `C`. Light `B` affects Sphere `C`, but not Sphere `D`.*

To implement the example:

1. [Enable Light Layers](#enable) in your project.

2. Create two Point Lights (call them `A`, and `B`) and two Spheres (call them `C`, and `D`). Position the objects so that both Spheres are within the emission range of Lights.

3. Go to **Project Settings > Graphics > URP Global Settings**. Rename Light Layer 1 to `Red`, and Light Layer 2 to `Green`.

    ![URP Global Settings](../Images/lighting/light-layers/light-layers-urp-global-settings.png)

4. Select Light `A`, change its color to green. Select Light `B`, change its color to red. With this setup, both Lights affect both Spheres.

    ![Both Lights affect both Spheres.](../Images/lighting/light-layers/both-lights.png)

5. Make the following settings on Lights and Spheres:

    Light `A`: in the property **Light > General > Light Layer**, clear all options, and select `Green`.

    Light `B`: in the property **Light > General > Light Layer**, clear all options, and select `Red`.

    Sphere `C`: in the property **Mesh Renderer > Additional Settings > Rendering Layer Mask**, select all options, clear `Green`.

    Sphere `D`: in the property **Mesh Renderer > Additional Settings > Rendering Layer Mask**, select all options, clear `Red`.

    Now Point Light `A` affects Sphere `D`, but not Sphere `C`. Point Light `B` affects Sphere `C`, but not Sphere `D`.

    ![Point Light A affects Sphere D, but not Sphere C. Point Light B affects Sphere C, but not Sphere D.](../Images/lighting/light-layers/light-layers-example.png)

## <a name="shadow-layers"></a>How to use Custom Shadow Layers

In the illustration above, Light `A` does not affect Sphere `C`, and the Sphere does not cast shadow from Light `A`.

The **Custom Shadow Layers** property lets you configure the Scene so that Sphere `C` casts the shadow from Light `A`.

1. Select Light `A`.

2. In **Light > Shadows**, select the **Custom Shadow Layers** property. Unity shows the **Layer** property.

3. In the **Layer** property, select the Light Layer that Sphere C belongs to.

Now Light `A` does not affect Sphere `C`, but Sphere `C` casts shadow from Light `A`.

The following illustrations show the Scene with the **Custom Shadow Layers** property off and on.

![Custom Shadow Layers property off](../Images/lighting/light-layers/custom-shadow-layers-off.png)

![Custom Shadow Layers property on](../Images/lighting/light-layers/custom-shadow-layers-on.png)
