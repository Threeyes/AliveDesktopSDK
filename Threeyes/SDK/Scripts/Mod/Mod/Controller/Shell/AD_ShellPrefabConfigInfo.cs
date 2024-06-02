using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class AD_ShellPrefabConfigInfo : AD_PrefabConfigInfo<AD_ShellPrefabInfoCategory, AD_SOShellPrefabInfoGroup, AD_SOShellPrefabInfo>
{
    public AD_ShellPrefabConfigInfo()
    {
    }

    public AD_ShellPrefabConfigInfo(List<AD_ShellPrefabInfoCategory> listPrefabInfoCategory) : base(listPrefabInfoCategory) { }
    public AD_ShellPrefabConfigInfo(params AD_ShellPrefabInfoCategory[] listPrefabInfoCategory) : base(listPrefabInfoCategory.ToList()) { }

}
[System.Serializable]
public class AD_ShellPrefabInfoCategory : AD_PrefabInfoCategoryBase<AD_SOShellPrefabInfoGroup, AD_SOShellPrefabInfo>
{
    public AD_ShellPrefabInfoCategory() { }

    public AD_ShellPrefabInfoCategory(string title, List<AD_SOShellPrefabInfoGroup> listSOPrefabInfoGroup) : base(title, listSOPrefabInfoGroup) { }
}