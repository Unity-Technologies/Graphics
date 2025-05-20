# Interaction between the water system and the VFX Graph

The water system supports being evaluated from the VFX Graph, to access data such as the water height at a given point, the surface normal, or the current value.

However there are several simulations that are important to be aware of.
As the water surface gameobject is saved inside a scene, and the VFX graph is an asset on disk, it is not possible to directly reference the surface from within the graph. This means data of the water surface need to be set globally by the user before the VFX can sample the water.
As a result, only a single surface can be sampled from any VFX Graph at any given time.

Additionally, the settings on the Sample node in the VFX Graph needs to be set according to what water surfaces will be bound globally at runtime. This inclues setting the proper **Surface Type** and enabling **Include Current** if a current map is assigned on the water surface.
The **Evaluate Ripples** needs to be disabled if the surface doesn't have ripples, but can be disabled on purpose for performance reasons even if the surface has ripplies, at the cost of slighly lower precision in the results. The same applies to the **Include Deformation** option.

The following script can be used to bind the relevant textures in the global scope, so that the VFX Graph can access them.
```
public class WaterSurfaceBinder : MonoBehaviour 
{
    public WaterSurface waterSurface;

    void Start()
    {
        waterSurface.SetGlobalTextures();
    }
}
```
