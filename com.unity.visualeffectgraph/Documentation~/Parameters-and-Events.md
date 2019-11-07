Parameters and Events are the external interface of a Visual Effect, they enable custom behaviors and communication with the scene through C#

## Parameters

Parameters are the data interface that is exposed to the component. Its purpose is to make customizable, self-contained effects where inner logic can be dynamically linked to the scene. 

Parameters are exposed the same way shaders expose properties so they can be used by materials. The major difference is that all inner VFX Types can be exposed and manipulated using gizmos.

### Creating and Exposing Parameters

Parameters can be created through different actions :

* Using the Blackboard Panel, click the + button and select the type of parameter you want to create
* In the node creation panel, in an empty space, you can select `{any type} (Parameter)`  to create a parameter instance node, and also add an entry to the blackboard.
* On any Inline operator node, you can Right Click and select `Convert to Parameter` to convert the node to a parameter operator. It will also create an entry to the blackbord with the value transferred to it.

Parameters are not automatically exposed to the component level upon creation. You can tick the Exposed checkbox to make it actually visible to the component level.

* Exposed parameters are used to configure the effect from the outside
* Non-Exposed parameters are used to factorize values through complex graphs

### Modifying Parameter interface

When exposed, a parameter will appear inside the parameters section of the Visual Effect component, displaying its default value. You can override this value by ticking the checkbox left to the parameter's label, and by changing its value. This way, the parameter is now overridden and will retain its values for this particular instance.

Ticking off the override checkbox will revert the parameter to its default value.

Parameters can also be overridden using C# by using `SetFloat`, `SetVector`, ... methods

### Binding Parameters to Scene elements

The Visual Effects Editor comes with a set of VFX Binder utilities that you can use to bind parameters to various elements in scene (Transforms, Colliders), or to bind values to even more complex behaviors such as RayCast results, etc. See the Utilities section for more information.

## Events

Events are the triggers that will control the activation and deactivation of particle spawn. They connect to spawn contexts and can enable or disable them.

Events can also carry attributes so the spawn can be customized according to scene events: By sending parametrized events through the component, the spawn contexts can gather attributes and transfer them to the particles. 

For instance, a multi-explosion effect can configure a custom position every time a `Explode` event is sent. Using parametrized `SendEvent()` functions will attach an attribute payload to the event. See C# API section for more information.



