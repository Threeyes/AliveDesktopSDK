using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace Threeyes.Core
{
    /// <summary>
    /// Using reflection to call methods with specified parameters
    /// 
    /// Todo:
    /// -能够绑定0~1个参数（可以拆分为不同子类），外部能够通过唯一方法调用（避免uMod的重名绑定bug）
    /// </summary>
    public abstract class ReflectionMethodHolderBase : ReflectionMemberHolderBase
    {
        public string TargetSerializeMethodName { get { return targetSerializeMethodName; } set { targetSerializeMethodName = value; } }
        [SerializeField] protected string targetSerializeMethodName;

        public abstract bool IsDesireMethod(MethodInfo methodInfo);
        MethodInfo targetMethodInfo { get { return GetMemberInfo((type, name, bf) => type.GetMethods(bf).FirstOrDefault(mI => mI.Name == name && IsDesireMethod(mI)), targetSerializeMethodName); } }

        static object[] arrObj_Empty = new object[] { };

        /// <summary>
        /// Invoke target method
        /// </summary>
        public void InvokeTarget()
        {
            if (targetMethodInfo != null)
            {
                try
                {
                    targetMethodInfo.Invoke(Target, arrObj_Empty);
                }
                catch (Exception e)
                {
                    Debug.LogError("Invoke target method with error: \r\n" + e);
                }
            }
            else
            {
                if (!target || targetSerializeMethodName == emptyMemberName)//有部分信息无效
                    Debug.LogError("Please set all necessary member information first!");
                else
                    Debug.LogError($"Can't find target method {targetSerializeMethodName} on Object {target}!");
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValue">Param value</typeparam>
    public abstract class ReflectionMethodHolderBase<TValue> : ReflectionMethodHolderBase
    {

    }

}