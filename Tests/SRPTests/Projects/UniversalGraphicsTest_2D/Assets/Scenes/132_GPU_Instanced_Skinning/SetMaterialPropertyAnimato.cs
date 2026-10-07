using UnityEngine;

public class SetMaterialPropertyAnimato : MonoBehaviour
{
    public float multiplier = 0.12f;
    MaterialPropertyBlock matPropBlock;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        matPropBlock = new MaterialPropertyBlock();
        matPropBlock.SetFloat("_Multiplier", multiplier);
        gameObject.GetComponent<SpriteRenderer>().SetPropertyBlock(matPropBlock);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
