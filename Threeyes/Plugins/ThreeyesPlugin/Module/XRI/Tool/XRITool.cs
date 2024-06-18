#if Threeyes_XRI
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Threeyes.Core;
namespace Threeyes.XRI
{
    public static class XRITool
    {
        /// <summary>
        /// 查找场景首个有效的TeleportationProvider
        /// </summary>
        public static TeleportationProvider teleportationProvider
        {
            get
            {
                if (m_TeleportationProvider == null)
                {
                    if (!ComponentLocatorUtilityEx<TeleportationProvider>.TryFindComponent(out m_TeleportationProvider))
                        return null;
                }
                return m_TeleportationProvider;
            }
        }
        static TeleportationProvider m_TeleportationProvider;

        class CacheLocomotionCallback
        {

        }
        /// <summary>
        /// 
        /// PS:
        /// -执行方：TeleportationProvider
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="matchOrientation">Set to None if you don't want to change the rotation</param>
        public static bool TeleportTo(Vector3 position, Quaternion rotation, MatchOrientation matchOrientation, Action<LocomotionSystem> beginLocomotion = null, Action<LocomotionSystem> endLocomotion = null)
        {
            //Ref：UnityEngine.XR.Content.Walkthrough.WalkthroughStep.SetCameraPosition的实现（其中MatchOrientation为None）

            //MatchOrientation matchOrientation = rotation.HasValue ? MatchOrientation.TargetUpAndForward : MatchOrientation.None;//PS:只有rotation有值才设置matchOrientation

            TeleportRequest request = new TeleportRequest()
            {
                requestTime = Time.time,
                matchOrientation = matchOrientation,

                destinationPosition = position,
                destinationRotation = rotation
            };

            Action<LocomotionSystem> actEndLocomotionEx = null;//使用Action进行封装
            if (endLocomotion != null)
            {
                actEndLocomotionEx =
                    (lS) =>
                    {
                        endLocomotion.TryExecute(lS);

                        //Remove Callback on complete
                        if (beginLocomotion != null)
                            teleportationProvider.beginLocomotion -= beginLocomotion;
                        if (actEndLocomotionEx != null)
                            teleportationProvider.endLocomotion -= actEndLocomotionEx;
                    };
            }

            if (beginLocomotion != null)
                teleportationProvider.beginLocomotion += beginLocomotion;
            if (actEndLocomotionEx != null)
                teleportationProvider.endLocomotion += actEndLocomotionEx;

            return teleportationProvider.QueueTeleportRequest(request);// if successfully queued. Otherwise, returns <see langword="false"/>
        }
    }
}
#endif