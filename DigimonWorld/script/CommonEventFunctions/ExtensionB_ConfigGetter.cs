using Microsoft.Xna.Framework;
using System;
using Yukar.Engine;
using static Yukar.Engine.Input;

namespace ConfigGetter
{
    public class ConfigGetterClass : BakinObject
    {
        float moved = 0;
        Vector3 lastPos;

        public override void Update()
        {
            var curPos = mapScene.hero.pos;
            curPos.Y = 0; // 高さは考慮しない
            if ((curPos - lastPos).LengthSquared() > 0.0001f)
                moved = 1.5f;
            lastPos = curPos;
            moved -= Math.Min(1, GameMain.getRelativeParam60FPS());
        }

        [BakinFunction(Description = "自動ダッシュがONだったら1、OFFだったら0を返します")]
        public int GetAutoDash()
        {
            return GameMain.instance.data.system.autoDash ? 1 : 0;
        }
        
        [BakinFunction(Description = "ダッシュ中だったら1、それ以外は0を返します")]
        public int IsInDash()
        {
            if (!GameMain.instance.data.system.dashAvailable || moved < 0) return 0;
            return GameMain.instance.data.system.IsInDash(KeyTest(StateType.DIRECT, KeyStates.DASH, GameState.WALK)) ? 1 : 0;
        }
    }
}
