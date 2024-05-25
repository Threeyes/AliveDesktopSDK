#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using UnityEngine.Events;

namespace Threeyes.Core
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReflectionMethodHolderBase), true)]//editorForChildClasses
    public class InspectorView_ReflectionMethodHolder : InspectorView_ReflectionMemberHolder<ReflectionMethodHolderBase>
    {
        protected override void OnInspectorGUIFunc()
        {
            GUILayout.Space(5);

            Object targetObject = _target.Target;
            if (!targetObject)
                return;

            System.Type targetType = targetObject.GetType();
            bool isMemberInfoValid = false;

            List<string> listMethodOption = new List<string>() { ReflectionMemberHolderBase.emptyMemberName };
            foreach (MethodInfo methodInfo in targetType.GetAllMethods(ReflectionMemberHolderBase.defaultBindingFlags))
            {
                if (_target.IsDesireMethod(methodInfo))
                    listMethodOption.Add(methodInfo.Name);
            }
            DrawPopUp(target, new GUIContent("Method"), listMethodOption,
                () => _target.TargetSerializeMethodName,
                (val) => _target.TargetSerializeMethodName = val,
                ref isMemberInfoValid);

            if (!isMemberInfoValid)
            {
                EditorGUILayout.HelpBox(warningText_PleaseSetMemberInfo, MessageType.Warning);
            }
        }

        protected override void OnFieldTargetChanged()
        {
            _target.TargetSerializeMethodName = "";
        }
    }
}
#endif