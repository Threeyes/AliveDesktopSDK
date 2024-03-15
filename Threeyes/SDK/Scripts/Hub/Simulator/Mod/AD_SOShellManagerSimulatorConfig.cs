using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using UnityEngine;
[CreateAssetMenu(menuName = AD_EditorDefinition.AssetMenuPrefix_Root_Simulator + "ShellManagerSimulatorConfig", fileName = "ShellManagerSimulatorConfig")]
public class AD_SOShellManagerSimulatorConfig : SOInstanceBase<AD_SOShellManagerSimulatorConfig, AD_SOShellManagerSimulatorConfig.SOInfo>
{
    public List<AD_ShellItemInfo> listShellItemInfo = new List<AD_ShellItemInfo>();

    public class SOInfo : SOInstacneInfo
    {
        public override string pathInResources { get { return "Threeyes"; } }
        public override string defaultName { get { return "ShellManagerSimulatorConfig"; } }
    }

}
