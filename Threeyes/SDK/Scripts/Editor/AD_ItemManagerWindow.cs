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
using System.Collections.Generic;
using System.Linq;
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


        //——验证方法——
        [MenuItem("Alive Desktop/Shell/Init/Static Object", true)]
        public static bool AD_Shell_Init_StaticValidate()
        {
            return Selection.objects.Any(obj => obj is GameObject);
        }
        [MenuItem("Alive Desktop/Shell/Init/Grabable Object", true)]
        public static bool AD_Shell_Init_GrabableValidate()
        {
            return Selection.objects.Any(obj => obj is GameObject);
        }
        [MenuItem("Alive Desktop/Shell/PrefabInfo/Create", true)]
        public static bool AD_Shell_PrefabInfo_CreateValidate()
        {
            return Selection.objects.Any(obj =>
            {
                if (EditorUtility.IsPersistent(obj) && obj is GameObject go)
                {
                    return go.GetComponent<IAD_ShellItem>() != null;
                }
                return false;
            });//至少有一个选中物体，挂载了指定组件
        }
        [MenuItem("Alive Desktop/Shell/PrefabInfo/Group Selected", true)]
        public static bool AD_Shell_PrefabInfo_GroupSelectedValidate()
        {
            return Selection.objects.Any(obj => obj is AD_SOShellPrefabInfo);//是指定类型文件
        }
        [MenuItem("Alive Desktop/Decoration/Init/Static Object", true)]
        public static bool AD_Decoration_Init_StaticValidate()
        {
            return Selection.objects.Any(obj => obj is GameObject);
        }
        [MenuItem("Alive Desktop/Decoration/Init/Grabable Object", true)]
        public static bool AD_Decoration_Init_GrabableValidate()
        {
            return Selection.objects.Any(obj => obj is GameObject);
        }
        [MenuItem("Alive Desktop/Decoration/PrefabInfo/Create", true)]
        public static bool AD_Decoration_PrefabInfo_CreateValidate()
        {
            return Selection.objects.Any(obj =>
            {
                if (EditorUtility.IsPersistent(obj) && obj is GameObject go)
                {
                    return go.GetComponent<IAD_DecorationItem>() != null;
                }
                return false;
            });//至少有一个选中物体，挂载了指定组件
        }
        [MenuItem("Alive Desktop/Decoration/PrefabInfo/Group Selected", true)]
        public static bool AD_Decoration_PrefabInfo_GroupSelectedValidate()
        {
            return Selection.objects.Any(obj => obj is AD_SODecorationPrefabInfo);//是指定类型文件
        }

        //——Quick Setup Scene——
        [MenuItem("Alive Desktop/Shell/Init/Static Object", priority = 201)]
        public static void AD_Shell_Init_Static()
        {
            foreach (var go in Selection.gameObjects)
                InitSelectFunc<AD_DefaultShellItem>(go, false);
        }
        [MenuItem("Alive Desktop/Shell/Init/Grabable Object", priority = 202)]
        public static void AD_Shell_Init_Grabable()
        {
            foreach (var go in Selection.gameObjects)
                InitSelectFunc<AD_DefaultShellItem>(go, true);
        }
        [MenuItem("Alive Desktop/Shell/PrefabInfo/Create", priority = 205)]
        public static void AD_Shell_PrefabInfo_Create()
        {
            CreateSOPrefabInfoFunc<IAD_ShellItem, AD_SOShellPrefabInfo>();
        }
        [MenuItem("Alive Desktop/Shell/PrefabInfo/Group Selected", priority = 206)]
        public static void AD_Shell_PrefabInfo_GroupSelected()
        {
            AD_SOShellPrefabInfoGroup.CreateUsingSelectedObjects();
        }

        /// <summary>
        /// 不可交互的装饰，如墙壁（不包括Rigidbody和AD_XRGrabInteractable）
        /// 
        /// PS:
        /// -纯装饰的物体层级要简单，没有Model等中间层，避免多余性能消耗
        /// </summary>
        [MenuItem("Alive Desktop/Decoration/Init/Static Object", priority = 211)]
        public static void AD_Decoration_Init_Static()
        {
            foreach (var go in Selection.gameObjects)
                InitSelectFunc<AD_DefaultDecorationItem>(go, false);
        }
        /// <summary>
        /// 将选中的场景物体或Prefab 设置为【可抓取的】装饰品
        /// </summary>
        [MenuItem("Alive Desktop/Decoration/Init/Grabable Object", priority = 212)]
        public static void AD_Decoration_Init_Grabable()
        {
            foreach (var go in Selection.gameObjects)
                InitSelectFunc<AD_DefaultDecorationItem>(go, true);
        }
        /// <summary>
        /// 基于选中的Preafb，生成对应的PrefabInfo
        /// </summary>
        [MenuItem("Alive Desktop/Decoration/PrefabInfo/Create", priority = 215)]
        public static void AD_Decoration_PrefabInfo_Create()
        {
            CreateSOPrefabInfoFunc<IAD_DecorationItem, AD_SODecorationPrefabInfo>();
        }
        /// <summary>
        /// 把选中的PrefabInfo成组
        /// </summary>
        [MenuItem("Alive Desktop/Decoration/PrefabInfo/Group Selected", priority = 216)]
        public static void AD_Decoration_PrefabInfo_GroupSelected()
        {
            AD_SODecorationPrefabInfoGroup.CreateUsingSelectedObjects();
        }


        static void InitSelectFunc<TSerializableItem>(GameObject go, bool isGrabable = false)
            where TSerializableItem : Component, IAD_SerializableItem
        {
            Undo.RecordObject(go, "AD_InitSelect");//Bug:无效，待修复

            //#1 Add Components
            TSerializableItem aD_DefaultDecorationItem = go.AddComponentOnce<TSerializableItem>();
            RuntimeSerializable_GameObject runtimeSerialization_GameObject = go.AddComponentOnce<RuntimeSerializable_GameObject>();//管理物体的整体序列化数据
            go.AddComponentOnce<RuntimeSerializable_Transform>();//管理物体的Transform组件序列化数据
            aD_DefaultDecorationItem.RuntimeSerialization_GameObject = runtimeSerialization_GameObject;//Init Serialization Setting

            if (isGrabable)
            {
                go.AddComponentOnce<Rigidbody>();
                AD_XRGrabInteractable aD_XRGrabInteractable = go.AddComponentOnce<AD_XRGrabInteractable>();
                aD_XRGrabInteractable.useDynamicAttach = true;//Allow smooth grab
            }

            EditorUtility.SetDirty(go);
        }

        const string prefabInfoDirName = "PrefabInfo";
        static void CreateSOPrefabInfoFunc<TItem, TSOPrefabInfo>()
           where TSOPrefabInfo : AD_SOPrefabInfo
        {
            ///PS:
            ///-只创建SO，不创建SOGroup，因为用户可能会使用一个Group来包含多个文件夹中的SO
            List<TSOPrefabInfo> listResult = new List<TSOPrefabInfo>();

            foreach (var go in Selection.gameObjects)
            {
                if (!EditorUtility.IsPersistent(go))//排除非资源Prefab物体
                    continue;

                GameObject goRootPrefab = go.transform.root.gameObject;//查找根Prefab
                if (goRootPrefab.GetComponent<TItem>() == null)//忽略没有挂载指定组件的物体
                {
                    Debug.LogError($"{goRootPrefab.name} doesn't have the required component {nameof(TItem)}! Will not create the related PrefabInfo!");
                    continue;
                }

                string absPrefabFilePath = EditorPathTool.GetAssetAbsPath(goRootPrefab);
                FileInfo fileInfo_Prefab = new FileInfo(absPrefabFilePath);

                string outputDir = fileInfo_Prefab.Directory.FullName + "/" + prefabInfoDirName; //暂时存在Prefab文件夹的PreabInfo文件夹下，用户可自行挪动到其他位置
                PathTool.GetOrCreateDir(outputDir);

                string assetPackPath = EditorPathTool.AbsToUnityRelatePath(outputDir + "/" + goRootPrefab.name + ".asset");
                TSOPrefabInfo soInst = AssetDatabase.LoadAssetAtPath<TSOPrefabInfo>(assetPackPath);
                //bool hasCreated = false;
                if (soInst == null)
                {
                    TSOPrefabInfo soInstTemp = CreateInstance<TSOPrefabInfo>();
                    AssetDatabase.CreateAsset(soInstTemp, assetPackPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    soInst = AssetDatabase.LoadAssetAtPath<TSOPrefabInfo>(assetPackPath);//重新加载Assets中的文件，便于后续被选中
                    //hasCreated = true;
                }
                soInst.Prefab = goRootPrefab;
                soInst.InitAfterPrefab();
                //Debug.Log($"{(hasCreated ? "Create" : "Update")} {goRootPrefab.name}'s SOPreabInfo at path: {outputDir}");

                listResult.Add(soInst);
            }
            Selection.objects = listResult.ToArray();//PS:可能EditorUI、报错（ArgumentOutOfRangeException: Index was out of range.），但程序正常运行。遇到此问题可以重置UnityEditor的Layout
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