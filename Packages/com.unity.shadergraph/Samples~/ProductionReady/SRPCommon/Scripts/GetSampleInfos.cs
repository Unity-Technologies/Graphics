using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteInEditMode]
public class PRSGetSampleInfos : MonoBehaviour
{
    public enum Type
    {
        Introduction,
        Title, 
        Description
    }
    
    public Type type;
    public GameObject prefab;
    
    private TextMeshPro TextMeshProComponent = null;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0);
        UpdateText();
    }
    
    void UpdateTextMeshProReference()
    {
        TextMeshProComponent = this.GetComponent<TextMeshPro>();
        if(TextMeshProComponent == null)
            Debug.LogError($"TextMeshPro Component cannot be found on this GameObject: {this.gameObject.name}");
    }
    
    // Called when something has changed in the script
    void OnValidate()
    {
        UpdateText();
    }
    
    void UpdateText()
    {
        if(TextMeshProComponent == null) 
            UpdateTextMeshProReference();

        switch(type)
        {
            case Type.Introduction:
                TextMeshProComponent.text = PRSSamplesShowcase.GetSanitizedIntroduction();
                break;
            case Type.Title:
                TextMeshProComponent.text = PRSSamplesShowcase.GetSanitizedTitle(prefab.name);
                break;
            case Type.Description:
                TextMeshProComponent.text = PRSSamplesShowcase.GetSanitizedDescription(prefab.name);
                break;
        }
    }

}
