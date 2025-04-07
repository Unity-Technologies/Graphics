# How to make shapes that adapt to the aspect ratio of the UI element
Buttons and other UI elements come in all shapes and sizes. Frequently, the same style of button or background element needs to work with many different shapes or adapt to a changing width and height. Many of the UI elements in this sample are designed to adapt to changing dimensions.  Follow these steps to create a UI element that can do that.

1. Create a new Shader Graph asset. 
1. In the Graph Inspector, set the Shader Graph’s Material setting to **Canvas**.
1. Add an SDF Pill node to the new Shader Graph file. ( You can find this node in the **Add Node** menu under **UI/SDFs**.) 
1. Connect the Pill node’s **Fill** output port to the **Base Color** input port of the **Master Stack**. Now you’ve created a shader for a UI element that’s meant to be square.  If you assign this shader to a non-square UI element, it will stretch and not look like a proper pill shape.
Notice that the Pill node’s last input port is called **WidthHeight**. This is an input for the width and height data of the UI element that the shader will be assigned to. By default, the Pill node assumes the UI element will be square, but if you plan to use a different aspect ratio, you can enter other values. Rather than entering the values manually, it’s best to connect them to the actual UI element values.  That’s what we’ll do next.
1. Open the **Blackboard** and add a new Vector2 parameter.  
1. Name the parameter **WidthHeight**.  
1. Drag the parameter into the graph and connect it to the **WidthHeight** input port on the Pill node.  You’ve now exposed the **WidthHeight** parameter so it can be connected outside the shaders.
1. In your scene, create a new **Canvas** element (if there isn’t one already) in the **Hierarchy** panel. (Right click in the **Hierarchy** panel and select **UI** > **Canvas**). 
1. Then add an Image element to the Canvas. (Right click on the **Canvas** element and select **UI** > **Image**) Select the Image element.
1. Select the material associated with your shader in the Project panel and drag and drop it into the Material slot in the Inspector for the Image UI element. Now your shader’s material is assigned to the UI element.
1. Click the **Add Component** button at the bottom of the Inspector. 
1. Select and assign the **Image Size** script to the Image element.  This script connects the Image element’s Width and Height parameters to the WidthHeight parameter of your shader, so when the Width or Height values change, the shader adapts.

Now you have a shader that will adapt to the aspect ratio of the UI element it’s assigned to. You can adjust the Width and Height parameters of the Image element and the Pill shape will maintain its rounded shape. This same feature is also available on many of the other nodes in the node library. You can control any subgraph node with a WidthHeight input port in this same way.