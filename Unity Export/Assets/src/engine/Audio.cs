using System;
using System.Collections.Generic;
using System.Text;

namespace Yukar.Engine
{
    public class Audio
    {
        private static AudioCore sInstance;

        public static void Initialize()
        {
            sInstance = new AudioCore();
        }

        public static void Destroy()
        {
            sInstance.Destroy();
            sInstance = null;
        }

        public static void PlayBgm(Common.Resource.Bgm rom, float volume = 1.0f, float tempo = 1.0f)
        {
            if (rom == null || !Common.Util.file.exists(rom.path))
                return;

            sInstance.PlayBgm(rom, volume, tempo);
        }

        public static void PlayBgs(Common.Resource.Bgs rom, float volume = 1.0f, float tempo = 1.0f)
        {
            if (rom == null || !Common.Util.file.exists(rom.path))
                return;

            sInstance.PlayBgs(rom, volume, tempo);
        }

        public static void StopBgm()
        {
            sInstance.StopBgm();
        }

        public static void StopBgs()
        {
            sInstance.StopBgs();
        }

        // サウンドを読み込む 既に読み込み済みの場合は ID を返却するだけ
        public static int LoadSound(Common.Resource.Se rom)
        {
            return sInstance.LoadSound(rom);
        }

        // サウンドをアンロードする 他の場所から参照されている場合は 参照カウントを減らすだけ
        public static void UnloadSound(Common.Resource.Se rom)
        {
            sInstance.UnloadSound(rom);
        }

        public static void PlaySound(int id, float pan = 0f, float volume = 1.0f, float tempo = 1.0f)
        {
            sInstance.PlaySound(id, pan, volume, tempo);
        }

        public static void UnloadSound(int id)
        {
            sInstance.UnloadSound(id);
        }

        internal static Yukar.Engine.AudioCore.SoundDef GetNowBgm(bool doClear = true)
        {
            return sInstance.GetNowBgm(doClear);
        }

        internal static void SwapBgm(Yukar.Engine.AudioCore.SoundDef sound)
        {
            sInstance.SwapBgm(sound);
        }

        public static bool IsBgmPlaying()
        {
            return sInstance.IsBgmPlaying();
        }

        internal static AudioCore.SoundDef GetNowBgs()
        {
            return sInstance.mBgsSound;
        }

        internal static Common.Resource.ResourceItem GetNowBgsRom()
        {
            if (sInstance.mBgsSound == null)
                return null;
            return sInstance.mBgsSound.rom;
        }

        internal static void setMasterVolume(float bgm, float se)
        {
            sInstance.masterBgmVolume = bgm;
            sInstance.masterSeVolume = se;
            sInstance.changeVolume();
        }

        internal static float getMasterBgmVolume()
        {
            return sInstance.masterBgmVolume;
        }

        internal static float getMasterSeVolume()
        {
            return sInstance.masterSeVolume;
        }

        internal static bool IsSePlaying(int seId)
        {
            return sInstance.IsSePlaying(seId);
        }

        internal static bool IsSePlaying(Common.Resource.Se sound)
        {
            var def = sInstance.SearchSound(sound);
            if (def == null)
                return false;
            return def.sound.isPlaying();
        }

        internal static int GetSeId(Common.Resource.Se sound)
        {
            var def = sInstance.SearchSound(sound);
            if (def == null)
                return -1;
            return def.id;
        }

        internal static void StopAllSound()
        {
            foreach (var def in sInstance.mSoundDictionary.Values)
            {
                if (def.sound != null && def.rom is Common.Resource.Se && def.sound.isPlaying())
                {
                    def.sound.stop();
                }
            }
        }

        public static void StopSound(int loadedSeId)
        {
            if (sInstance.mSoundDictionary.ContainsKey(loadedSeId))
            {
                var def = sInstance.mSoundDictionary[loadedSeId];
                if (def.sound != null && def.sound.isPlaying())
                    def.sound.stop();
            }
        }

        public static bool GetLoopPoint(string path, out int loopStart, out int loopEnd)
        {
            var def = new AudioCore.SoundDef();
            def.sound = new SharpKmyAudio.Sound();
            def.rom = new Common.Resource.ResourceItem();
            def.rom.path = path;
            var result = AudioCore.getLoopPoint(ref def);
            if (result)
            {
                loopStart = def.loopStart;
                loopEnd = def.loopEnd;
            }
            else
            {
                loopStart = 0;
                loopEnd = 0;
            }
            return result;
        }
    }

    internal class AudioCore
    {
        internal float masterBgmVolume = 1.0f;
        internal float masterSeVolume = 1.0f;

        static void loadSoundImpl(SoundDef def)
        {
            // 読み込む
            def.sound = SharpKmyAudio.Sound.load(def.rom.path);

            // ループするタイプだったらポイント定義を拾う
            // TODO Unity向け実装
            if (def.sound != null && def.rom is Common.Resource.Bgs ||
                (def.rom is Common.Resource.Bgm && ((Common.Resource.Bgm)def.rom).isLoop))
            {
                if (getLoopPoint(ref def))
                {
                    def.applyLoopInfo();
                }
#if WINDOWS
#else
                // ループフラグがtrueでループポイントが未指定の場合もループ処理を実行する
                else
                {
                    // 例外を示す値として-1を入れてループ情報を適用
                    def.loopStart = -1;
                    def.loopEnd = -1;
                    def.applyLoopInfo();
                }
#endif
            }
        }

        internal static bool getLoopPoint(ref SoundDef def)
        {
#if WINDOWS
            // .sli によるループ指定があるかどうか
            var sliPath = def.rom.path + ".sli";
            if (Common.Util.file.exists(sliPath))
            {
                return getLoopPointByKirikiri2Format(sliPath, ref def);
            }

            // メタデータからループ指定を取得する
            return getLoopPointByMetadata(ref def);
#else
            // Unity では FileUtil の getExtras から値を貰う
            var values = Common.Util.file.getExtras(def.rom.path);
            if (values != null && values.Length == 2)
            {
                def.setLoopInfo(values[0], values[1]);
                return true;
            }
            return false;
#endif
        }

        private static bool getLoopPointByMetadata(ref SoundDef def)
        {
            // 先頭512バイトをみる
            var stream = Common.Util.getFileStream(def.rom.path);
            var buf = new byte[512];
            stream.Read(buf, 0, buf.Length);
            stream.Close();

            var startStr = System.Text.Encoding.ASCII.GetBytes("LOOPSTART=");
            var lengthStr = System.Text.Encoding.ASCII.GetBytes("LOOPLENGTH=");

            int startCount = 0;
            int lengthCount = 0;

            int len = 0;
            int to = 0;

            for (int pos = 0; pos < buf.Length; pos++)
            {
                var b = buf[pos];

                if (b == startStr[startCount])
                    startCount++;
                else
                    startCount = 0;
                if (b == lengthStr[lengthCount])
                    lengthCount++;
                else
                    lengthCount = 0;

                if (startStr.Length == startCount)
                {
                    to = getNumberFromByteBuf(buf, pos + 1);
                    startCount = 0;
                }
                else if (lengthStr.Length == lengthCount)
                {
                    len = getNumberFromByteBuf(buf, pos + 1);
                    lengthCount = 0;
                }
            }

            if (len > 0 && def.sound != null)
            {
                var from = len + to;
                def.setLoopInfo(to, from);
                return true;
            }

            return false;
        }

        private static int getNumberFromByteBuf(byte[] buf, int pos)
        {
            int result = 0;

            for (; pos < buf.Length; pos++)
            {
                var b = buf[pos];

                if (b < '0' || '9' < b)
                    break;

                result = result * 10 + b - '0';
            }

            return result;
        }

        private static bool getLoopPointByKirikiri2Format(string sliPath, ref SoundDef def)
        {
            var sr = Common.Util.file.getStreamReader(sliPath, Encoding.UTF8);
            var sliText = sr.ReadToEnd();
            var sliTexts = sliText.Split(new char[] { ' ', '=', ';' }, StringSplitOptions.RemoveEmptyEntries);

            int from = 0;
            int to = 0;

            for (int i = 0; i < sliTexts.Length; i++)
            {
                if (sliTexts[i] == "From")
                {
                    from = int.Parse(sliTexts[i + 1]);
                    i++;
                }
                else if (sliTexts[i] == "To")
                {
                    to = int.Parse(sliTexts[i + 1]);
                    i++;
                }
            }

            if (def.sound != null)
            {
                def.setLoopInfo(to, from);
                return true;
            }

            return false;
        }

        internal int mSoundCount = 0;
        internal class SoundDef
        {
            public int id;
            public int refCount;
            public Common.Resource.ResourceItem rom;
            public SharpKmyAudio.Sound sound;
            public int loopStart;
            public int loopEnd;

            internal void setLoopInfo(int to, int from)
            {
                loopStart = to;
                loopEnd = from;
            }

            internal void applyLoopInfo()
            {
                sound.setLoopInfo(loopStart, loopEnd);
            }
        }
        internal Dictionary<int, SoundDef> mSoundDictionary;

        internal SoundDef mBgmSound;
        internal SoundDef mBgsSound;

        internal AudioCore()
        {
            mSoundDictionary = new Dictionary<int, SoundDef>();
        }

        internal void Destroy()
        {
            DestroyBgs();
            DestroyBgm();

            foreach (var def in mSoundDictionary)
            {
                if (def.Value.sound != null)
                {
                    def.Value.sound.stop();
                    def.Value.sound.Release();
                }
            }
        }

        internal void PlayBgs(Common.Resource.Bgs rom, float volume, float tempo)
        {
            if (mBgsSound != null && mBgsSound.rom == rom)
                return;

            DestroyBgs();

            mBgsSound = new SoundDef();
            mBgsSound.id = mSoundCount++;
            mBgsSound.refCount = 1;
            mBgsSound.rom = rom;
            loadSoundImpl(mBgsSound);

            if (mBgsSound.sound != null)
            {
                mBgsSound.sound.setVolume(masterSeVolume * volume);
                mBgsSound.sound.setTempo(tempo);
                mBgsSound.sound.play(true);
            }
        }

        internal void PlayBgm(Common.Resource.Bgm rom, float volume, float tempo)
        {
            if (mBgmSound != null && mBgmSound.rom == rom && mBgmSound.sound != null)
            {
                mBgmSound.sound.setVolume(masterBgmVolume * volume);
                mBgmSound.sound.setTempo(tempo);
                return;
            }

            DestroyBgm();

            mBgmSound = new SoundDef();
            mBgmSound.id = mSoundCount++;
            mBgmSound.refCount = 1;
            mBgmSound.rom = rom;
            loadSoundImpl(mBgmSound);

			if (mBgmSound.sound != null)
			{
				mBgmSound.sound.setVolume(masterBgmVolume * volume);
				mBgmSound.sound.setTempo(tempo);
				mBgmSound.sound.play(rom.isLoop);
			}
        }

        internal void StopBgs()
        {
            // TODO フェードアウトなどに対応
            DestroyBgs();
        }

        internal void StopBgm()
        {
            // TODO フェードアウトなどに対応
            DestroyBgm();
        }

        internal SoundDef GetNowBgm(bool doClear)
        {
            SoundDef result = mBgmSound;

            if (doClear && mBgmSound != null && mBgmSound.sound != null)
            {
                mBgmSound.sound.pause();
                result = mBgmSound;
                mBgmSound = null;
            }

            return result;
        }

        internal void SwapBgm(SoundDef sound)
        {
            if (mBgmSound == sound)
                return;

            DestroyBgm();
            
            if (sound != null && sound.sound != null)
            {
                if (sound.sound.isAvailable())
                {
                    mBgmSound = sound;
                    mBgmSound.sound.play(((Common.Resource.Bgm)mBgmSound.rom).isLoop);
                }
                else
                {
                    PlayBgm(sound.rom as Common.Resource.Bgm, 1.0f, 1.0f);
                }
            }
        }

        internal void DestroyBgs()
        {
            if (mBgsSound != null && mBgsSound.sound != null)
            {
                mBgsSound.sound.stop();
                mBgsSound.sound.Release();
                mBgsSound = null;
            }
        }

        internal void DestroyBgm()
        {
			if (mBgmSound != null && mBgmSound.sound != null)
            {
                mBgmSound.sound.stop();
                mBgmSound.sound.Release();
                mBgmSound = null;
            }
        }

        internal int LoadSound(Common.Resource.Se rom)
        {
            if (rom == null)
                return -1;

            // 既にある場合はそのIDを返す
            var res = SearchSound(rom);
            if (res != null)
            {
                res.refCount++;
                return res.id;
            }

            // ない場合は新規に読み込む
            var def = new SoundDef();
            def.id = mSoundCount++;
            def.refCount = 1;
            def.rom = rom;
            loadSoundImpl(def);
			
			if(def.sound != null)mSoundDictionary.Add(def.id, def);//読めなかったら加えない

            return def.id;
        }

        internal SoundDef SearchSound(Common.Resource.Se rom)
        {
            foreach (var def in mSoundDictionary)
            {
                var defSrcGuid = Guid.Empty;
                if (def.Value != null && def.Value.rom != null)
                    defSrcGuid = def.Value.rom.guId;
                if (defSrcGuid == (rom == null ? Guid.Empty : rom.guId))
                {
                    return def.Value;
                }
            }

            return null;
        }

        internal void UnloadSound(Common.Resource.Se rom)
        {
            var def = SearchSound(rom);
            if (def == null)
                return;

            Unload(def);
        }

        internal void UnloadSound(int id)
        {
            if (!mSoundDictionary.ContainsKey(id))
                return;

            var def = mSoundDictionary[id];
            if (def == null)
                return;

            Unload(def);
        }

        private void Unload(SoundDef def)
        {
            def.refCount--;

            if (def.refCount > 0)
                return;

            // 先にPlayerを解放する
            if(def.sound != null)def.sound.stop();

            // 既に参照カウントがゼロなので解放する
            // TODO : 頻繁に使われる場合を想定して、少し解放を保留にする仕組みを入れたい
			if (def.sound != null)
			{
				def.sound.Release();
			}
			mSoundDictionary.Remove(def.id);
		}

        internal void PlaySound(int id, float pan, float volume, float tempo)
        {
            if(!mSoundDictionary.ContainsKey(id))
                return;


            // 再生する
            var def = mSoundDictionary[id];
			if (def.sound != null)
			{
				def.sound.setVolume(masterSeVolume * volume);
				def.sound.setPan(pan);
				def.sound.setTempo(tempo);
				def.sound.play();
			}
        }

        internal bool IsBgmPlaying()
        {
            if(mBgmSound == null)
                return false;
			if (mBgmSound.sound == null) return false;

            return mBgmSound.sound.isPlaying();
        }

        internal bool IsSePlaying(int id)
        {
            if (!mSoundDictionary.ContainsKey(id))
                return false;

            // 再生中？
            var def = mSoundDictionary[id];
            if (def.sound == null)
                return false;

            return def.sound.isPlaying();
        }

        internal void changeVolume()
        {
            if (mBgmSound != null && mBgmSound.sound != null)
                mBgmSound.sound.setVolume(masterBgmVolume);
            if (mBgsSound != null && mBgsSound.sound != null)
                mBgsSound.sound.setVolume(masterSeVolume);
        }
    }
}
