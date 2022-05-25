using System;
using UnityEngine;
using System.Collections.Generic;

namespace SharpKmyAudio
{
    public class Sound : MonoBehaviour
    {
        static Dictionary<int, int> sRefDic = new Dictionary<int, int>();

        private bool mIsLoad = false;

        private AudioClip mAudioClip = null;
        private AudioSource mSourceIntro = null;
        private int mSourceIntroStartSamples = 0;

        private int mSourceLoopBufPlayIndex = -1;
        private double mSourceLoopEndDpsTime = 0;
        private bool mLooped; // 一度でもループしたかどうか
        private AudioSource[] mSourceLoopBuf = null;

        private int mPauseSamples = -1;

        private GameObject mGameObject = null;
        private int mLoopStartSamples = -1;
        private int mLoopEndSamples = -1;
        private bool mIsLoop = false;
        private float mPan = 0;
        private float mVolume = 0;
        private float mTempo;

        public Sound()
        {
        }

        public void Ref()
        {
            if (this.mAudioClip == null) return;
            var id = this.mAudioClip.GetInstanceID();
            if (sRefDic.ContainsKey(id))
            {
                sRefDic[id]++;
            }
            else
            {
                sRefDic[id] = 1;
            }
        }

        public bool Unref()
        {
            if (this.mAudioClip == null) return false;
            var id = this.mAudioClip.GetInstanceID();
            if (sRefDic.ContainsKey(id) == false) return false;

            sRefDic[id]--;
            if (0 < sRefDic[id]) return false;
            sRefDic.Remove(id);

            Resources.UnloadAsset(this.mAudioClip);
            this.mAudioClip = null;
            return true;
        }

        public void Update()
        {
            if (this.mIsLoop == false) return;
            if (this.mAudioClip.loadState != AudioDataLoadState.Loaded) return;
            if (this.isPlaying(false) == false) return;

            //再スケジュール処理
            if (this.mIsLoad == false
            && this.mSourceIntro.timeSamples != this.mSourceIntroStartSamples)
            {
                this.mIsLoad = true;
                var tmeSamples = this.mSourceIntro.timeSamples;
                if (this.mLoopEndSamples <= this.mSourceIntro.timeSamples)
                {
                    //1Frameで再生終了位置を超える場合はすぐに再生するように再スケジュール
                    this.mSourceIntro.Stop();
                    this.ResetLoopBuf(AudioSettings.dspTime);
                }
                else
                {
                    //再生位置から終わる時間を割り出して再スケジュール
                    var startDpsTime = AudioSettings.dspTime + ((double)(this.mLoopEndSamples - this.mSourceIntro.timeSamples) / (double)this.mAudioClip.frequency);
#if !UNITY_WEBGL
                    this.mSourceIntro.SetScheduledEndTime(startDpsTime);
#endif
                    this.ResetLoopBuf(startDpsTime);
                }
                
                return;
            }

            //ループバッファの処理
            if (this.mSourceLoopBufPlayIndex < 0) return;

            if (!mLooped && !mSourceIntro.isPlaying)
                mLooped = true;

            //次再生されるバッファのインデックス
            var nextLoopBufPlayIndex = this.mSourceLoopBufPlayIndex + 1;
            if (this.mSourceLoopBuf.Length <= nextLoopBufPlayIndex)
            {
                nextLoopBufPlayIndex = 0;
            }
            var curLoopBuf = this.mSourceLoopBuf[this.mSourceLoopBufPlayIndex];

#if UNITY_WEBGL
            var nextLoopBuf = this.mSourceLoopBuf[nextLoopBufPlayIndex];
            //WEBGLはSetScheduledEndTimeが動作しないので、再生・停止部は特別対応
            if (0 <= this.mSourceLoopBufPlayIndex)
            {

                if ((curLoopBuf.isPlaying || this.mSourceIntro.isPlaying) && nextLoopBuf.isPlaying)
                {
                    if (this.mSourceIntro.isPlaying)
                    {
                        if (this.mLoopEndSamples < this.mSourceIntro.timeSamples)
                        {
                            var diffSamples = this.mSourceIntro.timeSamples - this.mLoopEndSamples;
                            this.mSourceIntro.Stop();
                            curLoopBuf.timeSamples = this.mLoopStartSamples + diffSamples;
                            curLoopBuf.volume = this.mVolume;
                        }
                    }
                    if (curLoopBuf.isPlaying)
                    {
                        if (this.mLoopEndSamples < curLoopBuf.timeSamples)
                        {
                            //var diffSamples = curLoopBuf.timeSamples - this.mLoopEndSamples;
                            curLoopBuf.Stop();
                            nextLoopBuf.timeSamples = this.mLoopStartSamples;
                            nextLoopBuf.volume = this.mVolume;
                        }
                    }
                }
            }
#endif

            //イントロ再生中・バッファが設定されていない場合・先頭バッファ使用中は、再使用可能なバッファの設定をしない
            if (this.mSourceIntro.isPlaying) return;
            if (curLoopBuf.isPlaying) return;

            //再生されているバッファにインデックスを持っていく
            this.mSourceLoopBufPlayIndex = nextLoopBufPlayIndex;

            //空いたバッファは再度再生位置を設定
            this.UpdateLoopBuf(curLoopBuf);
        }

        private void UpdateLoopBuf(AudioSource inBuf)
        {
            var lenDpsTime = (double)(this.mLoopEndSamples - this.mLoopStartSamples) / (double)this.mAudioClip.frequency;
#if UNITY_WEBGL
            var playStartDpsTime = (this.mSourceLoopEndDpsTime - AudioSettings.dspTime);
            inBuf.PlayDelayed((float)playStartDpsTime);
            this.mSourceLoopEndDpsTime += lenDpsTime;
            inBuf.volume = 0;
#else
            inBuf.PlayScheduled(this.mSourceLoopEndDpsTime);
            this.mSourceLoopEndDpsTime += lenDpsTime;
            inBuf.SetScheduledEndTime(this.mSourceLoopEndDpsTime);
            inBuf.timeSamples = this.mLoopStartSamples;
            inBuf.volume = this.mVolume;
#endif
            inBuf.panStereo = this.mPan;
        }

        private void ResetLoopBuf(double inStartDpsTime)
        {
            foreach (var src in this.mSourceLoopBuf) src.Stop();
            this.mSourceLoopBufPlayIndex = 0;
            this.mSourceLoopEndDpsTime = inStartDpsTime;
            for (int i1 = 0; i1 < this.mSourceLoopBuf.Length; ++i1)
            {
                this.UpdateLoopBuf(this.mSourceLoopBuf[i1]);
            }

        }

        public static Sound load(string path)
        {
            // ループポイントの監視のためにUpdate()が使えるようMonoBehaviourクラスを継承している
            // しかしその場合はnewによる初期化をすると正しく動作しないため以下のように初期化している

            // 1. Unityではオーディオを読み込む際にResourcesフォルダ下にあるファイルを指定する必要があるためそのためのファイルパス処理
            path = Yukar.Common.UnityUtil.pathConvertToUnityResource(path);

            // 2. クリップへオーディオをロード
            AudioClip clip = null;
            clip = Resources.Load<AudioClip>(path);

            if (clip == null)
            {
                Debug.Log("Sound Error : " + path + " could not be loaded.");
                return null;
            }

            // 3. ゲームオブジェクトとして追加する際の名前を設定(とりあえずクリップの名前を入れておく)
            var obj = Yukar.Common.UnityUtil.createObject(Yukar.Common.UnityUtil.ParentType.SOUND, clip.name);

            Sound sound = obj.AddComponent<Sound>();
            sound.mGameObject = obj;

            sound.mSourceIntro = obj.AddComponent<AudioSource>();
            sound.mSourceIntro.playOnAwake = false;
            sound.mSourceIntro.clip = sound.mAudioClip = clip;
            sound.mSourceIntro.loop = false;
            sound.Ref();

            return sound;
            // ---
        }

        internal bool isPlaying(bool oneLoopOnly = true)
        {
            if (this.mSourceIntro != null && this.mSourceIntro.isPlaying) return true;
            if (this.mSourceLoopBuf != null)
            {
                if (oneLoopOnly && mLooped)
                    return false;

                foreach (var src in this.mSourceLoopBuf) if (src.isPlaying) return true;
            }
            return false;
        }

        internal void stop()
        {
            this.mIsLoad = false;

            if (this.mSourceIntro != null) this.mSourceIntro.Stop();
            if (this.mSourceLoopBuf != null)
            {
                foreach (var src in this.mSourceLoopBuf) src.Stop();
            }
            this.mPauseSamples = -1;
        }

        internal void pause()
        {
            this.mIsLoad = false;
            if (this.isPlaying(false) == false) return;

            if (this.mSourceIntro != null && this.mSourceIntro.isPlaying)
            {
                var samples = this.mSourceIntro.timeSamples;
                this.stop();
                this.mPauseSamples = samples;
                return;
            }

            if (this.mSourceLoopBuf != null
            && 0 <= this.mSourceLoopBufPlayIndex)
            {
                var buf = this.mSourceLoopBuf[this.mSourceLoopBufPlayIndex];
                if (buf.isPlaying)
                {
                    var samples = buf.timeSamples;
                    this.stop();
                    this.mPauseSamples = samples;
                    return;
                }
#if false
                foreach (var src in this.mSourceLoopBuf)
                {
                    if (src.isPlaying)
                    {
                        var samples = src.timeSamples;
                        this.stop();
                        this.mPauseSamples = samples;
                        return;
                    }
                }
#endif
            }
            this.mPauseSamples = -1;
        }

        internal void play(bool isLoop = false)
        {
            this.mIsLoad = false;
            if (this.isPlaying(false))this.stop();

            if (0 <= this.mPauseSamples)
            {
                //ループ時間が1フレームで超えるようであればループスタート位置から再生
                if (this.mIsLoop)
                {
                    if (this.mLoopEndSamples < this.mPauseSamples + (this.mAudioClip.frequency * 2.0 / 30.0))
                    {
                        this.mPauseSamples = this.mLoopStartSamples;
                    }
                }

                this.mSourceIntro.timeSamples = this.mSourceIntroStartSamples = this.mPauseSamples;
                this.mPauseSamples = -1;
            }
            else
            {
                this.mSourceIntro.timeSamples = this.mSourceIntroStartSamples = 0;
            }

            this.mSourceIntro.loop = false;
            this.mSourceIntro.Play();
        }

        internal void Release()
        {
            if (this.mGameObject == null) return;
            if (this.mAudioClip == null) return;
            this.stop();
            MonoBehaviour.Destroy(this.mGameObject);
            this.Unref();
            this.mGameObject = null;
            this.mAudioClip = null;
        }

        internal void setLoopInfo(int to, int from)
        {
            if (this.mAudioClip == null) return;
            this.mIsLoop = true;
            this.mLoopStartSamples = to;
            this.mLoopEndSamples = from;
            
            if (this.mLoopStartSamples < 0) this.mLoopStartSamples = 0;
            if (this.mLoopEndSamples < 0) this.mLoopEndSamples = this.mSourceIntro.clip.samples;
            if (this.mAudioClip.samples < this.mLoopStartSamples) this.mLoopStartSamples = this.mAudioClip.samples;
            if (this.mAudioClip.samples < this.mLoopEndSamples) this.mLoopEndSamples = this.mAudioClip.samples;

            if (this.mSourceLoopBuf == null)
            {
                this.mSourceLoopBuf = new AudioSource[2];

                for (int i1 = 0; i1 < this.mSourceLoopBuf.Length; ++i1)
                {
                    var src = this.mSourceLoopBuf[i1] = this.mGameObject.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.clip = this.mAudioClip;
                    src.loop = false;
                }
            }

            this.mSourceLoopBufPlayIndex = -1;
        }


        internal void setPan(float pan)
        {
            this.mPan = pan;
            if (this.mSourceIntro != null) this.mSourceIntro.panStereo = pan;
            if (this.mSourceLoopBuf != null)
            {
                foreach (var src in this.mSourceLoopBuf) src.panStereo = pan;
            }
        }

        internal void setVolume(float v)
        {
            this.mVolume = v;
            if (this.mSourceIntro != null) this.mSourceIntro.volume = v;
#if !UNITY_WEBGL
            if (this.mSourceLoopBuf != null)
            {
                foreach (var src in this.mSourceLoopBuf) src.volume = v;
            }
#else
            if(0 < this.mSourceLoopBufPlayIndex)
            {
                var curLoopBuf = this.mSourceLoopBuf[this.mSourceLoopBufPlayIndex];
                if (curLoopBuf.isPlaying) curLoopBuf.volume = v;
            }
#endif
        }

        internal bool isAvailable()
        {
            return mAudioClip != null;
        }

        internal void setTempo(float tempo)
        {
            this.mTempo = tempo;
            if (this.mSourceIntro != null) this.mSourceIntro.pitch = tempo;
            if (this.mSourceLoopBuf != null)
            {
                foreach (var src in this.mSourceLoopBuf) src.pitch = tempo;
            }
        }

        public float getVolume()
        {
            return mVolume;
        }

        public float getPan()
        {
            return mPan;
        }

        public float getTempo()
        {
            return mTempo;
        }
    }
}