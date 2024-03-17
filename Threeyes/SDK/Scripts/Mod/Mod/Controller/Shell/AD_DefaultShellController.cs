using System;
using System.Collections.Generic;
using ItemInfo = AD_DefaultShellItem.ItemInfo;
using System.Linq;
using Threeyes.Core;

/// <summary>
/// 管理系统图标
/// 
/// ToAdd：
/// +将该组件的敏感代码（如GetListData）提炼成Manager
///     -改造方式1：通过Action回调等方式（参考InputTool）来为其提供操作，并且禁止用户自行调用回调。
///     -改造方式2：将GetListData改为SetListData，也就是主动给其赋予数据
///     +改造方式3【优先】：把所有执行代码放到Manager中（如Init、Refresh）
/// 该组件仍为Controller，仅提供Prefab以及生成点等信息，继承IConfigurableComponent接口
/// -【V2】可自定义待扫描的路径（常见特殊路径或自定义文件夹）（配置字段放在Setting中）
/// +如果扫描路径是桌面则需要额外扫描SpecialFolder
/// -为Item增加替换物体样式的功能
/// +从 SpawnPointProvider 中获取可以放置的位置
/// +单独增加一个配置类SOPrefabInfo，提供Prefab信息
/// -心跳检测检查回收站是否被清空，或者是隔一段时间调用以下刷新
/// </summary>
public sealed class AD_DefaultShellController : AD_ShellControllerBase<AD_DefaultShellController, AD_DefaultShellItem, ItemInfo, AD_ShellItemInfo, AD_SODefaultShellControllerConfig, AD_DefaultShellController.ConfigInfo>
    , IAD_ShellController
{
    #region Init
    protected override ItemInfo ConvertFromBaseData(AD_ShellItemInfo baseEleData)
    {
        var result = new ItemInfo(baseEleData);
        result.IsBaseType = true;
        return result;
    }

    /// <summary>
    /// 基于传入的最新ListData，对现有Item进行更新（增删减）
    /// 在外部有改动，或者读取还原时调用。
    ///
    /// PS:
    ///-ItemInfo重载了对比方法，原理是对比唯一的字段而不是引用 
    ////// </summary>
    public void RefreshBase(List<AD_ShellItemInfo> listNewItemInfo)
    {
        //#0 因为输入的信息里面有最新的数据，所以先清空现有实例的旧数据
        listElement.ForEach(e => e.data.DestroyRuntimeAssets());

        //#1 【删除】已经不存在的旧Item
        List<AD_DefaultShellItem> listWasteElement = listElement.FindAll(e => !listNewItemInfo.Any(newData => e.data.Equals(newData)));//【Warning】：针对TEleData比较，不能使用==，因为那是静态方法无法在继承中使用

        //UnityEngine.Debug.LogError($"Waste Count: {listWasteElement.Count}");
        listWasteElement.ForEach(e =>
        {
            listElement.Remove(e);
            e.gameObject.DestroyAtOnce();
        });

        ///#2 【更新】为所有现有Item.Init传入最新数据进行重新调用:
        ///     -传入图标等无法反序列化的信息
        ///     -重新链接Manager等数据
        foreach (var element in listElement)
        {
            AD_ShellItemInfo relatedItemInfo = listNewItemInfo.FirstOrDefault((newData) => element.data.Equals(newData));//查找匹配的数据
            if (relatedItemInfo != null)
            {
                InitData(element, ConvertFromBaseData(relatedItemInfo));
            }
        }

        //#3 【新增】创建尚未生成数据的对应实例
        List<AD_ShellItemInfo> listNotCreatedItemInfo = listNewItemInfo.FindAll(newData => !listElement.Exists(e => e.data.Equals(newData)));
        InitBase(listNotCreatedItemInfo, false);
    }
    #endregion

    #region PrefabInfo
    ///ToUpdate:
    ///-以下两个方法应该先考虑自身，然后再考虑现存的SDK及所有Mod内置信息（可以调用Manager的GetListPrefabConfigInfo）(或者把方法直接挪到Manager，然后把该Mod的PrefabConfigInfo放到首位)

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eleData"></param>
    /// <param name="listSourcePrefabInfo">源List</param>
    /// <param name="listTargetPrefabInfo">需要输出的list</param>
    protected override List<AD_SOShellPrefabInfo> GetAllValidPrefabInfos_Matching(ItemInfo eleData, List<AD_SOShellPrefabInfo> listSourcePrefabInfo)
    {
        List<AD_SOShellPrefabInfo> listTargetPrefabInfo = new List<AD_SOShellPrefabInfo>();
        //尝试匹配相同枚举的应用
        AD_ShellType dataShellType = eleData.ItemShellType;
        AD_SpeicalFolder dataSpeicalFolder = eleData.SpecialFolder;

        //#1 匹配ShellType
        listTargetPrefabInfo = listSourcePrefabInfo.FindAll(pl => pl.shellType.Has(dataShellType));

        //#2 在上面的筛选结果中，进一步匹配SpecialFolder
        if (dataShellType == AD_ShellType.SpecialFolder)
        {
            listTargetPrefabInfo = listTargetPrefabInfo.FindAll(pI => pI.speicalFolder.Has(dataSpeicalFolder));
        }
        return listTargetPrefabInfo;
    }
    protected override AD_SOShellPrefabInfo GetFallbackPrefabInfo(ItemInfo eleData)
    {
        AD_SOShellPrefabInfo result = null;
        List<AD_ShellPrefabConfigInfo> allPCI = AD_ManagerHolder.ShellManager.GetAllPrefabConfigInfo();
        foreach (var pCI in allPCI)
        {
            result = pCI.FindAllPrefabInfo().FirstOrDefault();//返回首个元素
            //result = pCI.FindAllPrefabInfo().FirstOrDefault(pl => pl.shellType != AD_ShellType.SpecialFolder);//返回首个普通（非SpecialFolder）元素

            if (result)
                return result;
        }

        return null;
    }
    #endregion

    #region Define
    [Serializable]
    public class ConfigInfo : AD_SerializableItemControllerConfigInfoBase<AD_SOShellPrefabInfo>
    {
        ///ToAdd：
        ///-与呈现相关的通用配置【V2】【可以移动到全局配置中】，如：
        ///     -显示图标文字（关闭可以更沉浸）（可以删掉，用下面的字段代替）
        ///     -仅在鼠标悬浮时显示文字（可以由Item自行实现）
        ///     -仅在全局文本中显示文字（可以由Item自行实现）
        ///     
        ///-与特殊文件夹相关，如：



    }
    #endregion
}