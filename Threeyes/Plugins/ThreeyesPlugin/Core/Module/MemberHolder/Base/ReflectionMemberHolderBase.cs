using UnityEngine;
using System;
using System.Reflection;

namespace Threeyes.Core
{
    public abstract class ReflectionMemberHolderBase : MonoBehaviour
    {
        public static string emptyMemberName = "___";//占位，代表不选，用于EditorGUI
        public const BindingFlags defaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public Type TargetType { get { return Target ? Target.GetType() : null; } }
        public UnityEngine.Object Target { get { return target; } set { target = value; } }

        /// <summary>
        /// Target Script instance in Scene or Asset window 
        /// </summary>
        [SerializeField] protected UnityEngine.Object target;

        #region Utility

        /// <summary>
        /// 
        /// Todo:
        ///     -将GetMemberInfo弄成链式调用，而不是包含
        /// </summary>
        /// <typeparam name="TMemberInfo"></typeparam>
        /// <param name="actGetMemberInfo"></param>
        /// <param name="memberName"></param>
        /// <param name="bindingFlags"></param>
        /// <returns></returns>
        protected TMemberInfo GetMemberInfo<TMemberInfo>(Func<Type, string, BindingFlags, TMemberInfo> actGetMemberInfo, string memberName, BindingFlags bindingFlags = defaultBindingFlags)
            where TMemberInfo : MemberInfo
        {
            if (TargetType == null)
                return null;

            if (memberName == emptyMemberName || string.IsNullOrEmpty(memberName))//该字段没选择任意Member，不当报错
                return null;

            TMemberInfo memberInfo = actGetMemberInfo(TargetType, memberName, bindingFlags);
            if (memberInfo == null)
            {
                Debug.LogError("Can't find " + typeof(TMemberInfo) + " with name " + memberName + "in" + TargetType + "!");
            }
            return memberInfo;
        }
        #endregion
    }
}