# Visual Effect Graph Assets

A Visual Effect Graph Asset is a data container that the Visual Effect Graph uses the play a visual effect. It includes:

* Graph elements
* Exposed properties
* Compiled Shaders
* Operator bytecode

## Creating Visual Effect Graph Assets

To create a Visual Effect Graph Asset, select **Assets > Create > Visual Effects > Visual Effect Graph**. Unity creates a new Visual Effect in the currently opened folder of your Project window.

## Using Visual Effect Graph Assets

To edit a Visual Effect Graph Asset, first open it in the [Visual Effect Graph window](VisualEffectGraphWindow.md). To do this, you can either:

* Double-Click the Visual Effect Graph Asset in the Project window.
* Select the Visual Effect Graph Asset in the Project window to view it in the Inspector then, in the Asset's header, click the **Open** button.
* In the Inspector for a [Visual Effect component](VisualEffectComponent.md#the-visual-effect-inspector), click the **Edit** button next to the **Asset Template** property. This opens the assigned Visual Effect Graph Asset.

With the Visual Effect Graph open, you can now edit the Visual Effect.

#### Visual Effect Asset Inspector

When you select a Visual Effect Graph Asset, the Inspector displays Asset-wide configuration Options.

![](Images/VisualEffectAssetInspector.png)

| Property Name           | Description / Values                                         |
| ----------------------- | ------------------------------------------------------------ |
| **Update Mode**         | Sets the rate at which Unity updates the visual effect:<br /> &#8226;**Fixed Delta Time**: Updates the visual effect at the rate that the **Fixed Time Step** property defines in the [Visual Effect Project Settings](VisualEffectProjectSettings.md).<br />&#8226; **Delta Time**: Updates the visual effect every frame. |
| **Culling Flags**       | Sets whether Unity updates the visual effect depending on its culling state. The culling state refers to whether a Camera can see the visual effect's bounding box or not. The options are:<br />&#8226; **Recompute bounds and simulate when visible**: Unity simulates the effect and recalculates the effect's bounding box when the effect is visible. If your visual effect uses a dynamic bounding box (one that you compute with operators), you should not use this option in favor of one that includes **Always Recompute Bounds** .<br /> &#8226; **Always Recompute Bounds, simulate only when Visible**: Regardless of whether any Camera can see the effect's bounding box or not, Unity always recalculates the bounding box. Unity only simulates the effect if a Camera can see the updated bounds.<br />&#8226; **Always Recompute Bounds and Simulate**:  Regardless of whether any Camera can see the effect's bounding box or not, Unity always recalculates the bounding box and simulates the effect.<br /><br />**Note**: Regardless of the mode, Unity always uses the bounding box to perform culling of the effect. |
| **PreWarm Total Time**  | Sets the duration that Unity should simulate the effect when `Reset()` occurs. This pre-simulates the effect so that, when the effect starts, it appears already 'built-up'. When you change this value, Unity calculates a new value for **PreWarm Delta Time**. |
| **PreWarm Step Count**  | Sets the number of simulation steps that Unity uses to calculate the PreWarm. A greater number of steps increase precision as well as the resource intensity of the effect, which decreases performance. When you change this value, Unity calculates a new value for **PreWarm Delta Time**. |
| **PreWarm Delta Time**  | Sets the delta time  that Unity uses for the PreWarm. When you change this value, Unity  calculates new values for **PreWarm Total Time** and **PreWarm Step Count**. Adjust this value, instead of **PreWarm Total Time** and **PreWarm Step Count** individually, if you need to use a precise delta time for your simulation. |
| **Initial Event Name**  | Sets the name of the [Event](Events.md) that Unity sends when the effect enables. The default value is **OnPlay**, but you can change this to another name, or even a blank field, to make it so that every system does not spawn by default. |
| **Output Render Order** | Defines a list that shows every Output Context in their rendering order. You can re-order this list to change the order that Unity renders the Output Contexts. Unity draws items at the top of the list first then progressively draws those lower down the list in front of those above. |
| **Shaders**             | Defines a list of every Shader that Unity has compiled for the Visual Effect Graph. These are read-only and mainly for debugging purposes. Use **Shader Externalization** in [Visual Effect Preferences](VisualEffectPreferences.md) to externalize Shaders temporarily for debugging purposes. |
