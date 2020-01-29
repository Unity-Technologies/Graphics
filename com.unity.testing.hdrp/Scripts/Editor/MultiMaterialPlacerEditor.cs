using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MultiMaterialPlacer))]
[CanEditMultipleObjects]
public class MultiMaterialPlacerEditor : Editor
{
    // We need to use and to call an instnace of the default MaterialEditor
    private MaterialEditor _materialEditor;

    private void OnEnable()
    {
        if ( !serializedObject.FindProperty("material").hasMultipleDifferentValues )
        {
            if (serializedObject.FindProperty("material") != null)
            {
                // Create an instance of the default MaterialEditor
                _materialEditor = (MaterialEditor)CreateEditor((Material) serializedObject.FindProperty("material").objectReferenceValue);
            }
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();
        if ( GUILayout.Button("Place") )
        {
            foreach (object obj in targets)
            {
                PlaceObjects(obj as MultiMaterialPlacer);
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();

            if (_materialEditor != null)
            {
                // Free the memory used by the previous MaterialEditor
                DestroyImmediate(_materialEditor);
            }

            if (!serializedObject.FindProperty("material").hasMultipleDifferentValues && (serializedObject.FindProperty("material") != null) )
            {
                // Create a new instance of the default MaterialEditor
                _materialEditor = (MaterialEditor)CreateEditor((Material)serializedObject.FindProperty("material").objectReferenceValue);

            }
        }


        if (_materialEditor != null)
        {
            // Draw the material's foldout and the material shader field
            // Required to call _materialEditor.OnInspectorGUI ();
            _materialEditor.DrawHeader();

            //  We need to prevent the user to edit Unity default materials
            bool isDefaultMaterial = !AssetDatabase.GetAssetPath((Material)serializedObject.FindProperty("material").objectReferenceValue).StartsWith("Assets");

            using (new EditorGUI.DisabledGroupScope(isDefaultMaterial))
            {

                // Draw the material properties
                // Works only if the foldout of _materialEditor.DrawHeader () is open
                _materialEditor.OnInspectorGUI();
            }
        }
    }

    public static void PlaceObjects(MultiMaterialPlacer _target)
    {
        //clear hierarchy
        for (int i=_target.transform.childCount-1; i>=0; --i)
        {
            //DestroyImmediate(_target.transform.GetChild(i).GetComponent<Renderer>().sharedMaterial);
            DestroyImmediate(_target.transform.GetChild(i).gameObject);
        }

        if (_target.prefabObject == null) return;

        List<Material> materials = new List<Material>();

        Renderer refObject = Instantiate(_target.prefabObject.gameObject).GetComponent<Renderer>();
        if (_target.material != null)
            refObject.sharedMaterial = Instantiate( _target.material );
        else
            refObject.sharedMaterial = Instantiate(_target.prefabObject.sharedMaterial);

        for (int i = 0; i < _target.commonParameters.Length; i++)
        {
            ApplyParameterToMaterial(refObject.sharedMaterial, _target.commonParameters[i]);
            if (_target.overideRenderQueue) refObject.sharedMaterial.renderQueue = (int)_target.renderQueue;
        }

        float x = 0f;
        float y = 0f;

        if (_target.is2D)
        {
            if (_target.instanceParameters.Length < 2) return;
            if (!(_target.instanceParameters[0].multi && _target.instanceParameters[1].multi)) return;

            for (int i = 0; i < _target.instanceParameters[0].count; i++)
            {
                for (int j = 0; j < _target.instanceParameters[1].count; j++)
                {
                    Renderer tmp = CopyObject(refObject, x, y, _target.transform, _target);
                    tmp.gameObject.name = _target.prefabObject.name+"_"+ ApplyParameterToMaterial(tmp.sharedMaterial, _target.instanceParameters[0], i);
                    tmp.gameObject.name += "_"+ApplyParameterToMaterial(tmp.sharedMaterial, _target.instanceParameters[1], j);
                    materials.Add(tmp.sharedMaterial);
                    y -= _target.offset;
                }
                x += _target.offset;
                y = 0f;
            }
        }
        else
        {
            for (int i = 0; i < _target.instanceParameters.Length; i++)
            {
                if (!string.IsNullOrEmpty(_target.instanceParameters[i].parameter))
                {
                    if (_target.instanceParameters[i].multi)
                    {
                        if (_target.instanceParameters[i].paramType == MaterialParameterVariation.ParamType.Texture)
                        {
                            Renderer tmp = CopyObject(refObject, x, y, _target.transform, _target);
                            tmp.gameObject.name = _target.prefabObject.name + "_" + ApplyParameterToMaterial(tmp.sharedMaterial, _target.instanceParameters[i]);
                            materials.Add(tmp.sharedMaterial);
                        }
                        else
                        {
                            for (int j = 0; j < _target.instanceParameters[i].count; j++)
                            {
                                if (j > 0)
                                    x += _target.offset;

                                Renderer tmp = CopyObject(refObject, x, y, _target.transform, _target);
                                tmp.gameObject.name = _target.prefabObject.name + "_" + ApplyParameterToMaterial(tmp.sharedMaterial, _target.instanceParameters[i], j);
                                materials.Add(tmp.sharedMaterial);
                            }
                        }
                    }
                    else
                    {
                        Renderer tmp = CopyObject(refObject, x, y, _target.transform, _target);
                        tmp.gameObject.name = _target.prefabObject.name + "_" + ApplyParameterToMaterial(tmp.sharedMaterial, _target.instanceParameters[i]);
                        materials.Add(tmp.sharedMaterial);
                    }
                }

                x += _target.offset;
            }
        }

        DestroyImmediate(refObject.gameObject);
        
        if (materials != null)
        {
            foreach (Material mat in materials)
            {
                UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(mat);
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
    }

    public static Renderer CopyObject( Renderer _target, float _x, float _y, Transform _parent, MultiMaterialPlacer _placer )
    {
        Renderer o = Instantiate(_target.gameObject).GetComponent<Renderer>();
        o.sharedMaterial = Instantiate(_target.sharedMaterial);
        o.transform.parent = _parent;
        o.transform.localPosition = new Vector3(_x, _y, 0f);
        o.transform.localRotation = Quaternion.identity;
        o.transform.localScale = Vector3.one * _placer.scale;
        o.transform.localEulerAngles = _placer.rotation;

        o.gameObject.isStatic = _parent.gameObject.isStatic;
        return o;
    }

    public static string ApplyParameterToMaterial(Material _mat, MaterialParameterVariation _param)
    {
        if (_param.multi) return null;
        string o = _param.parameter + "_";
        switch (_param.paramType)
        {
            case MaterialParameterVariation.ParamType.Bool:
                _mat.SetFloat(_param.parameter, _param.b_Value ? 1f : 0f);
                o += _param.b_Value.ToString();
                break;
            case MaterialParameterVariation.ParamType.Float:
                _mat.SetFloat(_param.parameter, _param.f_Value);
                o += string.Format("{0:0.00}", _param.f_Value);
                break;
            case MaterialParameterVariation.ParamType.Int:
                _mat.SetInt(_param.parameter, _param.i_Value);
                o += _param.i_Value.ToString();
                break;
            case MaterialParameterVariation.ParamType.Vector:
                _mat.SetVector(_param.parameter, _param.v_Value);
                o += _param.v_Value.ToString();
                break;
            case MaterialParameterVariation.ParamType.Texture:
                _mat.SetTexture(_param.parameter, _param.t_Value);
                o += _param.t_Value.ToString();
                break;
            case MaterialParameterVariation.ParamType.Color:
                _mat.SetColor(_param.parameter, _param.c_Value);
                o += _param.c_Value.ToString();
                break;
        }

        return o;
    }

    public static string ApplyParameterToMaterial(Material _mat, MaterialParameterVariation _param, int _num)
    {
        if (!_param.multi) return null;
        if (_param.paramType == MaterialParameterVariation.ParamType.Texture) return null;
        if ((_num < 0) || (_num > _param.count)) return null;
        if ((_param.paramType == MaterialParameterVariation.ParamType.Bool) && (_num > 1)) return null;

        float f = 1.0f * _num / (_param.count - 1.0f);
        string o = _param.parameter+"_";

        switch (_param.paramType)
        {
            case MaterialParameterVariation.ParamType.Bool:
                _mat.SetFloat(_param.parameter, _num);
                o += (_num==1)?"true":"false";
                break;
            case MaterialParameterVariation.ParamType.Float:
                _mat.SetFloat(_param.parameter, Mathf.Lerp(_param.f_Value, _param.f_Value_Max, f));
                o += string.Format("{0:0.00}", Mathf.Lerp(_param.f_Value, _param.f_Value_Max, f));
                break;
            case MaterialParameterVariation.ParamType.Int:
                _mat.SetInt(_param.parameter, Mathf.RoundToInt(Mathf.Lerp(_param.i_Value, _param.i_Value_Max, f)));
                o += Mathf.RoundToInt(Mathf.Lerp(_param.i_Value, _param.i_Value_Max, f)).ToString();
                break;
            case MaterialParameterVariation.ParamType.Vector:
                _mat.SetVector(_param.parameter, Vector4.Lerp(_param.v_Value, _param.v_Value_Max, f));
                o += Vector4.Lerp(_param.v_Value, _param.v_Value_Max, f).ToString();
                break;
            case MaterialParameterVariation.ParamType.Color:
                _mat.SetColor(_param.parameter, Color.Lerp(_param.c_Value, _param.c_Value_Max, f));
                o += Color.Lerp(_param.c_Value, _param.c_Value_Max, f).ToString();
                break;
        }

        return o;
    }
}
