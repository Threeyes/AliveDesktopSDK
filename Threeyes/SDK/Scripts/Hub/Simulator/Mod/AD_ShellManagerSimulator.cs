using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Threeyes.Core;
public class AD_ShellManagerSimulator : AD_SerializableItemManagerSimulatorBase<AD_ShellManagerSimulator, IAD_ShellController, AD_DefaultShellController, AD_ShellPrefabConfigInfo, AD_SOShellPrefabInfoGroup, AD_SOShellPrefabInfo, AD_ShellItemInfo>
    , IAD_ShellManager
{
    public AD_SOShellManagerSimulatorConfig soDefaultConfig;//SDK默认的配置
    protected override void InitWithDefaultDatas()
    {
        ActiveController.InitBase(GetSimulatorDatas(), true);
    }

    protected virtual List<AD_ShellItemInfo> GetSimulatorDatas()
    {
        AD_SOShellManagerSimulatorConfig soTargetConfig = soDefaultConfig;
        if (AD_SOShellManagerSimulatorConfig.InstanceExists)//如果用户提供了自定义的SO，则改为使用其数据
        {
            soTargetConfig = AD_SOShellManagerSimulatorConfig.Instance;//(调用Instance就会创建实例)用户自定义的存放在（Resources/Threeyes）的配置文件
            if (soTargetConfig.listShellItemInfo.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"For simulator testing, please initialize the parameters of {nameof(AD_SOShellManagerSimulatorConfig)} in path {UnityEditor.AssetDatabase.GetAssetPath(soTargetConfig)}!");
#endif
            }
        }

        return soTargetConfig.listShellItemInfo;
    }

    #region Utility
    [ContextMenu("Create Custom Config")]
    void EditorCreatConfig()
    {
        var inst = AD_SOShellManagerSimulatorConfig.Instance;//里面有在默认路径创建或获取单例的方法GetOrCreateInstance
    }
    #endregion
}