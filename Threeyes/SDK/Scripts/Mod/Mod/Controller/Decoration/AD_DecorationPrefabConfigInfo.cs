using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[System.Serializable]
public class AD_DecorationPrefabConfigInfo : AD_PrefabConfigInfo<AD_DecorationPrefabInfoCategory, AD_SODecorationPrefabInfoGroup, AD_SODecorationPrefabInfo>
{
    public AD_DecorationPrefabConfigInfo() { }

    public AD_DecorationPrefabConfigInfo(List<AD_DecorationPrefabInfoCategory> listPrefabInfoCategory) : base(listPrefabInfoCategory) { }

    public AD_DecorationPrefabConfigInfo(params AD_DecorationPrefabInfoCategory[] listPrefabInfoCategory) : base(listPrefabInfoCategory.ToList()) { }

}
[System.Serializable]
public class AD_DecorationPrefabInfoCategory : AD_PrefabInfoCategoryBase<AD_SODecorationPrefabInfoGroup, AD_SODecorationPrefabInfo>
{
    public AD_DecorationPrefabInfoCategory() { }

    public AD_DecorationPrefabInfoCategory(string title, List<AD_SODecorationPrefabInfoGroup> listSOPrefabInfoGroup) : base(title, listSOPrefabInfoGroup) { }
}