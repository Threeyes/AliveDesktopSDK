#if UNITY_EDITOR
using UMod.BuildEngine;
using UnityEditor;
using UMod.Shared;
using Threeyes.Steamworks;
using Threeyes.RuntimeSerialization;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using Threeyes.Core;
using Threeyes.RuntimeEditor;
using Threeyes.Core.Editor;
using System.IO;
using Threeyes.Common;

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
        #region Override
        EnumField enumFieldItemType;

        protected override void InitUXMLField()
        {
            base.InitUXMLField();

            enumFieldItemType = rootVisualElement.Q<EnumField>("ItemTypeEnumField");
            enumFieldItemType.RegisterCallback<ChangeEvent<System.Enum>>(OnItemTypeEnumFieldChanged);
        }
        void OnItemTypeEnumFieldChanged(ChangeEvent<Enum> evt)
        {
            RefreshItemInfoGroupUIState();//当Item切换模式时，需要刷新状态（主要是根据ItemType，决定有效的条件）
        }

        protected override void RefreshItemInfoGroupUIState()
        {
            base.RefreshItemInfoGroupUIState();

            //当为非Scene时，“Create Scene"为灰色不可点击（或者改为创建测试场景）
            if (curSOWorkshopItemInfo)
            {
                buttonEditScene.SetInteractable(curSOWorkshopItemInfo.itemType == AD_WSItemType.Scene);
            }
        }

        protected override void AfterCreateItem(AD_SOWorkshopItemInfo infoInst)
        {
            base.AfterCreateItem(infoInst);
            CreateOrUpdateAssetPack(infoInst);//Item创建完成后，需要生成默认的AssetPack
        }
        #endregion

        #region Screenshot
        GameObject tempGOXRController;
        AD_XRManagerSimulator tempXRManagerSimulator;
        protected override void OnBeforeCreateScreenshot()
        {
            base.OnBeforeCreateScreenshot();

            //临时隐藏XRManager
            tempXRManagerSimulator = GameObject.FindObjectOfType<AD_XRManagerSimulator>();
            if (tempXRManagerSimulator)
                tempXRManagerSimulator.TempShowControllers(false);
        }
        protected override void OnAfterCreateScreenshot()
        {
            base.OnAfterCreateScreenshot();

            if (tempXRManagerSimulator)
                tempXRManagerSimulator.TempShowControllers(true);
        }
        #endregion

        #region MenuItem

        //[UnityEditor.MenuItem("Alive Desktop/Export Settings", priority = 44)]//【ToDelete】
        //internal static void Menu_Export_Settings()
        //{
        //    ModToolsUtil.ShowToolsWindow(typeof(UMod.Exporter.SettingsWindow));
        //}
        [MenuItem("Alive Desktop/Item Manager", priority = 0)]
        public static void AD_OpenWindow()
        {
            OpenWindow();
        }
        [MenuItem("Alive Desktop/Add Simulator Scene", priority = 1)]//ToUpdate：只有当前item为场景Mod时才有效
        public static void AD_RunCurSceneWithSimulator()
        {
            RunCurSceneWithSimulator();
        }

        //——Change Assets——
        [MenuItem("Alive Desktop/Set PlatformMode/PC", priority = 100)]
        public static void AD_SetPlatformModeToPC()
        {
            AD_SOEditorSettingManager.Instance.PlatformMode = AD_PlatformMode.PC;
        }

        [MenuItem("Alive Desktop/Set PlatformMode/VR", priority = 101)]
        public static void AD_SetPlatformModeToVR()
        {
            AD_SOEditorSettingManager.Instance.PlatformMode = AD_PlatformMode.PCVR;
        }
        [MenuItem("Alive Desktop/Update Cur Item's AssetPack", priority = 102)]
        public static void AD_CreateOrUpdateAssetPack()
        {
            AD_SOWorkshopItemInfo workshopItemInfo = SOManagerInst.CurWorkshopItemInfo;
            if (!workshopItemInfo)
                return;

            CreateOrUpdateAssetPack(workshopItemInfo);
        }

        //——Quick Setup Scene——

        /// <summary>
        /// 不可交互的装饰，如墙壁（不包括Rigidbody和AD_XRGrabInteractable）
        /// 
        /// PS:
        /// -纯装饰的物体层级要简单，没有Model等中间层，避免多余性能消耗
        /// </summary>
        [MenuItem("Alive Desktop/Init Select/Static Decoration", priority = 201)]
        public static void AD_InitSelectAsStaticDecoration()
        {
            foreach (var go in Selection.gameObjects)
            {
                //#1 Add Components
                AD_DefaultDecorationItem aD_DefaultDecorationItem = go.AddComponentOnce<AD_DefaultDecorationItem>();
                RuntimeSerializable_GameObject runtimeSerialization_GameObject = go.AddComponentOnce<RuntimeSerializable_GameObject>();
                go.AddComponentOnce<RuntimeSerializable_Transform>();

                //Init Setting
                aD_DefaultDecorationItem.runtimeSerialization_GameObject = runtimeSerialization_GameObject;
            }
        }
        /// <summary>
        /// 将选中物体（Prefab或场景物体）设置为【可抓取的】装饰品
        /// </summary>
        [MenuItem("Alive Desktop/Init Select/Grabable Decoration", priority = 202)]
        public static void AD_InitSelectAsInteractableDecoration()
        {
            foreach (var go in Selection.gameObjects)
            {
                //#1 Add Components
                AD_DefaultDecorationItem aD_DefaultDecorationItem = go.AddComponentOnce<AD_DefaultDecorationItem>();
                RuntimeSerializable_GameObject runtimeSerialization_GameObject = go.AddComponentOnce<RuntimeSerializable_GameObject>();//确保编辑模式可选择
                go.AddComponentOnce<RuntimeSerializable_Transform>();

                //#Interactable
                go.AddComponentOnce<Rigidbody>();
                AD_XRGrabInteractable aD_XRGrabInteractable = go.AddComponentOnce<AD_XRGrabInteractable>();

                //Init Setting
                aD_DefaultDecorationItem.runtimeSerialization_GameObject = runtimeSerialization_GameObject;
                aD_XRGrabInteractable.useDynamicAttach = true;//Allow smooth grab
            }
        }


        /// <summary>
        /// 针对选中的Prefab，生成对应的SOPreabInfo
        /// </summary>
        [MenuItem("Alive Desktop/Create PrefabInfo/Shell", priority = 203)]
        public static void AD_CreatePrefabInfo_Shell()
        {
            AD_CreatePrefabInfoFunc<AD_SOShellPrefabInfo>();
        }
        [MenuItem("Alive Desktop/Create PrefabInfo/Decoration", priority = 204)]
        public static void AD_CreatePrefabInfo_Decoration()
        {
            AD_CreatePrefabInfoFunc<AD_SODecorationPrefabInfo>();
        }

        const string prefabInfoDirName = "PreabInfo";
        static void AD_CreatePrefabInfoFunc<TSOPrefabInfo>()
           where TSOPrefabInfo : SOPrefabInfo
        {
            foreach (var go in Selection.gameObjects)
            {
                if (!EditorUtility.IsPersistent(go))//排除非资源Prefab物体
                    continue;

                GameObject goRootPrefab = go.transform.root.gameObject;//查找根Prefab

                string absPrefabFilePath = EditorPathTool.GetAssetAbsPath(goRootPrefab);
                FileInfo fileInfo_Prefab = new FileInfo(absPrefabFilePath);

                string outputDir = fileInfo_Prefab.Directory.FullName + "/" + prefabInfoDirName; //暂时存在Prefab同级的PreabInfo文件夹下，用户可自行挪动到其他位置
                PathTool.GetOrCreateDir(outputDir);

                Debug.Log($"Create {goRootPrefab.name}'s SOPreabInfo at path: {outputDir}");

                string assetPackPath = EditorPathTool.AbsToUnityRelatePath(outputDir + "/" + goRootPrefab.name + ".asset");
                TSOPrefabInfo soInst = AssetDatabase.LoadAssetAtPath<TSOPrefabInfo>(assetPackPath);
                if (soInst == null)
                {
                    soInst = ScriptableObject.CreateInstance<TSOPrefabInfo>();
                    AssetDatabase.CreateAsset(soInst, assetPackPath);
                    AssetDatabase.SaveAssets();
                }
                soInst.Prefab = goRootPrefab;
                soInst.InitAfterPrefab();
            }
            AssetDatabase.Refresh();
        }


        //——验证方法——
        [MenuItem("Alive Desktop/Init Select/Static Decoration", true, priority = 201)]
        public static bool AD_InitSelectAsStaticDecorationValidate()
        {
            return Selection.activeGameObject != null;
        }
        [MenuItem("Alive Desktop/Init Select/Grabable Decoration", true, priority = 202)]
        public static bool AD_InitSelectAsInteractableDecorationValidate()
        {
            return Selection.activeGameObject != null;
        }
        [MenuItem("Alive Desktop/Create PrefabInfo/Shell", true, priority = 203)]
        public static bool AD_CreatePrefabInfo_ShellValidate()
        {
            return Selection.activeGameObject != null;
        }
        [MenuItem("Alive Desktop/Create PrefabInfo/Decoration", true, priority = 204)]
        public static bool AD_CreatePrefabInfo_DecorationValidate()
        {
            return Selection.activeGameObject != null;
        }
        //——Build & Run——

        [MenuItem("Alive Desktop/Build And Run %m", priority = 1001)]
        public static void AD_BuildAndRunCurItem()
        {
            BuildAndRunCurItem();
        }
        [MenuItem("Alive Desktop/Build All", priority = 1002)]
        public static void AD_BuildAll()
        {
            BuildAll();
        }

        [MenuItem("Alive Desktop/SDK Wiki", priority = 2000)]
        public static void AD_OpenSDKWiki()
        {
            OpenSDKWiki("https://github.com/Threeyes/" + SORuntimeSettingManager.Instance.productName + "SDK/wiki");
        }
        #endregion

        #region Utility

        /// <summary>
        /// 针对Item文件夹生成或更新SOAssetPack
        /// 
        /// ToUpdate：
        /// -移动到其他位置
        /// -针对文件类型，创建其他必须文件
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
            //打包前，针对Item文件夹生成或更新SOAssetPack
            AD_ItemManagerWindow.CreateOrUpdateAssetPack(soItemInfo);
        }
        public override void AfterBuild(ModBuildResult result, ref AD_SOWorkshopItemInfo sOWorkshopItemInfo)
        {
            if (result.BuiltMod != null)//避免因为取消打包导致为空
            {
                ModContent modContent = result.BuiltMod.GetModContentMask();
                {
                    sOWorkshopItemInfo.itemAdvance = modContent.Has(ModContent.Scripts) ? AD_WSItemAdvance.IncludeScripts : AD_WSItemAdvance.None;
                    EditorUtility.SetDirty(sOWorkshopItemInfo);
                }
            }
        }
    }
}
#endif