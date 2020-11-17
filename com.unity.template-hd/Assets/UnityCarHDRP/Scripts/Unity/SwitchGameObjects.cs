using UnityEngine;

public class SwitchGameObjects : MonoBehaviour {

    [SerializeField] string switchButton = "";
    [SerializeField] KeyCode switchKey = KeyCode.None;

    [SerializeField]
    GameObject[] onGameObjects;

    [SerializeField]
    GameObject[] offGameObjects;

    bool visible = false;

    // Update is called once per frame
    void Update () {
        if (switchButton != "") if (Input.GetButtonDown(switchButton)) SwitchVisible();
        if (switchKey != KeyCode.None) if (Input.GetKeyDown(switchKey)) SwitchVisible();
    }

    public void SwitchVisible()
    {
        visible = !visible;
        for (int i = 0; i < onGameObjects.Length; i++)
        {
            onGameObjects[i].SetActive(visible);
        }

        for (int i = 0; i < offGameObjects.Length; i++)
        {
            offGameObjects[i].SetActive(!visible);
        }
    }
}
