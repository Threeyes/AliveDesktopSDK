using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Core;
using UnityEngine;
using Threeyes.Persistent;
using UnityEngine.Events;
using System.Linq;

namespace Threeyes.Steamworks
{
    /// <summary>
    /// Change target renderer's material
    /// </summary>
    public class MaterialSwitchController : MaterialControllerBase<MaterialSwitchController, SOMaterialSwitchControllerConfig, MaterialSwitchController.ConfigInfo, MaterialSwitchController.PropertyBag>
    {
        #region Property & Field
        [Header("Target")]
        [SerializeField] protected Renderer targetRenderer;//Where the main material attached
        [SerializeField] protected int targetMaterialIndex = 0;
        [SerializeField] protected List<RendererMaterialInfo> listRendererMaterialInfo = new List<RendererMaterialInfo>();//其他模型的材质信息，方便针对多个使用了相同或类似材质的模型进行统一修改

        #endregion

        protected override void Awake()
        {
            base.Awake();

            Config.actionMaterialOptionChanged += OnMaterialOptionChanged;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Config.actionMaterialOptionChanged -= OnMaterialOptionChanged;
        }

        public override void UpdateSetting()
        {
            //ToDo:Init，以及根据curOptionMaterialIndex设置对应材质

            SetMaterialToRenderer(targetRenderer, targetMaterialIndex);

            //针对额外的Renderer进行修改（因为使用的材质可能不一致，仅仅是某些字段相同，所以不能直接用Material对其他Renderer进行替换）
            foreach (var rmInfo in listRendererMaterialInfo)
            {
                SetMaterialToRenderer(rmInfo.renderer, rmInfo.materialIndex);
            }
        }
        private void OnMaterialOptionChanged()
        {
            //ToDelete
        }

        #region Utility
        void SetMaterialToRenderer(Renderer renderer, int materialIndex)
        {
            if (!renderer)
                return;

            var materials = renderer.sharedMaterials.ToList();//使用共享材质，避免克隆原材质

            if (materialIndex <= materials.Count - 1)
            {
                materials[materialIndex] = Config.curOptionMaterial;
            }
            else
            {
                Debug.LogError($"Index out of bounds! {materialIndex} in {materials.Count}!");
            }
        }
        #endregion

        #region Define
        [Serializable]
        public class ConfigInfo : SerializableComponentConfigInfoBase
        {
            public UnityAction actionMaterialOptionChanged;

            [JsonIgnore] public Material curOptionMaterial;//Cur selected material
            [Tooltip("All optional material")][JsonIgnore] public List<Material> listOptionMaterial = new List<Material>();
            [PersistentOption(nameof(listOptionMaterial), nameof(curOptionMaterial))][PersistentValueChanged(nameof(OnOptionMaterialChanged))] public int curOptionMaterialIndex = 0;//提供下拉菜单选项

            #region Callback
            void OnOptionMaterialChanged(int oldValue, int newValue, PersistentChangeState persistentChangeState)
            {
                actionMaterialOptionChanged.Execute();
            }
            #endregion
        }
        public class PropertyBag : ConfigurableComponentPropertyBagBase<MaterialSwitchController, ConfigInfo> { }

        /// <summary>
        /// 针对其他模型
        /// 
        /// PS:
        /// -暂不复用MaterialController的类，方便以后按需添加字段
        /// </summary>
        [Serializable]
        public class RendererMaterialInfo
        {
            public Renderer renderer;
            public int materialIndex = 0;//对应的材质序号
        }

        #endregion
    }
}