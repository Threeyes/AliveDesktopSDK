using System.Collections;
using System.Collections.Generic;
using Threeyes.Steamworks;
using UnityEngine;

public class AD_SOWorkshopItemInfo : SOWorkshopItemInfo<AD_WorkshopItemInfo>
{
    public override AD_WorkshopItemInfo ItemInfo { get { return AD_WorkshopItemInfoFactory.Instance.Create(this); } }

    public AD_WSItemAgeRating ageRatingType = AD_WSItemAgeRating.General;
    public AD_WSItemStyle itemStyle = AD_WSItemStyle.None;
    public AD_WSItemGenre itemGenre = AD_WSItemGenre.None;
    public AD_WSItemReference itemReference = AD_WSItemReference.None;
    public AD_WSItemFeature itemFeature = AD_WSItemFeature.None;

    [HideInInspector] public AD_WSItemAdvance itemSafety = AD_WSItemAdvance.None;

    public override string[] Tags
    {
        get
        {
            List<string> listTag = new List<string>();
            listTag.Add(ageRatingType.ToString());//必选唯一

            listTag.AddRange(itemStyle.GetNamesEx());
            listTag.AddRange(itemGenre.GetNamesEx());
            listTag.AddRange(itemReference.GetNamesEx());
            listTag.AddRange(itemFeature.GetNamesEx());
            listTag.AddRange(itemSafety.GetNamesEx());

            return listTag.ToArray();
        }
    }
}
