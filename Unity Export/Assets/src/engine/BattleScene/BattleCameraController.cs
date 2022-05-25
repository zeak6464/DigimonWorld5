using System;
using System.Collections.Generic;

namespace Yukar.Engine
{
    internal class BattleCameraController
    {
        Common.Rom.ThirdPersonCameraSettings nowParam = new Common.Rom.ThirdPersonCameraSettings();
        float nowCount = -1;
        internal class CameraControlEntry
        {
            // 実行前にセットされる
            internal Common.Rom.ThirdPersonCameraSettings startParam;

            // プッシュしたときにセットされる
            internal Common.Rom.ThirdPersonCameraSettings endParam;
            internal float endTime;
            internal Common.Rom.GameSettings.BattleCamera.TweenType tweenType =
                Common.Rom.GameSettings.BattleCamera.TweenType.EASE_OUT;
            internal string tag;
        }
        private List<CameraControlEntry> cameraQueue = new List<CameraControlEntry>();
        public float defaultHeight;
        public static string TAG_FORCE_WAIT = "WAIT";

        internal BattleCameraController()
        {
            nowParam = new Common.Rom.ThirdPersonCameraSettings();
        }

        internal BattleCameraController(Common.Rom.ThirdPersonCameraSettings initialParam)
        {
            nowParam.copyFrom(initialParam);
        }

        internal void setMapCenterHeight(float p)
        {
            defaultHeight = p;
        }

        internal void push(Common.Rom.ThirdPersonCameraSettings param, float time,
            Common.Rom.GameSettings.BattleCamera.TweenType type = Common.Rom.GameSettings.BattleCamera.TweenType.EASE_OUT, string newTag = "")
        {
            var end = new Common.Rom.ThirdPersonCameraSettings();
            end.copyFrom(param);
            end.height += defaultHeight;
            cameraQueue.Add(new CameraControlEntry() { endParam = end, endTime = time, tweenType = type, tag = newTag });
        }

        internal void push(Common.Rom.ThirdPersonCameraSettings param, float time, BattleActor actor,
            Common.Rom.GameSettings.BattleCamera.TweenType type = Common.Rom.GameSettings.BattleCamera.TweenType.EASE_OUT, string newTag = "")
        {
            var end = new Common.Rom.ThirdPersonCameraSettings();
            end.copyFrom(param);
            var target = actor.getPos();
            end.x = target.x;
            end.height += target.y + actor.Height / 2;
            end.y = target.z;
            if(actor.frontDir < 0)
                end.yAngle = (end.yAngle + 180) % 360;
            cameraQueue.Add(new CameraControlEntry() { endParam = end, endTime = time, tweenType = type, tag = newTag });
        }

        internal void push(Common.Rom.ThirdPersonCameraSettings param, float time, SharpKmyMath.Vector3 target, float yAngle,
            Common.Rom.GameSettings.BattleCamera.TweenType type = Common.Rom.GameSettings.BattleCamera.TweenType.EASE_OUT, string newTag = "")
        {
            var end = new Common.Rom.ThirdPersonCameraSettings();
            end.copyFrom(param);
            end.x = target.x;
            end.height += target.y;
            end.y = target.z;
            end.yAngle = yAngle;
            cameraQueue.Add(new CameraControlEntry() { endParam = end, endTime = time, tweenType = type, tag = newTag });
        }

        internal void set(Common.Rom.ThirdPersonCameraSettings param, float time,
            Common.Rom.GameSettings.BattleCamera.TweenType type = Common.Rom.GameSettings.BattleCamera.TweenType.EASE_OUT, string newTag = "")
        {
            clearQueue();
            push(param, time, type, newTag);
        }

        private void clearQueue()
        {
            cameraQueue.Clear();
            nowCount = -1;
        }

        internal void set(Common.Rom.ThirdPersonCameraSettings param, float time, BattleActor actor)
        {
            clearQueue();
            push(param, time, actor);
        }

        internal void set(Common.Rom.ThirdPersonCameraSettings param, float time, SharpKmyMath.Vector3 target, float yAngle)
        {
            clearQueue();
            push(param, time, target, yAngle);
        }

        internal Common.Rom.ThirdPersonCameraSettings Now
        {
            get { return nowParam; }
        }

        internal string CurrentTag
        {
            get
            {
                if (cameraQueue.Count > 0)
                    return cameraQueue[0].tag;

                return "";
            }
        }

        internal void update()
        {
            if (nowCount == -1 && cameraQueue.Count > 0)
            {
                cameraQueue[0].startParam = new Common.Rom.ThirdPersonCameraSettings();
                cameraQueue[0].startParam.copyFrom(nowParam);
                nowCount = 0;
                
                // 回転軸は近い方を採用する
                float diffY = cameraQueue[0].startParam.yAngle - cameraQueue[0].endParam.yAngle;
                float absY = Math.Abs(diffY);
                if (absY > 180)
                    cameraQueue[0].endParam.yAngle += diffY * 360 / absY;
            }

            if (nowCount >= 0) { 
                nowCount += GameMain.getRelativeParam60FPS();
                var startParam = cameraQueue[0].startParam;
                var endParam = cameraQueue[0].endParam;

                float delta = nowCount / cameraQueue[0].endTime;

                switch (cameraQueue[0].tweenType)
                {
                    case Common.Rom.GameSettings.BattleCamera.TweenType.EASE_OUT:
                        delta = 1f - (1f - delta) * (1f - delta);
                        break;
                    case Common.Rom.GameSettings.BattleCamera.TweenType.EASE_IN:
                        delta = delta * delta;
                        break;
                    case Common.Rom.GameSettings.BattleCamera.TweenType.EASE_IN_OUT:
                        if (delta < 0.5f)
                        {
                            delta *= 2;
                            delta = delta * delta;
                            delta /= 2;
                        }
                        else
                        {
                            delta = (delta - 0.5f) * 2;
                            delta = 1f - (1f - delta) * (1f - delta);
                            delta = (delta / 2) + 0.5f;
                        }
                        break;
                }
                float inverted = 1f - delta;

                if (nowCount < cameraQueue[0].endTime)
                {
                    nowParam.xAngle = inverted * startParam.xAngle + delta * endParam.xAngle;
                    nowParam.yAngle = inverted * startParam.yAngle + delta * endParam.yAngle;
                    nowParam.fov = inverted * startParam.fov + delta * endParam.fov;
                    nowParam.distance = inverted * startParam.distance + delta * endParam.distance;
                    nowParam.x = inverted * startParam.x + delta * endParam.x;
                    nowParam.y = inverted * startParam.y + delta * endParam.y;
                    nowParam.height = inverted * startParam.height + delta * endParam.height;
                }
                else
                {
                    nowParam.copyFrom(endParam);
                    nowCount = -1;
                    cameraQueue.RemoveAt(0);
                }
            };
        }
    }
}
