using UnityEngine;
using UnityEngine.UI;

public class UGUIErrorButtonLogic : MonoBehaviour
{
    public void OnButtonClick()
    {
        // Your logic goes here
        Debug.LogError("Hello from uGUI!");
    }
}
