using System.Collections;
using System.Collections.Generic;
using System.IO;
using Threeyes.Core;
using Threeyes.GameFramework;
using UnityEngine;

public class AD_SOWorkshopItemInfo : SOWorkshopItemInfo<AD_WorkshopItemInfo>
{
    public override AD_WorkshopItemInfo ItemInfo { get { return AD_WorkshopItemInfoFactory.Instance.Create(this); } }

    #region Property & Field
    //##Tags
    public AD_WSItemType itemType = AD_WSItemType.Scene;//Item类型(ToUpdate:弄成通用，放到基类中)
    public AD_WSItemAgeRating ageRatingType = AD_WSItemAgeRating.General;
    public AD_WSItemStyle itemStyle = AD_WSItemStyle.None;
    public AD_WSItemGenre itemGenre = AD_WSItemGenre.None;
    public AD_WSItemReference itemReference = AD_WSItemReference.None;
    public AD_WSItemFeature itemFeature = AD_WSItemFeature.None;

    [HideInInspector] public AD_WSItemAdvance itemAdvance = AD_WSItemAdvance.None;//(Edit after mod build completed)//仅作为筛选，运行时通过UMod来检查其是否带脚本
    #endregion

    #region Check State
    public override bool CheckIfBuildValid(out string errorLog)
    {
        bool isValid = base.CheckIfBuildValid(out errorLog);
        if (isValid)
        {
            ////——SOAssetPack——
            ///(ToUpdate:应该要移动到SOWorkshopItemInfo.Check State中，或者暂时不检测，因为都会自动创建)
            string filePath_SOAssetPack = GetSOAssetPackFilePath(itemName);
            if (!File.Exists(filePath_SOAssetPack))
            {
                errorLog = $"The AssetPack file does not exist in {filePath_SOAssetPack}!";
            }
        }
        return errorLog.IsNullOrEmpty();
    }
    #endregion

    #region Export Path
    //protected readonly string ItemModName_Model = "Model";//Model文件的名称
    //public override string ItemModName
    //{
    //    get
    //    {
    //        switch (itemType)
    //        {
    //            case AD_WSItemType.Scene:
    //                return base.ItemModName;
    //            case AD_WSItemType.Model:
    //                return itemName + "_" + ID.Guid;//为了支持同时加载，需要提供唯一的ID
    //            //return ItemModName_Model;
    //            default:
    //                Debug.LogError($"Type not define!");
    //                return "NotDefine";
    //        }
    //    }
    //}
    #endregion

    public AD_SOWorkshopItemInfo()
    {
        ageRatingType = AD_WSItemAgeRating.General;//避免ItemManagerWindow默认将该值设置为0
    }

    public override string[] Tags
    {
        get
        {
            List<string> listTag = new List<string>();
            listTag.Add(itemType.ToString());//类型，必须

            listTag.Add(ageRatingType.ToString());//必选唯一

            listTag.AddRange(itemStyle.GetNamesEx());
            listTag.AddRange(itemGenre.GetNamesEx());
            listTag.AddRange(itemReference.GetNamesEx());
            listTag.AddRange(itemFeature.GetNamesEx());
            listTag.AddRange(itemAdvance.GetNamesEx());

            return listTag.ToArray();
        }
    }

    protected override bool IsSceneFile { get { return itemType == AD_WSItemType.Scene; } }
}