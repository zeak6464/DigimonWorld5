using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Yukar.Common.GameData;
using Yukar.Common.Rom;
using Yukar.Engine;

namespace Bakin
{
    public class CaptureMon : BakinObject
    {
        private Party _party;
		private SystemData _systemData;
		private readonly string _CaptureEnemy1 = "Capture 1"; //Enemy Capture 1 Flag 
		private readonly string _CaptureEnemy2 = "Capture 2"; //Enemy Capture 2 Flag
		private readonly string _CaptureEnemy3 = "Capture 3"; //Enemy Capture 3 Flag
		private readonly string EnemyOne = "Enemy 1"; //Enemy 1 Name as a String
		private readonly string EnemyTwo = "Enemy 2"; //Enemy 2 Name as a String
		private readonly string EnemyThree = "Enemy 3"; //Enemy 3 Name as a String
        
        public override void Start()
        {
            // キャラクターが生成される時に、このメソッドがコールされます。
            // This method is called when the character is created.
        }

        public override void Update()
        {
            Initialise();
            
			if (_systemData.GetSwitch(_CaptureEnemy1))
			{
				var castName = _systemData.GetStrVariable(EnemyOne, Guid.Empty, false);
				
				var heroToAdd = catalog.getFullList().OfType<Cast>().FirstOrDefault(x => x.Name == castName);

				if (heroToAdd != null)
				{
					PushLog($"Adding {heroToAdd.Name} to party");
					_party.AddReserve(heroToAdd);
				}
				
				_systemData.SetSwitch(_CaptureEnemy1, false);
			}
			
			if (_systemData.GetSwitch(_CaptureEnemy2))
			{
				var castName = _systemData.GetStrVariable(EnemyTwo, Guid.Empty, false);
				
				var heroToAdd = catalog.getFullList().OfType<Cast>().FirstOrDefault(x => x.Name == castName);

				if (heroToAdd != null)
				{
					PushLog($"Adding {heroToAdd.Name} to party");
					_party.AddReserve(heroToAdd);
				}
				
				_systemData.SetSwitch(_CaptureEnemy2, false);
			}
			
			if (_systemData.GetSwitch(_CaptureEnemy3))
			{
				var castName = _systemData.GetStrVariable(EnemyThree, Guid.Empty, false);
				
				var heroToAdd = catalog.getFullList().OfType<Cast>().FirstOrDefault(x => x.Name == castName);

				if (heroToAdd != null)
				{
					PushLog($"Adding {heroToAdd.Name} to party");
					_party.AddReserve(heroToAdd);
				}
				
				_systemData.SetSwitch(_CaptureEnemy3, false);
			}
        }

        private void Initialise()
        {	
            if (_party == null)
            {
                _party = GameMain.instance?.data?.party;
            }
			
			if (_systemData == null)
			{
				_systemData = GameMain.instance?.data?.system;
			}

            if (mapChr.isCommonEvent && mapScene == null)
            {
                mapScene = GameMain.instance?.mapScene;
            }
        }
        
        private void PushLog(string message, string sender = "DGTEST", DebugDialog.LogEntry.LogType type = DebugDialog.LogEntry.LogType.BATTLE)
        {
            GameMain.PushLog(type, sender, message);
        }

        public override void BeforeUpdate()
        {
            // キャラクターが生存している間、
            // 毎フレーム、イベント内容の実行前にこのメソッドがコールされます。
            // This method will be called every frame while the character is alive, before the event content is executed.
        }

        public override void Destroy()
        {
            // キャラクターが破棄される時に、このメソッドがコールされます。
            // This method is called when the character is destroyed.
        }

        public override void AfterDraw()
        {
            // このフレームの2D描画処理の最後に、このメソッドがコールされます。
            // This method is called at the end of the 2D drawing process for this frame.
        }

        [BakinFunction(Description="CaptureMon")]
        public float Func(float attr)
        {
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
            
            return 0;
        }
    }
}
