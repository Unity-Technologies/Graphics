using UnityEngine;

public class SwitchMaterialColor : MonoBehaviour {

    [SerializeField] Color[] colors;
    [Range(0,1)]
    [SerializeField] float[] reflecions;
    [SerializeField] int currentIndex;
    [SerializeField] Material targetMaterial;

    [SerializeField] string nextButton = "";
    [SerializeField] KeyCode nextKey = KeyCode.None;

    [SerializeField] string previousButton = "";
    [SerializeField] KeyCode previousKey = KeyCode.None;
    
    private void OnEnable()
    {
        UpdateActiveStates();
    }

    // Update is called once per frame
    void Update()
    {
        if (nextButton != "") if (Input.GetButtonDown(nextButton)) Next();
        if (nextKey != KeyCode.None) if (Input.GetKeyDown(nextKey)) Next();

        if (previousButton != "") if (Input.GetButtonDown(previousButton)) Previous();
        if (previousKey != KeyCode.None) if (Input.GetKeyDown(previousKey)) Previous();
    }

    public void Next()
    {
        currentIndex++;
        if (currentIndex > colors.Length - 1) currentIndex = 0;
        UpdateActiveStates();
    }

    public void Previous()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = colors.Length - 1;
        UpdateActiveStates();
    }

    void UpdateActiveStates()
    {
        targetMaterial.SetColor("_BaseColor", colors[currentIndex]);
        targetMaterial.SetFloat("_Metallic", reflecions[currentIndex]);
    }
}
