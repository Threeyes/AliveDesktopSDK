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
    #region IAD_DecorationController
    public GameObject CreateElement(GameObject prefab, Vector3? initPosition = null, Quaternion? initRotation = null, Action<GameObject> actionCreateCompleted = null)
    {
        if (!prefab)//避免因为卸载模型Mod导致引用丢失
            return null;

        //# Create
        overridePrefab = prefab;
        var data = new ItemInfo();
        AD_DefaultDecorationItem newInst = CreateElementFunc(data);//先临时创建默认Data
        actionCreateCompleted.TryExecute(newInst.gameObject);//创建完成后的回调，可用已有数据进行反序列化
       
        //# Init
        newInst.transform.SetProperty(initPosition, initRotation, isLocalSpace: false);//初始化Transform（参考Editor，仅设置位置，不设置旋转等）。PS：SetProperty方法可避免刚体物体在初始化修改其位置/缩放时出错
        InitData(newInst, data);//InitData会初始化IRuntimeEditable，并基于RS的数据初始化配置
        AddElementToList(newInst);

        //#Reset
        overridePrefab = null;

        return newInst.gameObject;
    }

    public void DeleteElement(IAD_DecorationItem item)
    {
        DeleteElementFunc(item as AD_DefaultDecorationItem);
    }
    void DeleteElementFunc(AD_DefaultDecorationItem element)
    {
        if (!element)
            return;
        listElement.Remove(element);
        element.gameObject.DestroyAtOnce();
    }
    #endregion

    protected override ItemInfo ConvertFromBaseData(AD_DecorationItemInfo baseEleData)
    {
        var result = new ItemInfo(baseEleData);
        result.IsBaseType = true;
        return result;
    }

    #region Define
    [Serializable]
    public class ConfigInfo : AD_SerializableItemControllerConfigInfoBase<AD_SODecorationPrefabInfo>
    {
    }
    #endregion
}
