#if UNITY_EDITOR
using UMod.BuildEngine;
using UnityEditor;
using UMod.Shared;
using Threeyes.Steamworks;
using Threeyes.RuntimeSerialization;

namespace Threeyes.AliveCursor.SDK.Editor
{
    /// <summary>
    ///
    /// UIBuilder注意：
    /// 1.（Bug）TextFiled/Label通过bingdingPath绑定ulong后会显示错误，因此暂时不显示ItemID（应该是官方的bug：https://forum.unity.com/threads/binding-ulong-serializedproperty-to-inotifyvaluechanged-long.1005417/）
    /// 2.ViewDataKey只对特定UI有效（PS：This key only really applies to ScrollView, ListView, and Foldout. If you give any of these a unique key (not enforced, but recommended （https://forum.unity.com/threads/can-someone-explain-the-view-data-key-and-its-uses.855145/）)）
    ///
    /// ToUpdate:
    /// 1.ChangeLog输入框只有上传成功后才清空
    /// </summary>
    public sealed class AD_ItemManagerWindow : ItemManagerWindow<AD_ItemManagerWindow, AD_ItemManagerWindowInfo, AD_SOEditorSettingManager, AD_SOWorkshopItemInfo, AD_WorkshopItemInfo>
    {
        #region MenuItem

        [UnityEditor.MenuItem("Alive Desktop/Export Settings", priority = 44)]//ToDelete
        internal static void Menu_Export_Settings()
        {
            ModToolsUtil.ShowToolsWindow(typeof(UMod.Exporter.SettingsWindow));
        }
        [MenuItem("Alive Desktop/Item Manager", priority = 0)]
        public static void AD_OpenWindow()
        {
            OpenWindow();
        }
        [MenuItem("Alive Desktop/Build And Run %m", priority = 1)]
        public static void AD_BuildAndRunCurItem()
        {
            BuildAndRunCurItem();
        }
        [MenuItem("Alive Desktop/Build All", priority = 2)]
        public static void AD_BuildAll()
        {
            BuildAll();
        }
        [MenuItem("Alive Desktop/Add Simulator Scene", priority = 3)]
        public static void AD_RunCurSceneWithSimulator()
        {
            RunCurSceneWithSimulator();
        }

        [MenuItem("Alive Desktop/CreateOrUpdate Cur AssetPack", priority = 100)]
        public static void AD_CreateOrUpdateAssetPack()
        {
            AD_SOWorkshopItemInfo workshopItemInfo = SOManagerInst.CurWorkshopItemInfo;
            if (!workshopItemInfo)
                return;

            CreateOrUpdateAssetPack(workshopItemInfo);
        }

        [MenuItem("Alive Desktop/SDK Wiki", priority = 1000)]
        public static void AD_OpenSDKWiki()
        {
            OpenSDKWiki("https://github.com/Threeyes/AliveCursorSDK/wiki");
        }
        #endregion

        #region Utility


        /// <summary>
        /// 针对Item文件夹生成或更新SOAssetPack
        /// ToUpdate：移动到其他位置
        /// </summary>
        /// <param name="workshopItemInfo"></param>
        public static void CreateOrUpdateAssetPack(AD_SOWorkshopItemInfo workshopItemInfo)
        {
            string itemDirPath = workshopItemInfo.ItemDirPath;
            string destAbsDirPath = workshopItemInfo.DataDirPath;
            SOAssetPack.CreateFromFolder(itemDirPath, destAbsDirPath);
        }
        #endregion
    }
    public class AD_ItemManagerWindowInfo : ItemManagerWindowInfo<AD_SOEditorSettingManager, AD_SOWorkshopItemInfo>
    {
        public override AD_SOEditorSettingManager SOEditorSettingManagerInst { get { return AD_SOEditorSettingManager.Instance; } }
        public override string WindowAssetPath { get { return "Layouts/AD_ItemManagerWindow"; } }

        public override void BeforeBuild(ref AD_SOWorkshopItemInfo soItemInfo)
        {
            //针对Item文件夹生成或更新SOAssetPack
            AD_ItemManagerWindow.CreateOrUpdateAssetPack(soItemInfo);
        }
        public override void AfterBuild(ModBuildResult result, ref AD_SOWorkshopItemInfo sOWorkshopItemInfo)
        {
            ModContent modContent = result.BuiltMod.GetModContentMask();
            {
                sOWorkshopItemInfo.itemSafety = modContent.Has(ModContent.Scripts) ? AD_WSItemAdvance.IncludeScripts : AD_WSItemAdvance.None;
                EditorUtility.SetDirty(sOWorkshopItemInfo);
            }
        }
    }
}
#endif