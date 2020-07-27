# Output Event Handlers

A VFXOutputEventHandler is an API helper that hooks into an [Output Event](Contexts.md#output-events) to allow you to execute scripts based on the event.

The Visual Effect Graph includes a set of example scripts as a sample. For information on how to install the sample, see [Installing sample scripts](#installing-sample-scripts).

## Output Event Handler API

To create your own output event handler, write a script that extends the `UnityEngine.VFX.Utility.VFXOutputEventHandler` class.

When you write a MonoBehaviour that extends this class, it reduces the  amount of code required to perform a hook. This is because the base  class does the job of filtering the event and calls the following method :

`override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)`

When you implement this method, Unity calls it every time the event triggers and passes in the event's attributes.

### Example

The following example teleports a GameObject to the Given position when it receives an event.

```c#
[RequireComponent(typeof(VisualEffect))]
public class VFXOutputEventTeleportObject : VFXOutputEventHandler
{
    public Transform target;
    
    static readonly int kPosition = Shader.PropertyToID("position");
    
    public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
    {
        if(target != null)
            target.position = eventAttribute.GetVector3(kPosition);
    }
}
```

## Installing Sample Scripts

To help you create your own VFXOutputEventHandler, the Visual Effect  Graph package includes a set of example scripts that you can install via the Package Manager. To do this, select the Visual Effect Graph package then, next to **OutputEvent Helpers Sample**, click the **Import** button.