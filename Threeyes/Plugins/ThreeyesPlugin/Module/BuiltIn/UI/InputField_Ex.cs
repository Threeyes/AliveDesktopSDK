using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Threeyes.BuiltIn
{
    /// <summary>
    /// 向TextMeshPro靠齐
    /// </summary>
    public class InputField_Ex : InputField
    {
        #region Property&Fields
        public SelectionEvent onSelect { get { return m_OnSelect; } set { SetPropertyUtility.SetClass(ref m_OnSelect, value); } }

        /// <summary>
        /// Event delegates triggered when the input field is focused.
        /// </summary>
        [SerializeField]
        private SelectionEvent m_OnSelect = new SelectionEvent();
        #endregion

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            SendOnFocus();
        }


        #region SendEvents
        protected void SendOnFocus()
        {
            if (onSelect != null)
                onSelect.Invoke(m_Text);
        }

        #endregion

        #region Define
        [Serializable]
        public class SelectionEvent : UnityEvent<string> { }
        #endregion
    }

    /// <summary>
    /// Ref: TMP_InputField.SetPropertyUtility
    /// </summary>
    static class SetPropertyUtility
    {
        public static bool SetColor(ref Color currentValue, Color newValue)
        {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetEquatableStruct<T>(ref T currentValue, T newValue) where T : IEquatable<T>
        {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }
    }

}