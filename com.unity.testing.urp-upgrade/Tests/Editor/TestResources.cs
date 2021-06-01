using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

/// <summary>
/// Utility containing references to various test resources.
/// Default values for each resource have been applied via this script's .meta file.
/// </summary>
public class TestResources : ScriptableObject
{
    // clips
    public AnimationClip Clip_Animation_WithMaterialProperties_PotentiallyUpgradable;
    public AnimationClip Clip_Animation_WithoutMaterialProperties;
    public AnimationClip Clip_Animation_WithMaterialProperties_OnlyUsedByUpgradable;
    public AnimationClip Clip_Animation_WithMaterialProperties_OnlyUsedByNotUpgradable;
    public AnimationClip Clip_Animation_WithMaterialProperties_UsedByUpgradableAndNotUpgradable;
    public AnimationClip Clip_Animator_WithMaterialProperties_PotentiallyUpgradable;
    public AnimationClip Clip_Animator_WithoutMaterialProperties;
    public AnimationClip Clip_Animator_WithMaterialProperties_OnlyUsedByUpgradable;
    public AnimationClip Clip_Animator_WithMaterialProperties_OnlyUsedByNotUpgradable;
    public AnimationClip Clip_Animator_WithMaterialProperties_UsedByUpgradableAndNotUpgradable;
    public AnimationClip Clip_Timeline_WithoutMaterialProperties;
    public AnimationClip Clip_Timeline_WithMaterialProperties_OnlyUsedByUpgradable;
    public AnimationClip Clip_Timeline_WithMaterialProperties_OnlyUsedByNotUpgradable;
    public AnimationClip Clip_Timeline_WithMaterialProperties_UsedByUpgradableAndNotUpgradable;
    public AnimationClip Clip_Timeline_WithMaterialProperties_PotentiallyUpgradable;
    public AnimationClip Clip_Timeline_Standalone_WithMaterialProperties_Upgradable;

    public AnimationClip Clip_Animator_WithMaterialProperties_PotentiallyUpgradable_NotEditable;

    public UnityObject Clip_ParentAsset;
    public UnityObject Clip_ChildAsset_Animation_WithMaterialProperties;
    public UnityObject Clip_ChildAsset_Animation_WithoutMaterialProperties;
    public UnityObject Clip_ChildAsset_Animator_WithMaterialProperties;
    public UnityObject Clip_ChildAsset_Animator_WithoutMaterialProperties;

    // materials
    public Material Material_Legacy_Upgradable;
    public Material Material_Legacy_NotUpgradable;
    public Material Material_URP;

    // upgradable prefabs
    public GameObject Prefab_Animation_WithMaterialProperties_Upgradable;
    public GameObject Prefab_Animator_WithMaterialProperties_Upgradable;
    public GameObject Prefab_Timeline_WithMaterialProperties_Upgradable;
    public GameObject Variant_Animation_WithMaterialProperties_Upgradable;
    public GameObject Variant_Animator_WithMaterialProperties_Upgradable;
    public GameObject Variant_Timeline_WithMaterialProperties_Upgradable;

    public GameObject Prefab_Animation_WithoutMaterialProperties;
    public GameObject Prefab_Animator_WithoutMaterialProperties;
    public GameObject Prefab_Timeline_WithoutMaterialProperties;
    public GameObject Variant_Animation_WithoutMaterialProperties;
    public GameObject Variant_Animator_WithoutMaterialProperties;
    public GameObject Variant_Timeline_WithoutMaterialProperties;

    // non-upgradable prefabs
    public GameObject Prefab_Animation_WithMaterialProperties_NotUpgradable;
    public GameObject Prefab_Animation_WithMaterialProperties_NoMaterials;
    public GameObject Prefab_Animation_WithMaterialProperties_NoRenderer;
    public GameObject Prefab_Animator_WithMaterialProperties_NotUpgradable;
    public GameObject Prefab_Animator_WithMaterialProperties_NoMaterials;
    public GameObject Prefab_Animator_WithMaterialProperties_NoRenderer;
    public GameObject Prefab_Timeline_WithMaterialProperties_Upgradable_AlsoUsedByNotUpgradable;
    public GameObject Prefab_Timeline_WithMaterialProperties_NotUpgradable;
    public GameObject Prefab_Timeline_WithMaterialProperties_NotUpgradable_AlsoUsedByUpgradable;
    public GameObject Prefab_Timeline_WithMaterialProperties_NoMaterials;
    public GameObject Prefab_Timeline_WithMaterialProperties_NoRenderer;
    public GameObject Prefab_Timeline_Standalone_WithMaterialProperties_Upgradable;
    public GameObject Variant_Animation_WithMaterialProperties_NotUpgradable;
    public GameObject Variant_Animator_WithMaterialProperties_NotUpgradable;
    public GameObject Variant_Timeline_WithMaterialProperties_NotUpgradable;

    // upgradable scenes
    public SceneAsset Scene_Animation_WithMaterialProperties_Upgradable;
    public SceneAsset Scene_Animator_WithMaterialProperties_Upgradable;
    public SceneAsset Scene_Timeline_WithMaterialProperties_Upgradable;

    // non-upgradable scenes
    public SceneAsset Scene_Animation_WithoutMaterialProperties;
    public SceneAsset Scene_Animator_WithoutMaterialProperties;
    public SceneAsset Scene_Timeline_WithoutMaterialProperties;
    public SceneAsset Scene_NoAnimation;
}
