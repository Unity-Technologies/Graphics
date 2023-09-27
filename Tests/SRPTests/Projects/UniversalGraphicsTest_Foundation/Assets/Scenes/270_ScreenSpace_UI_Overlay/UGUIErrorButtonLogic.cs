using UnityEngine;
using UnityEngine.UI;

public class UGUIErrorButtonLogic : MonoBehaviour
{
    public void OnButtonClick()
    {
        Debug.LogError("Hello from uGUI!");
    }
}
