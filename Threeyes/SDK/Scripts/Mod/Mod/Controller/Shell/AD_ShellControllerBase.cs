using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threeyes.Config;
using Threeyes.Core;
using UnityEngine;

public abstract class AD_ShellControllerBase<TManager, TElement, TEleData, TBaseEleData, TSOConfig, TConfig> : AD_SerializableItemControllerBase<TManager, AD_ShellPrefabConfigInfo, AD_SOShellPrefabInfoGroup, AD_SOShellPrefabInfo, TElement, TEleData, TBaseEleData, TSOConfig, TConfig>
    where TManager : AD_SerializableItemControllerBase<TManager, AD_ShellPrefabConfigInfo, AD_SOShellPrefabInfoGroup, AD_SOShellPrefabInfo, TElement, TEleData, TBaseEleData, TSOConfig, TConfig>
    where TElement : ElementBase<TEleData>, IAD_SerializableItem
    where TEleData : class, IAD_SerializableItemInfo, new()
    where TSOConfig : SOConfigBase<TConfig>
    where TConfig : AD_SerializableItemControllerConfigInfoBase<AD_SOShellPrefabInfo>
{
    #region Init
    protected override GameObject GetPrefab(TEleData eleData)
    {
        return overridePrefab ?? GetFirstValidPrefab(eleData);
    }

    /// <summary>
    /// 查找首个有效的Prefab，常用于首次初始化
    /// </summary>
    /// <param name="eleData"></param>
    /// <returns></returns>
    GameObject GetFirstValidPrefab(TEleData eleData)
    {
        //#1 尝试查找匹配
        AD_SOShellPrefabInfo targetPrefabInfo = GetAllValidPrefabInfosFunc(eleData, true).FirstOrDefault();

        //#2 如果上述匹配找不到有效物体，则返回Fallback预制物信息，避免出错
        if (targetPrefabInfo == null)
        {
            Debug.LogWarning($"Can't find prefabInfo for {eleData}! Try get fallback element instead!");//不算错误，仅弹出警告
            targetPrefabInfo = GetFallbackPrefabInfo(eleData);
        }

        //#3 仍然找不到：报错
        if (!targetPrefabInfo)
        {
            Debug.LogError($"Can't find prefadInfo for [{eleData}]! Check if list empty！");
        }
        return targetPrefabInfo?.Prefab;
    }


    /// <summary>
    /// 获取与data匹配的所有预制物（可用于后期Shell根据条件进行匹配）
    /// </summary>
    /// <param name="eleData"></param>
    /// <param name="matchingCondition">是否根据条件进行匹配，如果为否则返回所有预制物。（可通过UI上的一个Toggle开关，方便用户使用其他物体代替）</param>
    /// <returns></returns>
    protected virtual List<AD_SOShellPrefabInfo> GetAllValidPrefabInfosFunc(TEleData eleData, bool matchingCondition)
    {
        List<AD_SOShellPrefabInfo> listTargetPrefabInfo = new List<AD_SOShellPrefabInfo>();

        //搜索所有有效的PI
        List<AD_SOShellPrefabInfo> listSourcePrefabInfo = new List<AD_SOShellPrefabInfo>();
        List<AD_ShellPrefabConfigInfo> allPCI = AD_ManagerHolder.ShellManager.GetAllPrefabConfigInfo();
        foreach (var pCI in allPCI)
        {
            listSourcePrefabInfo.AddRange(pCI.FindAllPrefabInfo());
        }

        //根据条件，查询有效的PCI
        if (matchingCondition)
        {
            listTargetPrefabInfo = GetAllValidPrefabInfos_Matching(eleData, listSourcePrefabInfo);
        }
        else//不需要匹配：返回所有
        {
            listTargetPrefabInfo.AddRange(listSourcePrefabInfo);
        }
        return listTargetPrefabInfo;
    }
    protected abstract List<AD_SOShellPrefabInfo> GetAllValidPrefabInfos_Matching(TEleData eleData, List<AD_SOShellPrefabInfo> listSourcePrefabInfo);


    protected abstract AD_SOShellPrefabInfo GetFallbackPrefabInfo(TEleData eleData);
    #endregion
}
