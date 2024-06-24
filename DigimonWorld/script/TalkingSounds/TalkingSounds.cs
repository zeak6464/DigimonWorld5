using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using Yukar.Common;
using Yukar.Common.Resource;
using Yukar.Common.Rom;
using Yukar.Engine;

namespace Bakin
{
    public class TalkingSounds : BakinObject
    {
        private int _canSound = 3;
        private int _soundId = -1;

        private List<int> _soundsIds = new List<int>();
        
        private float _lowerRange = 0.9f;
        private float _upperRange = 1.2f;
        private int _frequency;
        private string _soundVar = "talkingSound";
        // private string _soundDataVar = "talkingData";
        private string _useTalkingSoundVar = "useTalkingSound";
        private string _lowerRangeVar = "talkingLowerRange";
        private string _upperRangeVar = "talkingUpperRange";
        private string _soundVolumeVar = "talkingVolume";
        private string _soundPanVar = "talkingPan";
        private string _updateSettingsVar = "updateTalkingSound";
        private float _soundVolume = 1.0f;
        private float _soundPan = 1.0f;
        private bool _useTalkingSounds = true;
        Settings _settings = new Settings();
        readonly Guid SettingsChunk = new Guid("f061d7b2-2c3b-4242-8873-4100dfc299bc"); // don't ever change this
        readonly Guid soundCacheGUID = new Guid("43402c24-5517-4633-bb67-c378af0f5b96"); // don't ever change this
        private bool _isPluginDisable;
        public override void Start()
        {
            var settingsChunk = catalog.getFilteredExtraChunkList(SettingsChunk);
        
            if (settingsChunk.Count > 0)
            {
                settingsChunk[0].readChunk(_settings);
                _isPluginDisable = _settings.isDisabled;
                _useTalkingSoundVar = _settings.useTalkingVar;
                _soundVar = _settings.talkingSoundVar;
                _lowerRangeVar = _settings.talkingLowerVar;
                _upperRangeVar = _settings.talkingUpperVar;
                _soundVolumeVar = _settings.talkingVolumeVar;
                _soundPanVar = _settings.talkingPanVar;
                _updateSettingsVar = _settings.updateTalkingSoundVar;
            }

            if (_isPluginDisable) return;

            var useTalkingSound = GameMain.instance.data.system.GetSwitch(_useTalkingSoundVar, Guid.Empty, false);
            var currSound = GameMain.instance.data.system.GetStrVariable(_soundVar, Guid.Empty, false);
            var lowerRange = GameMain.instance.data.system.GetStrVariable(_lowerRangeVar, Guid.Empty, false);
            var upperRange = GameMain.instance.data.system.GetStrVariable(_upperRangeVar, Guid.Empty, false);
            var volume = GameMain.instance.data.system.GetStrVariable(_soundVolumeVar, Guid.Empty, false);
            var pan = GameMain.instance.data.system.GetStrVariable(_soundPanVar, Guid.Empty, false);

            _useTalkingSounds = useTalkingSound;

            if (!string.IsNullOrEmpty(currSound))
            {
                SetCurrentSoundId(currSound);
            }

            string[] dataList = { lowerRange, upperRange, volume, pan };

            SetData(dataList);

            UpdateFrequency();
            _canSound = _frequency;
            // キャラクターが生成される時に、このメソッドがコールされます。
            // This method is called when the character is created.
        }

        private void SetData(string[] dataList)
        {

            SetCurrentRange(dataList[0], dataList[1]);

            if (float.TryParse(dataList[2], out float volume))
            {
                SetTalkingVolume(volume);
            }
            if (float.TryParse(dataList[3], out float pan))
            {
                SetTalkingPan(pan);
            }

            _useTalkingSounds = mapScene.owner.data.system.GetSwitch(_useTalkingSoundVar, Guid.Empty, false);
        }

        private void SetCurrentRange(string lowerStr, string upperStr)
        {

            if (float.TryParse(lowerStr, out float lower))
            {
                _lowerRange = lower;
            }

            if (float.TryParse(upperStr, out float upper))
            {
                _upperRange = upper;
            }

            mapScene.owner.data.system.SetVariable(_lowerRangeVar, _lowerRange, Guid.Empty, false);
            mapScene.owner.data.system.SetVariable(_upperRangeVar, _upperRange, Guid.Empty, false);
        }

        public override void Update()
        {
            if (_isPluginDisable) return;
            if (mapChr.isCommonEvent && GameMain.instance.data.system.GetSwitch(_updateSettingsVar))
            {
                UpdateTalkingSound();
                GameMain.instance.data.system.SetSwitch(_updateSettingsVar, false, Guid.Empty, false);
            }

            // キャラクターが生存している間、
            // 毎フレームこのキャラクターのアップデート前にこのメソッドがコールされます。
            // This method is called every frame before this character updates while the character is alive.
        }

        private void SetCurrentSoundId(string name)
        {
            var multiSound = name.Split(',');
            _soundsIds.Clear();
      
            if (multiSound.Length > 1)
            {
                foreach (var item in multiSound)
                {
                    var curSound = catalog.getItemFromName<SoundResource>(item);

                    if (curSound != null)
                    {
                        var id = Audio.LoadSound(curSound, true);
                        if (!_soundsIds.Contains(id)) _soundsIds.Add(id);

                    }
                }

                mapScene.owner.data.system.SetVariable(_soundVar, name, Guid.Empty, false);
                return;
            }


            var sound = catalog.getItemFromName<SoundResource>(name);
            _soundId = Audio.LoadSound(sound, true);
        }

        public override void FixedUpdate()
        {
            if (_isPluginDisable || !mapChr.isCommonEvent || !_useTalkingSounds) return;
            UpdateFrequency();
            //var controller = mapScene.GetMenuController();
           
            if (!MessageReader.recentPopped)
            {
                _canSound = _frequency; return;
            }

            _canSound++;

            if (_canSound < _frequency)
            {
                return;
            }

            _canSound = 0;

          
            if (_soundsIds.Count > 0)
            {
            
                foreach (var soundId in _soundsIds)
                {
                    Audio.StopSound(soundId);
                }

                var random = mapScene.GetRandom(_soundsIds.Count, 0);

          
                Audio.PlaySound(_soundsIds[random], _soundPan, _soundVolume, mapScene.GetRandom(_upperRange, _lowerRange));
            }
            else
            {
                Audio.StopSound(_soundId);

                if (_soundId != -1)
                {
                    Audio.PlaySound(_soundId, _soundPan, _soundVolume, mapScene.GetRandom(_upperRange, _lowerRange));
                }
            }


            // キャラクターが生存している間、
            // 物理エンジンのアップデートに同期してこのメソッドが毎秒必ず60回コールされます。
            // This method will be called 60 times every second, synchronously with physics engine updates while the character is alive.
        }


        private void UpdateFrequency()
        {
            var _msgSpeed = catalog.getGameSettings().MessageSpeed;

            if (_msgSpeed == 1) _frequency = 3;
            else if (_msgSpeed == 2) _frequency = 4;
            else if (_msgSpeed == 3) _frequency = 3;
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

        [BakinFunction(Description = "Enable or disable the use of talking sounds. (0 = disable, 1 = enable). \n\n 話し声の使用を有効または無効にします（0 = 無効、1 = 有効）。")]
        public void EnableTalkingSound(int value)
        {
            mapScene.owner.data.system.SetVariable(_useTalkingSoundVar, value, Guid.Empty, false);
            _useTalkingSounds = value == 1;

            mapScene.owner.data.system.SetSwitch(_updateSettingsVar, true, Guid.Empty, false);
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
        }

        [BakinFunction(Description = "Set current talking sound or sounds separated by commas \",\" using a Bakin string.\n\n 現在の話し声を、Bakinの文字列を使用してコンマ「,」で区切った音声に設定します。")]
        public void SetTalkingSound(string name)
        {
          
            mapScene.owner.data.system.SetVariable(_soundVar, name, Guid.Empty, false);
            SetCurrentSoundId(name);

            mapScene.owner.data.system.SetSwitch(_updateSettingsVar, true, Guid.Empty, false);
            // [BakinFunction] を付与したメソッドはイベントパネル「C#プログラムの呼び出し」からコールできます。int/float/string の戻り値および引数を一つまで取ることができます。
            // One of the methods with [BakinFunction] can be called from the event panel "Calling C# Programs".  Up to one int/float/string return value and parameter can be used.
        }

        [BakinFunction(Description = "Set talking sound volume (1 = default). \n\n 話し声の音量を設定します（1 = デフォルト）。")]
        public void SetTalkingVolume(float value)
        {
            _soundVolume = value;

            mapScene.owner.data.system.SetVariable(_soundVolumeVar, _soundVolume, Guid.Empty, false);

            mapScene.owner.data.system.SetSwitch(_updateSettingsVar, true, Guid.Empty, false);
        }
        [BakinFunction(Description = "Set the lower limit for the pitch variance of sounds (0.9 = default). \n\n 音声のピッチの分散の下限を設定します（0.9 = デフォルト）。")]
        public void SetLowerPitch(float value)
        {
            _lowerRange = value;

            mapScene.owner.data.system.SetVariable(_lowerRangeVar, _lowerRangeVar, Guid.Empty, false);

            mapScene.owner.data.system.SetSwitch(_updateSettingsVar, true, Guid.Empty, false);
        }

        [BakinFunction(Description = "Set the upper limit for the pitch variance of sounds (1.2 = default). \n\n 音声のピッチの分散の上限を設定します（1.2 = デフォルト）。")]
        public void SetUpperPitch(float value)
        {
            _upperRange = value;

            mapScene.owner.data.system.SetVariable(_upperRangeVar, _upperRange, Guid.Empty, false);
            mapScene.owner.data.system.SetSwitch(_updateSettingsVar, true, Guid.Empty, false);
        }

        [BakinFunction(Description = "Set talking sound panning (1 = default). \n\n 話し声のパンニングを設定します（1 = デフォルト）。")]
        public void SetTalkingPan(float value)
        {
            _soundPan = value;

            mapScene.owner.data.system.SetVariable(_soundPanVar, _soundPan, Guid.Empty, false);
            mapScene.owner.data.system.SetSwitch(_updateSettingsVar, true, Guid.Empty, false);
        }

        private void UpdateTalkingSound()
        {
            Start();
        }
    }

    public class SoundCache : IChunk
    {
        public Guid guid;

        public string Name { get; set; }
        public SoundCache(Guid guid, string name)
        {
            this.guid = guid;
            Name = name;
        }

        public SoundCache() { }

        public void load(BinaryReader reader)
        {
            guid = new Guid(reader.ReadString());
            Name = reader.ReadString();
        }

        public void save(BinaryWriter writer)
        {
            writer.Write(guid.ToString());
            writer.Write(Name);
        }
    }

    internal class Settings : IChunk
    {

        public bool isDisabled = false;
        public string useTalkingVar = "useTalkingSound";
        public string talkingSoundVar = "talkingSound";
        public string talkingLowerVar = "talkingLowerRange";
        public string talkingUpperVar = "talkingUpperRange";
        public string talkingVolumeVar = "talkingVolume";
        public string talkingPanVar = "talkingPan";
        public string updateTalkingSoundVar = "updateTalkingSound";

        public void load(BinaryReader reader)
        {
            isDisabled = reader.ReadBoolean();
            useTalkingVar = reader.ReadString();
            talkingSoundVar = reader.ReadString();
            talkingLowerVar = reader.ReadString();
            talkingUpperVar = reader.ReadString();
            talkingVolumeVar = reader.ReadString();
            talkingPanVar = reader.ReadString();
            updateTalkingSoundVar = reader.ReadString();


            if (Util.isEndOfStream(reader)) return;
        }

        public void save(BinaryWriter writer)
        {
            writer.Write(isDisabled);
            writer.Write(useTalkingVar);
            writer.Write(talkingSoundVar);
            writer.Write(talkingLowerVar);
            writer.Write(talkingUpperVar);
            writer.Write(talkingVolumeVar);
            writer.Write(talkingPanVar);
            writer.Write(updateTalkingSoundVar);
        }
    }
}
