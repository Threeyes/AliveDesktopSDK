using Threeyes.Config;
using Threeyes.Persistent;
using Threeyes.Steamworks;
using UnityEngine;

[CreateAssetMenu(menuName = AD_EditorDefinition.AssetMenuPrefix_Root_Mod_Controller_XR + "DefaultXRControllerConfig", fileName = "DefaultXRControllerConfig")]
public class AD_SODefaultXRControllerConfig : SOConfigBase<AD_DefaultXRController.ConfigInfo>, IAD_SOXRControllerConfig
{ }