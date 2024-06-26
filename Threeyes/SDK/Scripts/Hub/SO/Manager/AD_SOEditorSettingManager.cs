#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Threeyes.Steamworks;
/// <summary>
/// 【Editor】缓存用户设置
/// </summary>
public class AD_SOEditorSettingManager : SOEditorSettingManager<AD_SOEditorSettingManager, AD_SOWorkshopItemInfo>
{
    #region Property & Field
    public AD_PlatformMode PlatformMode
    {
        get
        {
            return platformMode;
        }

        set
        {
            platformMode = value;
            EditorUtility.SetDirty(Instance);
        }
    }
    public bool HubSimulator_ShowAssistantGizmo
    {
        get
        {
            return hubSimulator_ShowAssistantGizmo;
        }
        set
        {
            hubSimulator_ShowAssistantGizmo = value;
            EditorUtility.SetDirty(Instance);
        }
    }
    public bool HubSimulator_ShowAssistantInfo
    {
        get
        {
            return hubSimulator_ShowAssistantInfo;
        }
        set
        {
            hubSimulator_ShowAssistantInfo = value;
            EditorUtility.SetDirty(Instance);
        }
    }
    public bool HubSimulator_EnableFly
    {
        get
        {
            return hubSimulator_EnableFly;
        }
        set
        {
            hubSimulator_EnableFly = value;
            EditorUtility.SetDirty(Instance);
        }
    }
    public bool HubSimulator_IsPenetrateOnFly
    {
        get
        {
            return hubSimulator_IsPenetrateOnFly;
        }
        set
        {
            hubSimulator_IsPenetrateOnFly = value;
            EditorUtility.SetDirty(Instance);
        }
    }
    public bool HubSimulator_UseGravity
    {
        get
        {
            return hubSimulator_UseGravity;
        }
        set
        {
            hubSimulator_UseGravity = value;
            EditorUtility.SetDirty(Instance);
        }
    }
    [Header("CommandLine Simulator")]//Simulate runtime command line
    [SerializeField] protected AD_PlatformMode platformMode = AD_PlatformMode.PC;


    [Header("Hub Simulator")]
    [SerializeField] protected bool hubSimulator_ShowAssistantGizmo = true;
    [SerializeField] protected bool hubSimulator_ShowAssistantInfo = true;
    [SerializeField] protected bool hubSimulator_EnableFly = true;
    [SerializeField] protected bool hubSimulator_IsPenetrateOnFly = true;
    [SerializeField] protected bool hubSimulator_UseGravity = false;


    #endregion
}
#endif
