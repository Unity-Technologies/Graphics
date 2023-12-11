# Debug Modes

For authoring purposes, the water surface component has debug view modes. Those views are available in Miscellaneous section in the water surface component. 
They are especially useful for placing the different areas (Mask, Deformation, Foam) precisely. 

## Water Mask
The Water Mask mode shows the attenuation of each frequency band. White color means no attenuation, black color means 100% masking. 
You can select which channel of the water mask to debug by using the **Water Mask Mode** dropdown. 
Note that, for saving texture space, the red channel always attenuate the first band (First swell band for oceans, Agitation for rivers, Ripples for pools), green channel, the second band (Second swell band for oceans, ripples for rivers)... etc

![](Images/water-debug-watermask.png)


## Simulation Foam Mask
The Simulation Foam Mask mode shows where the simulation foam is rendered on your water surface.  

![](Images/water-debug-foammask.png)


## Current
The Current mode shows in which direction the current flows. You can select between swell or agitation and ripples to debug.
Note that this mode does not take into account the chaos parameter set in the simulation section.

![](Images/water-debug-current.png)


## Deformation
The Deformation mode shows the deformation area and the deformation height of the water surface. 

![](Images/water-debug-deformation.png)


## Foam
The Foam mode shows the foam are and where the generated foam (both foam from foam generated and simulation foam) are rendered. 
You can select to show Surface Foam or Deep Foam using the **Water Foam Mode**. 

![](Images/water-debug-foam.png)



