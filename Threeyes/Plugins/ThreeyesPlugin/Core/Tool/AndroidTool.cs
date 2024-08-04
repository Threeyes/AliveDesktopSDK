using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Threeyes.Core
{
    public static class AndroidTool
    {
        public static AndroidJavaObject CurrentActivity
        {
            get
            {
                if (currentActivity == null)
                {
                    using (AndroidJavaObject unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
                    }
                }

                return currentActivity;
            }
        }
        private static AndroidJavaObject currentActivity = null;

        /// <summary>
        /// 显示气泡提示
        /// 
        /// Ref: https://agrawalsuneet.github.io/blogs/native-android-in-unity/
        /// </summary>
        /// <param name="content"></param>
        public static void ShowToast(string content)
        {
            //create a Toast class object
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");

            //create an array and add params to be passed
            object[] toastParams = new object[3];
            toastParams[0] = CurrentActivity;
            toastParams[1] = content;
            toastParams[2] = toastClass.GetStatic<int>("LENGTH_LONG");

            //call static function of Toast class, makeText
            AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", toastParams);

            //show toast
            toastObject.Call("show");
        }


        /// <summary>
        /// 分享文本到其他应用，可用于打开文件
        /// 
        /// PS：
        ///     -可以通过检查Application.isFocused是否为true，来判断是否完成
        ///     -相比直接提供文件的优点：不需要文件权限
        /// 
        /// Ref: 
        ///     -Sending Content to Other Apps: https://stuff.mit.edu/afs/sipb/project/android/docs/training/sharing/send.html
        ///     -分享文本: https://agrawalsuneet.github.io/blogs/native-android-text-sharing-in-unity/
        /// </summary>
        /// <param name="shareSubject"></param>
        /// <param name="shareMessage"></param>
        /// <returns></returns>
        public static void ShareText(string title, string shareMessage, string shareSubject = "")
        {
            //var title = "Share your high score";
            //var shareSubject = "I challenge you to beat my high score in Fire Block";
            //var shareMessage = "I challenge you to beat my high score in Fire Block. Get the Fire Block app from the link below. \nCheers\n\n http://onelink.to/fireblock";

            if (!Application.isEditor)
            {
                //Create intent for action send
                AndroidJavaClass intentClass =
                    new AndroidJavaClass("android.content.Intent");
                AndroidJavaObject intentObject =
                    new AndroidJavaObject("android.content.Intent");
                intentObject.Call<AndroidJavaObject>
                    ("setAction", intentClass.GetStatic<string>("ACTION_SEND"));

                //put text and subject extra
                intentObject.Call<AndroidJavaObject>("setType", "text/plain");

                if (shareSubject.NotNullOrEmpty())
                {
                    intentObject.Call<AndroidJavaObject>
                        ("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), shareSubject);
                }
                intentObject.Call<AndroidJavaObject>
                    ("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), shareMessage);

                //call createChooser method of activity class
                AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity =
                    unity.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject chooser =
                    intentClass.CallStatic<AndroidJavaObject>
                    ("createChooser", intentObject, title);
                currentActivity.Call("startActivity", chooser);
            }
        }
    }
}