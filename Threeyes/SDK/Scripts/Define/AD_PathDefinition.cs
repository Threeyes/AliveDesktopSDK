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
    /// ————Setting【Program Setting】
    /// ————Log【存储各Mod的Log文件】
    /// ——————（Mod ID）
    /// ————Item【存储Mod的持久化数据】
    /// ——————（Mod ID）
    /// ————————Persistent
    /// ————Item_Local【存储Mod的持久化数据】【仅保存到本地，不上传】
    /// ——————（Mod ID）
    /// ————————Persistent
    /// ——————————Directory【存储不同文件夹的信息】
    /// ————System【System info】
    /// ——————Cursor
    /// ————————CursorTheme.json
}
