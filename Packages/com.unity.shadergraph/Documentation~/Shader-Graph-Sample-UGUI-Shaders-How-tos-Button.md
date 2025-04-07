# How to create a functioning button
Buttons have multiple states depending on the user’s interaction with them. The button may change its appearance when the user’s mouse is hovering over the button. And when it’s pressed, the button may also change to indicate the press.  That means that the shader needs to create the various visual styles and expose parameters to trigger them.  You’ll then need to connect those exposed parameters to the button’s events. Follow these steps to learn how to do that.

1. Create a new Shader Graph asset. 
1. In the Graph Inspector, make sure that the Shader Graph’s Material setting is set to **Canvas**.
1. Open the shader’s Blackboard and add two boolean parameters.  
1. Name the parameters **Selected** and **Pressed**.
1. Drag these parameters into your graph and use them to create a shader that changes depending on if the values are true or false.  The SimpleButton Shader Graph asset is a good example of this.  Find it here: `Assets/Samples/Shader Graph/<your version>/UGUI Shaders/Examples/Buttons/SimpleButton`
1. In your scene, create a new Canvas element (if there isn’t one already) in the **Hierarchy** panel. (Right click in the Hierarchy panel and select **UI** > **Canvas**). 
1. Add a Button element to the Canvas. (Right click on the Canvas element and select **UI** > **Button - TextMeshPro**) Then select the Button element.
1. Select the material associated with your shader in the Project panel and drag and drop it into the Material slot in the Inspector for the Image UI element. Now your shader’s material is assigned to the UI element. 
1. Set the button’s **Source Image** and **Transition** to None. The shader provides these.
- Now click the **Add Component** button at the bottom of the Inspector and add the script called **UI Material**.  This script exposes the Selected and Pressed parameters so they can be used by other scripts.
1. Click **Add Component** again and add the script called **ButtonMaterial**.  This script connects the material’s exposed button parameters to the button’s events.

Now you have a button that correctly passes information to its material when the mouse pointer hovers over the button, presses down on the button, stops pressing the button, and moves off of the button.  You can set up the shader to respond to these events visually in whatever way makes sense for the style.  For example, when the button is pressed, you might move the button down slightly, invert its color, or get darker. When the mouse is hovering over the button, you might make an outline appear around it, or turn on an animated sheen effect.