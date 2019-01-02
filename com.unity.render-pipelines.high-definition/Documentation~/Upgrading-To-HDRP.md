# Unity Legacy to High Definition Render Pipeline conversion tutorial

The HDRP uses a new set of [Shaders](https://docs.unity3d.com/Manual/class-Shader.html) and new lighting units, both of which are incompatible with the built-in Unity rendering pipeline. To upgrade a Unity Project to HDRP, you must first [convert](#MaterialConversion) all of the [Materials](https://docs.unity3d.com/Manual/class-Material.html) and Shaders in your Project, and then [edit](#LightAdjustment) your individual [Light](https://docs.unity3d.com/Manual/class-Light.html) settings accordingly. 

This tutorial explains the HDRP upgrade process using a sample [Scene](https://docs.unity3d.com/Manual/CreatingScenes.html) containing Assets from Unity’s [Viking Village Asset package](https://assetstore.unity.com/packages/essentials/tutorial-projects/viking-village-29140). To get the Scene used in this example, download this [LegacyScene package](https://drive.google.com/open?id=1TQN3XotIBI_xHlq-jdbBm09iDd2tHTam).

![](Images/UpgradingToHDRP1Main.png)
<a name="ImportingHDRP"></a>
## Setting up the High Definition Render Pipeline (HDRP)


First, add the HDRP package to your existing Project: 

1. In the Unity Editor, open the Package Manager window (menu: __Window &gt; Package Manager__).

2. In the menu at the top of the window, change the package list, located in the top left, to __All packages__. Then, select __Advanced &gt; Show Preview Packages__. Now, find and select the __Render-Pipelines.High-Definition__ package, and click __Install__.

Next, create and set up a High Definition Render Pipeline Asset.

1. Create an HDRP Asset by selecting __Assets &gt; Create &gt; Rendering &gt; High Definition Render Pipeline Asset__.

2. Open the __Graphics Settings__ window (menu: __Edit &gt; Project Settings &gt; Graphics)__.

3. Assign the High Definition Render Pipeline Asset to the __Scriptable Render Pipelines Settings__ field, at the top of the window. To do this, click the radio button and select the Asset from the list, or drag the Asset into the field. 

After installing the HDRP package and assigning the HDRP Asset, your Scene will not render correctly (see screenshot below). This is because the Scene still uses the built-in Shaders. The following section shows you how to upgrade these built-in Shaders to an HDRP-compatible shader . 

![](Images/UpgradingToHDRP2.png)

<a name="MaterialConversion"></a>
### Upgrading Materials

To upgrade the Materials in your Scene to HDRP-compatible Materials, navigate to __Edit &gt; Render Pipeline__ and select one of the following options: 

* __Upgrade Project Materials to High Definition Materials__: Converts all compatible Materials in your Project to HDRP Materials.

* __Upgrade Selected Materials to High Definition Materials__: Converts Materials that are currently selected in the Project window. 

If your Project contains any custom Materials or Shaders then this script will not automatically update them to HDRP. You must [convert these Materials and Shaders manually](#ManualConversion). 

<a name="LightAdjustment"></a>
### Adjusting Lights

Firstly, you need to change your Color Space to __Linear__. Navigate to __Edit &gt; Settings &gt; Player__ and, in the __Other Settings__ section, set the __Color Space__ to __Linear__.

The HDRP uses physical Light units to control the intensity of Lights. This means that these units will not match the arbitrary units that the built-in render pipeline uses.

Directional Light intensities are expressed in [Lux]([https://en.wikipedia.org/wiki/Lux](https://en.wikipedia.org/wiki/Lux)) and other Light intensities are expressed in [Lumen]([https://en.wikipedia.org/wiki/Lumen_(unit)](https://en.wikipedia.org/wiki/Lumen_(unit))).

So, in the case of the example Scene, start by adding a Directional Light to represent the main, natural light in this Scene: the Moon. A full Moon, on a clear night sky, has a luminous flux of around 0.25 Lux.

Disable all other Lights in the Scene to exclusively see the effect of the Light representing the Moon. 

![](Images/UpgradingToHDRP3.png)

HDRP handles the Sky in a differently to the built-in render pipeline, to allow you to alter Sky parameters dynamically at run time using the __Volume__ script.

Select __GameObject &gt; Rendering &gt; Scene Settings__ and adjust the following settings for best effect:

* __HD Shadow Settings__ : The maximum shadow distance and the directional shadow cascade settings.

* __Visual Environment__ : The Sky and Fog type of your Scene.

* __Procedural Sky__ : This is a port of the legacy procedural Sky and contains the same settings.

* __Exponential Fog__ : The default Fog, that can handle fields such as __Density__, __Color Mode__, __Fog Distance__, and __Fog Height__.

Additionally, the [GameObject](https://docs.unity3d.com/Manual/class-GameObject.html) also has a Baking Sky component, that references the Volume’s procedural Sky. This component passes the Sky data to the lightmapper and only one should ever be present in the Scene at any time. Otherwise, Unity will exclusively use the first loaded Baking Sky component, and a warning in shown in the console.

Below are the values used for this example’s Procedural Sky: 

![](Images/UpgradingToHDRP4.png)

The Procedural Sky’s light intensity is expressed as __Exposure__ and __Multiplier__. To convert to Lux, set exposure to 0 Exposure Value (EV) and use the Multiplier as the Lux value. To create believable visuals in this example, set the Multiplier to 0.02 Lux. You can increase the value to 0.05 to make the Scene more visible, while still being low enough to be plausible.

At this point, you can __Generate Lighting__ in this Scene to create light bounces and directional soft shadows. Go to __Window &gt; Rendering &gt; Lighting Settings__ and, near the bottom of the __Scene__ tab, click __Generate Lighting__.![](Images/UpgradingToHDRP5.png)

Fire-lit torches are usually around 100 to 140 Lumen so set the __Intensity__ of the Point Lights in the Sene to somewhere between these two values, and make sure you set their __Mode__ to __Baked__. Baked lighting allows you to use smooth shadows baked into lightmaps.

<a name="CookieCorrection"></a>

You’ll also notice that the Light Cookie no longer works. That’s because HDRP uses standard textures as Light Cookies, and handles colored cookies. Simply change the cookie texture (named "TorchCookie") import settings to these:

* __Texture Type__ to __default__

* __Texture Shape__ to __Cube__

* __Mapping__ to __Latitude-Longitude Layout (Cylindrical)__

* Disable __sRGB (Color Texture)__

* __Alpha Source__ to __None__

* Disable __Border Mip Maps__

* __Wrap Mode__ to __Clamp__

![](Images/UpgradingToHDRP6.png)

Click __Generate Lighting__ again. The Scene now looks like this:

![](Images/UpgradingToHDRP7.png) 

Press the __Play__ button and you will see the following in your Game window:

![](Images/UpgradingToHDRP8.png)

If you compare this to the legacy screenshot, the changes may not be obvious. This is because this example does not use any of the new HDRP-specific Material features such as anisotropy, subsurface scattering, or parallax occlusion mapping, and the original Materials were already PBR compliant.

## Converting the template Scene

The __3D With Extras__ template Scene is another Scene that is interesting to test the conversion process on. You can get the Project by opening [Unity Hub](https://unity3d.com/get-unity/download), creating a new Project, and selecting __3D With Extras__ from the Template drop-down. After you create and open the Project, you will see the following Scene:

![](Images/UpgradingToHDRP9.png)

Like with the previous conversion example, [import the HDRP package](#ImportingHDRP) (menu: __Window &gt; Package Manager__), and run the converter (__Edit &gt; Render Pipeline__).

You must modify some of the GameObjects in the Scene so they behave correctly:

1. Add an __Auto Exposure__ effect to the Post Process Volume script attached to the __Post-process Volume__ GameObject (__Add effect &gt; Unity &gt; Auto Exposure__). 

2. Enable the Minimum (EV), Maximum (EV) and Exposure Compensation settings and then set them to the following values: 

![](Images/UpgradingToHDRP10.png)

This accommodates the high difference in light exposition values (Min and Max) and the overall high exposure.

3. The conversion process may have altered the size of the Reflection Probes. If so, alter the __Box Size__ field for each Reflection Probe until they match the size of the area they are in.

4. Set the __Intensity__ of the Light representing the Sun (the Light attached to the __Directional Light__ GameObject in the Scene) to 100000.

5. Create a Scene Settings GameObject (__GameObject &gt; Rendering &gt; Scene Settings__) and set the sky __Exposure__ to 0 and the __Multiplier__ to 20000.

6. Set the intensity of the Light attached to the __Spot Light__ GameObject to 17000. This is because there are two 8500 lumen lamps. Enable __Angle Affect Intensity__ to compensate for the spot angle and its reflector.

7. Correct the Light cookie (Spotlight_Cookie), as shown in the [cookie correction section](#CookieCorrection) in the previous example.

8. Set the emissive intensity of the light bulb Material (LightBulb_Mat) to 13.05. Click on the __Emissive Color__ picker and manually enter 13.05 into the __Intensity __field. This value is much lower than the others because the emission color intensity uses EV units and the other values use Lumen. To convert between the two, see the following formulas. Exposure Value is a scale of powers of 2. 
This means that `x EV = 2 ^ x Lumen` and `y Lumen is ln( y EV ) / ln( 2 )`

For example, `13.05 EV is: 2 ^ 13.05 = 8480 Lumen` therefore `8480 Lumen is: ln( 8480 ) / ln( 2 ) = 13.05 EV`

9. Click  __Generate Lighting__ to re-bake the lighting.

You should now have a Scene similar to this:

![](Images/UpgradingToHDRP11.png)

Note how the lighting is different from the original screenshot, and from the original HDRP template. The HDRP template examples were made to look good but were not realistic, whereas this Scene uses physically correct light values: an afternoon direct sun with no clouds in the sky is much brighter than even the best professional construction spotlight. However, the spotlight is still casting light and shadows on the side of the wall.
<a name="ManualConversion"></a>
## Converting Materials manually

The HDRP Material converter automatically converts Legacy Standards and Unlit Materials to HDRP Lit and Unlit Materials respectively. This section describes the steps the converter takes, to help you convert custom Materials manually.

### The mask map

The Legacy Standard to Lit conversion process combines the different Material maps of the Legacy Standard into the separate RGBA channels of the mask map in the HDRP Lit Material.

* Metallic goes in the Red channel

* Occlusion goes in the Green channel

* Detail Mask goes in the Blue channel 

* Smoothness goes in the Alpha channel

 ![](Images/UpgradingToHDRP12.png)

### The detail map

The Legacy Standard to Lit conversion process combines the different detail maps of the Legacy Standard into the separate RGBA channels of the detail map in the HDRP Lit Material. It also adds a smoothness detail too.

* Albedo is desaturated and goes in the Red channel

* Normal Y goes in the Green channel

* Smoothness goes in the Blue channel

* Normal X goes in the Alpha channel

![](Images/UpgradingToHDRP13.png)

The process blends detail albedo and smoothness with the base values using an overlay function, similar to the process you would use in image editing software, like Photoshop.

