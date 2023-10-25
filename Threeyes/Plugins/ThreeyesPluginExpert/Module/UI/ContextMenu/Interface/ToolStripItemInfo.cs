using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Threeyes.UI
{
    [Serializable]
    /// <summary>
    /// 工具栏控件信息
    /// Represents the abstract base class that manages events and layout for all the elements that a ToolStrip or ToolStripDropDown can contain.
    /// 
    ///ToAdd:
    ///-int Order(方便排序和归类，参考EventPlayer的Hierarchy菜单)
    ///
    /// PS:
    /// -因为是单独把数据抽出来，因此要在类名后增加Info
    /// Ref: [ToolStripItem]https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripitem?view=netframework-4.8
    /// </summary>
    public class ToolStripItemInfo
    {
        public string ToolTipText { get { return toolTipText; } set { toolTipText = value; } }
        public Texture Texture { get { return texture; } set { texture = value; } }//代替Image
        public string Text { get { return text; } set { text = value; } }//显示内容

        public event EventHandler Click { add { click += value; } remove { click -= value; } }
        EventHandler click;
        [SerializeField] private string toolTipText;
        [SerializeField] private Texture texture;
        [SerializeField] private string text;

        /// <summary>
        /// 
        /// PS：两个参数的值暂为空，请勿使用！
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FireEvent(object sender, EventArgs e)
        {
            if (click != null)
                click.Invoke(sender, e);
        }
        public ToolStripItemInfo()
        {
        }

        /// <summary>
        /// 单层菜单
        /// </summary>
        /// <param name="text"></param>
        /// <param name="onClick"></param>
        /// <param name="texture"></param>
        /// <param name="toolTipText"></param>
        public ToolStripItemInfo(string text, EventHandler onClick = null, Texture texture = null, string toolTipText = null)
        {
            Text = text;
            if (onClick != null)
            {
                Click += onClick;
            }

            //# Optional
            Texture = texture;
            ToolTipText = toolTipText;
        }
    }

    /// <summary>
    /// 代表分割线
    /// 
    /// Ref: [ToolStripSeparator](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripseparator?view=windowsdesktop-7.0)
    /// </summary>
    [Serializable]
    public class ToolStripSeparatorInfo : ToolStripItemInfo
    {
    }

}