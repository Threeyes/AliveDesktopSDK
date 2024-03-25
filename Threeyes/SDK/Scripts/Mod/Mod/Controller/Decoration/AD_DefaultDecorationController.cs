using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Threeyes.Core;
using UnityEngine;
using static AD_DefaultDecorationItem;
/// <summary>
/// PS：
/// -为了避免反序列化时预制物与已有的物体冲突，所以分成2个Manager进行分别管理(太麻烦，看下面一条)
/// -或者是在Content下弄2个子物体，分别对应静态和动态：
///     -Dynamic 对应用户自增物体，会进行增删减，由Controller进行管理
///     -Preset 适用于提前布置且能够编辑、删除的物体（如架子、桌椅等）
///     -如果是不可更改的静态物体，应该去掉所有可交互组件，并作为场景的的一部分
/// </summary>
public sealed class AD_DefaultDecorationController : AD_SerializableItemControllerBase<AD_DefaultDecorationController, AD_SODecorationPrefabInfoGroup, AD_SODecorationPrefabInfo, AD_DefaultDecorationItem, ItemInfo, AD_DecorationItemInfo, AD_SODefaultDecorationControllerConfig, AD_DefaultDecorationController.ConfigInfo>
    , IAD_DecorationController
{
    protected override ItemInfo ConvertFromBaseData(AD_DecorationItemInfo baseEleData)
    {
        var result = new ItemInfo(baseEleData);
        result.IsBaseType = true;
        return result;
    }

    public void AddElement(GameObject prefab, Vector3? initPosition = null, Quaternion? initRotation = null)
    {
        if (!prefab)
            return;

        overridePrefab = prefab;
        AD_DefaultDecorationItem newInst = InitElement(new ItemInfo());//先临时创建默认Data
        AddElementToList(newInst);
        overridePrefab = null; //Reset

        //初始化Transform（参考Editor，仅设置位置，不设置旋转等）
        newInst.transform.SetProperty(initPosition, initRotation, isLocalSpace: false);//PS：SetProperty方法可避免刚体物体在初始化修改其位置/缩放时出错
    }

    #region Define
    [Serializable]
    public class ConfigInfo : AD_SerializableItemControllerConfigInfoBase<AD_SODecorationPrefabInfo>
    {
    }
    #endregion
}
