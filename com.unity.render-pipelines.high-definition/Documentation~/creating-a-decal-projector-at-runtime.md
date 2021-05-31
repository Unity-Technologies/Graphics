# Creating a Decal Projector at runtime

The [Decal Projector](Decal-Projector.md) component enables you to project decals into an environment. When you create a Decal Projector at runtime, the workflow is slightly different to the workflow you usually use when instantiating [Prefabs](https://docs.unity3d.com/Manual/Prefabs.html).

Instantiating a new DecalProjector from a Prefab does not automatically create a new instance of the DecalProjector's material. If you spawn multiple instances of the DecalProjector and make changes in one of its materials, the changes affect every DecalProjector instance. In the usual Prefab workflow, Unity creates a new instance of the material with the new Prefab instance. To match this usual workflow you must manually create a new material from the Prefab when you instantiate a new DecalProjector instance. For an example of how to do this, see the following code sample.

```
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class CreateDecalProjectorExample : MonoBehaviour
{
    public GameObject m_DecalProjectorPrefab;

    void Start()
    {
        GameObject m_DecalProjectorObject = Instantiate(m_DecalProjectorPrefab);
        DecalProjector m_DecalProjectorComponent = m_DecalProjectorObject.GetComponent<DecalProjector>();

        // Creates a new material instance for the DecalProjector.
        m_DecalProjectorComponent.material = new Material(m_DecalProjectorComponent.material);

    }
}
```
