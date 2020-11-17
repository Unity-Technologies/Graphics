using UnityEngine;

public class SwitchMaterials : MonoBehaviour {

    [SerializeField] string switchButton = "";
    [SerializeField] KeyCode switchKey = KeyCode.None;

    [SerializeField]
    Material sharedMaterial;
    [SerializeField]
    Material replacementProperties;
    Material backup;

    bool visible = false;

    private void Awake()
    {
        backup = new Material(sharedMaterial);
    }

    // Update is called once per frame
    void Update () {
        if (switchButton != "") if (Input.GetButtonDown(switchButton)) SwitchVisible();
        if (switchKey != KeyCode.None) if (Input.GetKeyDown(switchKey)) SwitchVisible();
    }

    public void SwitchVisible()
    {
        visible = !visible;
        if (visible)
        {
            sharedMaterial.CopyPropertiesFromMaterial(replacementProperties);
        }
        else
        {
            sharedMaterial.CopyPropertiesFromMaterial(backup);
        }
    }

    private void OnDisable()
    {
        sharedMaterial.CopyPropertiesFromMaterial(backup);
    }
}
