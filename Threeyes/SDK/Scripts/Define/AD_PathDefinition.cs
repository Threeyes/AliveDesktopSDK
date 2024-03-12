using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AD_PathDefinition
{
    //——Save文件夹——
    ///
    ///PS：
    ///1.暂不支持以SteamID作为细分的存储，因为需求不多，避免增加复杂性
    ///2.对应的云存储文件夹为Item（SteamCloud存储路径（https://partner.steamgames.com/doc/features/cloud)）
    /// 目录实例：
    /// Data
    /// ——Save
    /// ————Item【存储Mod的持久化数据】
    /// ——————（Mod ID）
    /// ————————Persistent
    /// 
    /// ————Item_Local【存储Mod的持久化数据】【仅保存到本地，不上传】（PS：因为用户每台电脑的应用数量、类型不一致，且装饰有可能与应用绑定（如Socket），所以两者不应该进行云同步）
    /// ——————（Mod ID）
    /// ————————Persistent
    /// ——————————Directory【存储不同文件夹的信息】
    /// 
    /// ————Setting【Program Setting】
    /// 
    /// ————Log【存储各Mod的Log文件】
    /// ——————（Mod ID）
}
