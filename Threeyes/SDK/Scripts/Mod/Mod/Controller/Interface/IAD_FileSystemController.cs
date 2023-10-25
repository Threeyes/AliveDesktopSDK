using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAD_FileSystemController : IAD_SerializableItemController<AD_FileSystemItemInfo>
{
    void RefreshBase(List<AD_FileSystemItemInfo> listNewItemInfo);
}