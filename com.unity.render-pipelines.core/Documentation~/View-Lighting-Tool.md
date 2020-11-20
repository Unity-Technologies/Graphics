# View Lighting Tool

View Lighting Tool is a tool that allow to setup lighting in the camera space. The main Camera (Game View) is used to setup the orientation of a light with spherical coordinate. We can choose a target and Yaw, Pitch and Roll (useful when a cookie or IES is setuped).

![](Images/ViewLightingTool00.png)

## Using View Lighting Tool

From a light we can add a component following Rendering > Light Anchor. Select the light on the light in the hierachy and enable the tool:
![](Images/ViewLightingTool01.png)

Which change the gizmo of the light to setup the target.
![](Images/ViewLightingTool02.gif)

On the inspector we can orient the light relative to this target and the main camera.
The distance:
![](Images/ViewLightingTool03.gif)

And the orientation:
![](Images/ViewLightingTool04.gif)

When a Cookie or an IES is setuped the last knob allow us to orient this one:
![](Images/ViewLightingTool05.gif)
