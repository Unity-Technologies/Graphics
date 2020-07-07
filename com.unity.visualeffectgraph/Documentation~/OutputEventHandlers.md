# Output Event Handlers

Output Event Handlers is an API Helper that hooks into [Output Events](Contexts.md#output-events) in order to perform scripts based on these events.

A set of Example Scripts can be installed as samples

## Output Event Handler API

The output event Handler API helps you write utility scripts based on the `UnityEngine.VFX.Utility.VFXOutputEventHandler` class. 

Writing MonoBehaviours that extend this class reduce the amount of code required to perform a hook, the base class doing the job of filtering the event and calling a simple method :

`override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)`

Once implemented, this method is executed every time the event is sent, in to process it along with its attributes.

### Example

The following Example Teleports a Game Object to the Given position when an event is Received.

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

You can install a set of Sample Scripts through the Package Manager, by selecting the Visual Effect Graph package, and clicking the Import Button that corresponds to the OutputEvent Helpers Sample.