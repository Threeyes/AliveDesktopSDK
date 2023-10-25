
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Threeyes.UI
{
    /// 参考结构： https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/toolstrip-control-architecture?view=netframeworkdesktop-4.8

    public class ToolStripDropDownItemInfo : ToolStripItemInfo
    {
        public virtual bool HasDropDownItemInfo { get { return listDropDownItemInfo.Count > 0; } }

        public List<ToolStripItemInfo> ListDropDownItemInfo { get { return listDropDownItemInfo; } set { listDropDownItemInfo = value; } }
        private List<ToolStripItemInfo> listDropDownItemInfo = new List<ToolStripItemInfo>();//PS:不能序列化，否则会出现无穷显示的问题（后期可通过特殊方式保存）


        public ToolStripDropDownItemInfo()
        {
        }
        public ToolStripDropDownItemInfo(string text, EventHandler onClick = null, Texture texture = null, string toolTipText = null)
            : base(text, onClick, texture, toolTipText)
        {
        }
        public ToolStripDropDownItemInfo(string text, Texture texture, params ToolStripItemInfo[] dropDownItems)
         : this(text, null, texture)
        {
            if (dropDownItems != null)
                ListDropDownItemInfo.AddRange(dropDownItems);
        }
    }

    /// <summary>
    /// 
    /// Represents a selectable option displayed on a MenuStrip or ContextMenuStrip.
    /// Ref: [ToolStripMenuItem] https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripmenuitem?view=netframework-4.8
    /// 
    /// PS：
    /// -单独成为一个数据类，方便独立提供给Modder使用
    /// </summary>
    public class ToolStripMenuItemInfo : ToolStripDropDownItemInfo
    {
        public ToolStripMenuItemInfo()
        {
        }

        public ToolStripMenuItemInfo(string text, EventHandler onClick = null, Texture texture = null, string toolTipText = null) : base(text, onClick, texture, toolTipText)
        {
        }
        public ToolStripMenuItemInfo(string text, Texture texture, params ToolStripItemInfo[] dropDownItems)
       : base(text, texture, dropDownItems)
        {
        }
    }
}