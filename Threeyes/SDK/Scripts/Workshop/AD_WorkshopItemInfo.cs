using Newtonsoft.Json;
using Threeyes.Steamworks;

[System.Serializable]
[JsonObject(MemberSerialization.OptIn)]
public class AD_WorkshopItemInfo : WorkshopItemInfo<AD_WorkshopItemInfo>
{
    public AD_WorkshopItemInfo() : base() { }
}
