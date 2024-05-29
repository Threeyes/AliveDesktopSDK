using System;
using System.Collections;
using System.Collections.Generic;
using Threeyes.Config;
using Threeyes.Core;
using UnityEngine;
namespace Threeyes.Steamworks
{
    public abstract class MaterialControllerBase<TContainer, TSOConfig, TConfig, TPropertyBag> : ConfigurableComponentBase<TContainer, TSOConfig, TConfig, TPropertyBag>
        where TContainer : Component, IConfigurableComponent<TConfig>
        where TSOConfig : SOConfigBase<TConfig>
        where TConfig : SerializableComponentConfigInfoBase, new()
        where TPropertyBag : ConfigurableComponentPropertyBagBase<TContainer, TConfig>, new()
    {
    }


    /// <summary>
    /// 针对其他模型
    /// </summary>
    [Serializable]
    public class RendererMaterialInfo
    {
        public Renderer renderer;
        public int materialIndex = 0;//对应的材质序号
    }
}