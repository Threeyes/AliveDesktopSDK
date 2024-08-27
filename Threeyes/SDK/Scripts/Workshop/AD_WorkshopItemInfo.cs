using Newtonsoft.Json;
using Threeyes.GameFramework;
using UnityEngine;
using System.Linq;
[System.Serializable]
[JsonObject(MemberSerialization.OptIn)]
public class AD_WorkshopItemInfo : WorkshopItemInfo<AD_WorkshopItemInfo>
{
    //#ItemType
    public bool IsScene { get { return tags.Contains(AD_WSItemType.Scene.ToString()); } }
    public bool IsModel { get { return tags.Contains(AD_WSItemType.Model.ToString()); } }

    public AD_WorkshopItemInfo() : base() { }
}
