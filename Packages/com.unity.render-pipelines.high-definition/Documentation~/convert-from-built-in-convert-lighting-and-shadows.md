# Convert lighting and shadows

HDRP uses [physical Light units](Physical-Light-Units.md) to control the intensity of Lights. These units don't match the arbitrary units that the Built-in render pipeline uses.

For light intensity units, Directional Lights use [Lux](Physical-Light-Units.md#Lux) and all other Light types can use [Lumen](Physical-Light-Units.md#Lumen), [Candela](Physical-Light-Units.md#Candela), [EV](Physical-Light-Units.md#EV), or simulate Lux at a certain distance.

To set up lighting in your HDRP Project:

1. To add the default sky [Volume](understand-volumes.md) to your Scene and set up ambient lighting go to **GameObject** > **Volume** > **Sky and Fog Global Volume**.
2. Set the [Environment Lighting](Environment-Lighting.md) to use this new sky:

    1. Open the Lighting window (menu: **Window** > **Rendering** > **Lighting Settings**).
    2. In the **Environment** tab, set the **Profile** property to the same [Volume Profile](create-a-volume-profile.md) that the Sky and Fog Global Volume uses.
    3. Set the **Static Lighting Sky** property to **PhysicallyBasedSky**.
    4. Optionally, if you don't want Unity to re-bake the Scene's lighting when you make the rest of the changes in this section, you can disable the **Auto Generate** checkbox at the bottom of the window.

3. Currently, the shadows are low quality. To increase the shadow quality:

    1. Create a new **Global Volume** GameObject (menu: **GameObject** > **Volume** > **Global Volume**) and name it **Global Settings**.
    2. To create a new Volume Profile for this Global Volume:

        1. Open the Global Volume's Inspector window and go to the **Volume** component.
        2. Go to **Profile** and select **New**.

    3. To add a **Shadows** override:

        1. Go to **Add Override** > **Shadowing** > **Shadows**.
        2. Enable **Max Distance**.
        3. Set **Max Distance** to 50.

4. Configure your Sun Light GameObject.

    1. Select your Light GameObject that represents the Sun in your Scene to view it in the Inspector.
    2. Go to **Emmision** and set the **Intensity** to **100000**.
    3. Set **Light Appearance** to **Color**.
    4. Set **Color** to white.
    5. To see the sun in the sky, go to **Shape** and set **Angular Diameter** to **3**.

5. The Scene is now over-exposed. To fix this:

    1. Select the **Global Settings** GameObject you created in step **3**.
    2. Add an **Exposure** override to its Volume component (menu: **Add Override** > **Exposure**).
    3. Enable **Mode** and set it to **Automatic**.
    4. To refresh the exposure, go to the Scene view and enable **Always Refresh**.

    ![](Images/UpgradingToHDRP2.png)

7. HDRP supports colored light cookies, whereas the Built-in light cookies use a texture format that only contains alpha. To correct the Light cookie:

    1. In the Project window, select your Light cookie from your **Assets** folder.
    2. In the Inspector, change the import type from **Cookie** to **Default**.
    3. Set the **Wrap Mode** to **Clamp**.

8. Correct spot lights:

    1. In the Hierarchy, select a Spot Light and view the Light component in the Inspector.
    2. Go to **Emission** and set **Intensity** to 17000 **Lumen** to represent two 8500 Lumen light bulbs.
    3. In the **Emission**, select the More menu (&#8942;) and enable [additional properties](expose-all-additional-properties.md).
    4. Enable **Reflector** checkbox. This simulates a reflective surface behind the spot Light to adjust the visual intensity.

9. Make the light bulb Material emissive:

    1. In the Project window, select **Assets/ExampleAssets/Materials/Lightbulb_Mat.mat**.
    2. In the Inspector, go to **Emission Inputs** and enable **Use Emission Intensity**.
    3. Set **Emissive Color** to white, and set the **Emission Intensity** to **8500 Luminance**.

10. If you disabled **Auto Generate** in step **2**, go to the Lighting window and press the **Generate Lighting** button. You can also re-enable **Auto Generate**.
