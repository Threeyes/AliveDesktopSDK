using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IAD_ShellItemInfo : IAD_SerializableItemInfo
{
    public string Name { get; }
    AD_ShellType DestShellType { get; }
    public AD_ShellType ItemShellType { get; }
    public AD_ShellType TargetShellType { get; }
    public AD_SpeicalFolder SpecialFolder { get; }
    public string Path { get; }
    public string TargetPath { get; }
    public Texture2D TexturePreview { get; }

    //#Runtime
    public string DisplayName { get; }
    public string NameWithoutExtension { get; }
}