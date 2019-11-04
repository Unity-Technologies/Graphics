# Built-in renderer to High Definition Render Pipeline conversion tutorial

The High Definition Render Pipeline (HDRP) uses a new set of [Shaders](https://docs.unity3d.com/Manual/class-Shader.html) and new lighting units, both of which are incompatible with the built-in Unity rendering pipeline. To upgrade a Unity Project to HDRP, you must first [convert](#MaterialConversion) all of the [Materials](https://docs.unity3d.com/Manual/class-Material.html) and Shaders in your Project, and then [edit](#LightAdjustment) your individual Light settings accordingly. 

This tutorial explains the HDRP upgrade process using a sample [Scene](https://docs.unity3d.com/Manual/CreatingScenes.html) containing Assets from Unity’s [Viking Village Asset package](https://assetstore.unity.com/packages/essentials/tutorial-projects/viking-village-29140). To get the Scene that the example uses, download the [LegacyScene package](https://drive.google.com/open?id=1TQN3XotIBI_xHlq-jdbBm09iDd2tHTam) and add it to a Unity Project. To open the example Scene, in the Project window, go to **Assets > Legacy > _Scene** and double-click the **LegacyScene** Scene file.

![](Images/UpgradingToHDRP1Main.png)
<a name="ImportingHDRP"></a>

## Setting up HDRP


First, add the HDRP package to your existing Project: 

1. In the Unity Editor, open the Package Manager window (menu: **Window &gt; Package Manager**).

2. In the menu at the top of the window, change the package list, located in the top left, to **All packages**.  Now, find and select the **High Definition RP** package, and click **Install**.

Next, open the [Render Pipeline Wizard](Render-Pipeline-Wizard.html) (menu: **Window > Render Pipeline > HD Render Pipeline Wizard**). You can use the Render Pipeline Wizard to upgrade your Project, as well as upgrade every Material that uses Shaders from the built-in render pipeline. You can also use it to set up Virtual Reality or DXR (ray tracing) support. To set up base HDRP:

1. Go to the **HDRP** tab and click **Fix All**.
2. Whenever a pop-up appears asking you about selecting or creating Assets, click **Create One**.

The Scene now does not render correctly:

![](Images/UpgradingToHDRP2.png)

This is because the Scene still uses the built-in Shaders. To upgrade these built-in Shaders to an HDRP-compatible Shader click **Upgrade Project Materials to High Definition Materials** in the Render Pipeline Wizard window.

If your Project contains any custom Materials or Shaders then this script can not automatically update them to HDRP. You must [convert these Materials and Shaders manually](#ManualConversion).

<a name="LightAdjustment"></a>

### Adjusting Lights

HDRP uses [physical Light units](Physical-Light-Units.html) to control the intensity of Lights. These units do not match the arbitrary units that the built-in render pipeline uses. Directional Light intensities are expressed in [Lux](https://en.wikipedia.org/wiki/Lux)) and other Light intensities are expressed in [Lumen](https://en.wikipedia.org/wiki/Lumen_(unit)).

So, in the case of the example Scene, start by adding a Directional Light to represent the main, natural light in this Scene: the Moon. A full Moon, on a clear night sky, has a luminous flux of around 0.25 Lux.

To exclusively see the effect of the Light representing the moon, delete the **Fill_Directional** GameObject and disable all other Lights in the Scene.

Note: If your Scene does not render correctly at this point, you must  remove a legacy component from the **Main Camera**. To do this:

1. Select **Main Camera** in the Hierarchy to view it in the Inspector.
2. Remove the **Scene Render Pipeline** script. 

![](Images/UpgradingToHDRP3.png)

HDRP handles the Sky differently to the built-in render pipeline, to allow you to use [Volumes](Volumes.html) to alter Sky properties dynamically at run time.

To add a global Volume which already includes sky and fog overrides, select **GameObject &gt; Volume &gt; Sky and Fog Volume**.

This adds a GameObject to your Scene called **Sky and Fog Volume**. Select this GameObject in the Hierarchy to view it in the Inspector and see the list of Volume overrides that it contains. There are three:

* [Visual Environment](Override-Visual-Environment.html).

* [Physically Based Sky](Override-Physically-Based-Sky).
* [Fog](Override-Fog.html).

#### Lighting 
The Physically Based Sky’s light intensity is expressed as **Exposure** (units are EV) and **Multiplier**. To convert to Lux, set exposure to 0 Exposure Value (EV) and use the Multiplier as the Lux value. To create believable visuals in this example, set the Multiplier to 0.02 Lux. You can increase the value to 0.05 to make the Scene more visible, while still being low enough to be plausible.

At this point, you can **Generate Lighting** in this Scene to create light bounces and directional soft shadows. To do this, go to **Window &gt; Rendering &gt; Lighting Settings** and, near the bottom of the **Scene** tab, click **Generate Lighting**.

For information on environment lighting, specifically baked lighting and global illumination, see [Environment Lighting](Environment-Lighting.html#LightingEnvironment).

![](Images/UpgradingToHDRP5.png)

<a name="CookieCorrection"></a>

You may notice that the Light Cookie no longer works. That’s because HDRP uses standard Textures as Light Cookies, and handles colored cookies. Simply change the cookie texture (named "TorchCookie") import settings to match the following:

* **Texture Type** to **default**

* **Texture Shape** to **Cube**

* **Mapping** to **Latitude-Longitude Layout (Cylindrical)**

* Disable **sRGB (Color Texture)**

* **Alpha Source** to **None**

* Disable **Border Mip Maps**

* **Wrap Mode** to **Clamp**

![](Images/UpgradingToHDRP6.png)

Fire-lit torches are usually around 100 to 140 Lumen so set the **Intensity** of the Point Lights in the Sene to somewhere between these two values, and make sure you set their **Mode** to **Baked**. Baked lighting allows you to use smooth shadows baked into lightmaps.

Click **Generate Lighting** again. The Scene now looks like this:

![](Images/UpgradingToHDRP7.png) 

Press the **Play** button and you will see the following in your Game window:

![](Images/UpgradingToHDRP8.png)

If you compare this to the legacy screenshot, the changes may not be obvious. This is because this example does not use any of the new HDRP-specific Material features such as anisotropy, subsurface scattering, or parallax occlusion mapping, and the original Materials were already PBR compliant.

## Converting the template Scene

The **3D With Extras** template Scene is another Scene that is interesting to test the conversion process on. You can get the Project by opening [Unity Hub](https://unity3d.com/get-unity/download), creating a new Project, and selecting **3D With Extras** from the Template drop-down. After you create and open the Project, you will see the following Scene:

![](Images/UpgradingToHDRP9.png)

Like with the previous conversion example, [import the HDRP package](#ImportingHDRP) (menu: **Window &gt; Package Manager**) and open the Render Pipeline Wizard, to upgrade the Project and the Materials. Then add the **Sky and Fog Volume**.

HDRP has its own integrated [post-processing solution](Post-Processing-Main.html) that uses the Volumes in HDRP. This means that you must remove the built-in post-processing components. To do this:

* Delete the GameObject called **Post-process Volume**.
* Remove the Post-Processing Layer component from the GameObject called **Main Camera**.

You must also modify some of the GameObjects in the Scene so they behave correctly. 

1. Add an **Exposure** override to Volume component on the **Sky and Fog Volume** GameObject.

2. Customize the override so that it has the following values: 

![](Images/UpgradingToHDRP10.png)

This accommodates the high difference in light exposition values (Min and Max) and the overall high exposure.

3. The conversion process may have altered the size of the Reflection Probes. If so, alter the **Box Size** field for each Reflection Probe until they match the size of the area they are in.

4. Set the **Intensity** of the Light representing the Sun (the Light attached to the **Directional Light** GameObject in the Scene) to 100000.

5. Create a global [Volume](Volumes.html) (menu: **GameObject > Volume > Global Volume**), next to **Profile** click the **New** button, then add one of HDRP's [sky types](HDRP-Features.html#SkyOverview). Set the sky **Exposure** to 0 and the **Multiplier** to 20000.

6. Set the intensity of the Light attached to the **Spot Light** GameObject to 17000. This is because there are two 8500 lumen lamps. Enable **Angle Affect Intensity** to compensate for the spot angle and its reflector.

7. Correct the Light cookie (Spotlight_Cookie), as shown in the [cookie correction section](#CookieCorrection) in the previous example.

8. Set the emissive intensity of the light bulb Material (LightBulb_Mat) to 13.05. Click on the **Emissive Color** picker and manually enter 13.05 into the **Intensity **field. This value is much lower than the others because the emission color intensity uses EV units and the other values use Lumen. To convert between the two, see the following formulas. Exposure Value is a scale of powers of 2. 
This means that `x EV = 2 ^ x Lumen` and `y Lumen is ln( y EV ) / ln( 2 )`

For example, `13.05 EV is: 2 ^ 13.05 = 8480 Lumen` therefore `8480 Lumen is: ln( 8480 ) / ln( 2 ) = 13.05 EV`

9. Click  **Generate Lighting** to re-bake the lighting.

You should now have a Scene similar to this:

![](Images/UpgradingToHDRP11.png)

Note how the lighting is different from the original screenshot, and from the original HDRP template. The HDRP template examples were made to look good but were not realistic, whereas this Scene uses physically correct light values: an afternoon direct sun with no clouds in the sky is much brighter than even the best professional construction spotlight. However, the spotlight is still casting light and shadows on the side of the wall.
<a name="ManualConversion"></a>

## Converting Materials manually

The HDRP Material converter automatically converts Legacy Standard and Unlit Materials to HDRP Lit and Unlit Materials respectively. The process uses an overlay function to blend the color channels together, similar to the process you would use in image editing software, like Photoshop. This section describes the steps the converter takes, to help you convert custom Materials manually. 

### The mask map

The Legacy Standard to Lit conversion process combines the different Material maps of the Legacy Standard Shader into the separate RGBA channels of the mask map in the HDRP [Lit Material](Lit-Shader.html). For information on which color channel each map goes in, see [mask map](Mask-Map-and-Detail-Map.html#MaskMap).

### The detail map

The Legacy Standard to Lit conversion process combines the different detail maps of the Legacy Standard Shader into the separate RGBA channels of the detail map in the HDRP [Lit Material](Lit-Shader.html). It also adds a smoothness detail too. For information on which color channel each map goes in, see [detail map](Mask-Map-and-Detail-Map.html#DetailMap).
