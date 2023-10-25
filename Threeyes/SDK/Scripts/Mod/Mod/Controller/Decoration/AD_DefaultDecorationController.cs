using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threeyes.RuntimeEditor;
using UnityEngine;
using static AD_DefaultDecorationItem;
/// <summary>
/// PS：
/// -为了避免反序列化时预制物与已有的物体冲突，所以分成2个Manager进行分别管理(太麻烦，看下面一条)
/// -或者是在Content下弄2个子物体，分别对应静态和动态：
///     -Dynamic对应用户自增物体，会进行增删减，由Controller进行管理
///     -Static适用于长期存在且能够编辑的物体（如架子、桌椅等）
/// </summary>
public sealed class AD_DefaultDecorationController : AD_SerializableItemControllerBase<AD_DefaultDecorationController, AD_SOPrefabInfo_DecorationItem, AD_DefaultDecorationItem, ItemInfo, AD_DecorationItemInfo, AD_SODefaultDecorationControllerConfig, AD_DefaultDecorationController.ConfigInfo>
    , IAD_DecorationController
{
    protected override ItemInfo ConvertFromBaseData(AD_DecorationItemInfo baseEleData)
    {
        var result = new ItemInfo(baseEleData);
        result.IsBaseType = true;
        return result;
    }

    public void AddElement(AD_SOPrefabInfoBase sOPrefabInfoBase, Vector3? initPosition = null)
    {
        if (!sOPrefabInfoBase)
            return;
        overridePrefab = sOPrefabInfoBase.prefab;
        AD_DefaultDecorationItem newInst = InitElement(new ItemInfo());//先临时创建默认Data
        AddElementToList(newInst);
        overridePrefab = null; //Reset

        //初始化Transform（参考Editor，仅设置位置，不设置旋转等）
        if (initPosition.HasValue)
            newInst.transform.position = initPosition.Value;
    }

    #region Override
    protected override void GetAllValidPrefabInfos_Matching(ItemInfo eleData, ref List<AD_SOPrefabInfo_DecorationItem> listSourcePrefabInfo, ref List<AD_SOPrefabInfo_DecorationItem> listTargetPrefabInfo)
    {
        listTargetPrefabInfo.AddRange(listSourcePrefabInfo);
    }

    protected override AD_SOPrefabInfo_DecorationItem GetFallbackPrefabInfo(ItemInfo eleData)
    {
        return Config.listSOPrefabInfo.FirstOrDefault();//返回首个普通（非SpecialFolder）元素
    }
    #endregion

    #region Define
    [Serializable]
    public class ConfigInfo : AD_SerializableItemControllerConfigInfoBase<AD_SOPrefabInfo_DecorationItem>
    {
    }
    #endregion
}
