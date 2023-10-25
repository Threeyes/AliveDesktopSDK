using System.Collections;
using System.Collections.Generic;
using Threeyes.Config;
using Threeyes.RuntimeEditor;
using UnityEngine;

///ToMove
[System.Serializable]
[RuntimeEditorObject(MemberConsideration.OptIn)]
public class AD_DecorationItemInfo : AD_SerializableItemInfo
    , IAD_DecorationItemInfo
{
    public AD_DecorationItemInfo()
    {
    }
    public AD_DecorationItemInfo(AD_DecorationItemInfo otherInst)
    {
        CopyBaseMembersFromFunc(otherInst);
    }

    public override void CopyBaseMembersFrom(object otherInst)
    {
        if (otherInst is AD_DecorationItemInfo otherItemInfo)
            CopyBaseMembersFromFunc(otherItemInfo);
    }
    void CopyBaseMembersFromFunc(AD_DecorationItemInfo otherInst)
    {
        //ToAdd
    }


    public override void DestroyRuntimeAssets()
    {
        //ToAdd
    }
}
