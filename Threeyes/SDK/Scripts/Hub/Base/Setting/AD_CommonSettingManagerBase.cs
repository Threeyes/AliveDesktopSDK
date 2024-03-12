using System;
using System.Collections.Generic;
using Threeyes.Data;
using Threeyes.Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class AD_CommonSettingManagerBase<T> : HubSettingManagerBase<T, AD_SOCommonSettingManagerConfig, AD_CommonSettingConfigInfo>, IAD_CommonSettingManager
    where T : AD_CommonSettingManagerBase<T>
{
    #region Data Events
    public override void InitEvent()
    {
        Config.fileSystemSetting_TargetPhysicsSpeicalFolder.actionValueChangedEx += OnDataTargetPhysicsSpeicalFolderChangedEx;
        Config.fileSystemSetting_CustomPhysicsSpeicalFolderPath.actionValueChangedEx += OnDataCustomPhysicsSpeicalFolderPathChangedEx;

        Config.windowSetting_CoverAllMonitor.actionValueChangedEx += OnDataCoverAllMonitorChangedEx;
        Config.windowSetting_TargetMonitor.actionValueChangedEx += OnData_TargetMonitorChangedEx;



        Config.generalSetting_IsRunAtStartUp.actionValueChanged += OnDataIsRunAtStartUpChanged;
        Config.generalSetting_IsVSyncActive.actionValueChanged += OnDataIsVSyncActiveChanged;
        Config.generalSetting_TargetFrameRate.actionValueChanged += OnDataTargetFrameRateChanged;
        Config.generalSetting_Localization.actionValueChanged += OnDataLocalizationChanged;
        Config.generalSetting_Quality.actionValueChanged += OnDataQualityChanged;
        Config.generalSetting_ProcessPriority.actionValueChanged += OnDataProcessPriorityChanged;
    }

    protected virtual void OnDataTargetPhysicsSpeicalFolderChangedEx(string speicalFolderEnumValue, BasicDataState basicDataState) { }
    protected virtual void OnDataCustomPhysicsSpeicalFolderPathChangedEx(string customFolderDir, BasicDataState basicDataState) { }

    protected virtual void OnDataCoverAllMonitorChangedEx(bool value, BasicDataState basicDataState) { }
    protected virtual void OnData_TargetMonitorChangedEx(string value, BasicDataState basicDataState) { }


    protected virtual void OnDataIsRunAtStartUpChanged(bool value)
    {
        EventCommunication.SendMessage<IAD_CommonSetting_IsRunAtStartUpHandler>(inst => inst.OnIsRunAtStartUpChanged(value), includeHubScene: true);
    }
    protected virtual void OnDataIsVSyncActiveChanged(bool isActive)
    {
        EventCommunication.SendMessage<IAD_CommonSetting_IsVSyncActiveHandler>(inst => inst.OnIsVSyncActiveChanged(isActive), includeHubScene: true);
    }
    protected virtual void OnDataTargetFrameRateChanged(int value)
    {
        EventCommunication.SendMessage<IAD_CommonSetting_TargetFrameRateHandler>(inst => inst.OnTargetFrameRateChanged(value), includeHubScene: true);
    }
    protected virtual void OnDataLocalizationChanged(string value)
    {
        EventCommunication.SendMessage<IAD_CommonSetting_LocalizationHandler>(inst => inst.OnLocalizationChanged(value), includeHubScene: true);
    }
    protected virtual void OnDataQualityChanged(string value)
    {
        EventCommunication.SendMessage<IAD_CommonSetting_QualityHandler>(inst => inst.OnQualityChanged(value), includeHubScene: true);
    }
    protected virtual void OnDataProcessPriorityChanged(string value)
    {
        EventCommunication.SendMessage<IAD_CommonSetting_ProcessPriorityHandler>(inst => inst.OnProcessPriorityChanged(value), includeHubScene: true);
    }
    #endregion

    #region Data
    protected override List<string> GetListIgnoreBaseDataFieldName_Reset()
    {
        //忽略多语言
        return new List<string>() { nameof(AD_CommonSettingConfigInfo.generalSetting_Localization) };
    }
    #endregion

    #region Callback
    public virtual void OnModInit(Scene scene, ModEntry modEntry)
    {
        //ToUpdate：初始化设置
    }

    public virtual void OnModDeinit(Scene scene, ModEntry modEntry)
    {
    }
    #endregion
}

#region Define
/// <summary>
/// 通用设置
///
/// Todo:
/// -Quality适配URP，精简为3个，多语言照旧
/// 
/// PS：
/// 1.通过存储BasicData的类结构，用户可在程序运行前，修改字段的范围
/// </summary>
[System.Serializable]
public class AD_CommonSettingConfigInfo : HubSettingConfigInfoBase
{
    [Header("FileSystem")]
    public StringData fileSystemSetting_TargetPhysicsSpeicalFolder = new StringData("Desktop");//目标文件夹，对应 AD_PhysicsSpeicalFolder（Todo：要考虑多文件夹多场景共存的情况）（Todo：可以直接在界面上切换）
    public StringData fileSystemSetting_CustomPhysicsSpeicalFolderPath = new StringData("");//自定义文件夹路径（当上述设置为Custom时，对应的目标路径）

    [Header("Window Setting")]//窗口设置
    public BoolData windowSetting_CoverAllMonitor = new BoolData(false);//是否覆盖所有屏幕
    public StringData windowSetting_TargetMonitor = new StringData("");//（当CoverAllMonitor为false时有效）目标显示屏幕，默认为主屏幕

    [Header("General Setting")]//PS:(以下Option不能用枚举代替，因为可能会有变化（如多语言））
    //public BoolData generalSetting_IsAliveDesktopActive = new BoolData(true);//启用AD【先不使用】
    public BoolData generalSetting_IsRunAtStartUp = new BoolData(false);//系统运行时自动启动
    //public BoolData generalSetting_IsSupportMultiDisplay = new BoolData(true);//支持多屏幕【先不使用】
    public BoolData generalSetting_IsVSyncActive = new BoolData(true);//垂直同步（打开可以减少电脑发热现象及减少使用率；高屏幕刷新率的用户关闭以增加流畅度 ）
    public IntData generalSetting_TargetFrameRate = new IntData(60, new DataOption_Int(true, 60, 360));//垂直同步关闭后的默认帧率
    public StringData generalSetting_Localization = new StringData("English");//PS：多语言暂不需要重置，所以可以忽略
    public StringData generalSetting_Quality = new StringData("High");
    public StringData generalSetting_ProcessPriority = new StringData("High");
    public StringData generalSetting_Hotkeys_OpenBrowser = new StringData("");
    public StringData generalSetting_Hotkeys_OpenSetting = new StringData("");

    public AD_CommonSettingConfigInfo() { }
}
#endregion