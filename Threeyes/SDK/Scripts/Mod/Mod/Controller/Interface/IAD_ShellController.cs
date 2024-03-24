using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAD_ShellController : IAD_SerializableItemController<AD_ShellItemInfo>
{
    void RefreshBase(List<AD_ShellItemInfo> listNewItemInfo);
}