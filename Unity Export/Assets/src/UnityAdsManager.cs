#if (UNITY_IOS || UNITY_ANDROID) && (ENABLE_UNITY_ADS || ENABLE_GOOGLE_ADS)
#define ENABLE_ADS
#endif


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yukar.Engine;
using Yukar;

public class UnityAdsManager : MonoBehaviour
{
#if ENABLE_ADS
    static UnityAdsManager sInstance = null;
    static GameMain sGame = null;

#if ENABLE_UNITY_ADS
    UnityAds mUnityAdsVideo = new UnityAds(UnitySetting.Ads.UnityAdsId);
#endif //ENABLE_UNITY_ADS

#if ENABLE_GOOGLE_ADS
    UnityAdmobBanner mAdmobBanner = new UnityAdmobBanner(UnitySetting.Ads.UnityAdmobBannerId);
    UnityAdmobVideo mAdmobVideo = new UnityAdmobVideo(UnitySetting.Ads.UnityAdmobVideoId);
#endif //ENABLE_GOOGLE_ADS

    static public List<string> TestDeviceList = new List<string>()
    {
        //"A1234567890123456789012345678901234567890"
    };

    List<AdsInterface> mAdsList = new List<AdsInterface>();
#endif //ENABLE_ADS


    static public Yukar.AdsInterface GetVideo()
    {
#if ENABLE_ADS
#if ENABLE_UNITY_ADS
        return sInstance.mUnityAdsVideo;
#else
        return sInstance.mAdmobVideo;
#endif
#else
        return null;
#endif
    }

    static public void Initialize(GameMain inGame)
    {
#if ENABLE_ADS
        sGame = inGame;
#endif//ENABLE_ADS
    }

    // Use this for initialization
    void Start()
    {
#if ENABLE_ADS
        sInstance = this;

#if ENABLE_UNITY_ADS
        this.mAdsList.Add(this.mUnityAdsVideo);
#endif //ENABLE_UNITY_ADS

#if ENABLE_GOOGLE_ADS
        this.mAdsList.Add(this.mAdmobBanner);
        this.mAdsList.Add(this.mAdmobVideo);
#endif //ENABLE_GOOGLE_ADS

        foreach (var ads in this.mAdsList)
        {
            ads.Load();
        }
#endif //ENABLE_ADS
    }

    // Update is called once per frame
    void Update()
    {
#if ENABLE_ADS
        foreach (var ads in this.mAdsList)
        {
            ads.Update();
        }

        if (sGame == null) return;

#if ENABLE_GOOGLE_ADS
        if (sGame.nowScene == Yukar.Engine.GameMain.Scenes.TITLE)
        {
            this.mAdmobBanner.Show();
            //this.mAdmobVideo.Show();
        }
        else
        {
            this.mAdmobBanner.Hide();
            //this.mAdmobVideo.Hide();
        }
#endif //ENABLE_GOOGLE_ADS
#endif //ENABLE_ADS
    }
}

namespace Yukar
{

    //////////////////////////////////////
    //AdsInterface
    //////////////////////////////////////
    public class AdsInterface
    {
        public enum State
        {
            Hide = 0,
            Error,
            Show,
            Showing,
        }

        protected string mId = "";
        protected State mState = State.Hide;
        public State GetState() { return this.mState; }


        protected float mTryLoadStartTime = 0;


        protected virtual void TryReLoad()
        {
            if (this.IsTryReLoad()) return;
            this.mTryLoadStartTime = Time.time;
        }

        protected virtual bool IsTryReLoad()
        {
            return this.mTryLoadStartTime != 0;
        }

        protected virtual void ReLoad()
        {
            //通信できない場合はロードしない
            if (Application.internetReachability == NetworkReachability.NotReachable) return;

            //ロード
            this.Load();
        }
        
        public virtual void Load(){}

        public virtual void Update(){
            if(this.IsTryReLoad())
            {
                if(10 < Time.time - this.mTryLoadStartTime)
                {
                    this.mTryLoadStartTime = 0;
                    this.ReLoad();
                }
            }
        }
        
        public virtual bool Show()
        {
            if (this.mState == State.Error) return false;
            if (this.mState == State.Show) return true;
            if (this.mState == State.Showing) return true;
            this.mState = State.Show;
            return true;
        }

        public virtual bool IsShow()
        {
            if (this.mState == State.Show) return true;
            if (this.mState == State.Showing) return true;
            return false;
        }

        public virtual bool IsError()
        {
            if (this.mState == State.Error) return true;
            return false;
        }
        
        public virtual bool IsReward()
        {
            return false;
        }

        public virtual bool Hide()
        {
            if (this.mState == State.Error) return false;
            if (this.mState == State.Hide) return true;
            this.mState = State.Hide;
            return true;
        }
    }

#if ENABLE_UNITY_ADS
    //////////////////////////////////////
    //UnityAds
    //////////////////////////////////////
    public class UnityAds : AdsInterface
    {
        bool mInitialize = false;

        public UnityAds(string inId)
        {
            this.mId = inId;
        }

        public override void Load()
        {
            if (this.mInitialize) return;
            this.mInitialize = true;
            this.mState = State.Hide;
            UnityEngine.Advertisements.Advertisement.Initialize(this.mId);
            return;
        }

        public override void Update()
        {
            base.Update();
            switch (this.mState)
            {
                case State.Hide:
                case State.Error:
                    break;
                case State.Show:
                    if (UnityEngine.Advertisements.Advertisement.IsReady() == false
                    || (UnityEngine.Advertisements.Advertisement.GetPlacementState() != UnityEngine.Advertisements.PlacementState.Ready
                      && UnityEngine.Advertisements.Advertisement.GetPlacementState() != UnityEngine.Advertisements.PlacementState.Waiting
                      ))
                    {
                        this.mInitialize = false;
                        this.mState = State.Error;
                        break;
                    }
                    UnityEngine.Advertisements.Advertisement.Show();
                    this.mState = State.Showing;
                    break;
                case State.Showing:
                    if (UnityEngine.Advertisements.Advertisement.isShowing == false)
                    {
                        this.mState = State.Hide;
                    }
                    break;
            }
        }

        public override bool IsReward()
        {
            return true;
        }

        public override bool Hide()
        {
            return true;
        }
    }
#endif

#if ENABLE_GOOGLE_ADS
    //////////////////////////////////////
    //UnityAdmobBanner
    //////////////////////////////////////
    public class UnityAdmobBanner : AdsInterface
    {
        bool mIsAdsLoaded = false;
        GoogleMobileAds.Api.BannerView mBannerView = null;

        public UnityAdmobBanner(string inId)
        {
            this.mId = inId;
        }

        public override void Load()
        {
            if (this.mBannerView != null) return;
            this.mState = State.Hide;

            this.mBannerView = new GoogleMobileAds.Api.BannerView(
                    this.mId,
                    GoogleMobileAds.Api.AdSize.Banner,
                    GoogleMobileAds.Api.AdPosition.BottomRight);
            this.mBannerView.Hide();
            this.mBannerView.OnAdFailedToLoad += (handler, EventArgs) =>
            {
                Debug.Log("UnityAdmobBanner failed to load:" + EventArgs.Message);
                this.mBannerView.Destroy();
                this.mBannerView = null;
                this.mState = State.Error;
            };
            this.mBannerView.OnAdLoaded += (handler, EventArgs) =>
            {
                this.mIsAdsLoaded = true;
            };

            var builder = new GoogleMobileAds.Api.AdRequest.Builder();
            foreach (var deviceId in UnityAdsManager.TestDeviceList)
            {
                builder.AddTestDevice(deviceId);
            }
            this.mBannerView.LoadAd(builder.Build());
            return;
        }
        
        public override void Update()
        {
            base.Update();
            switch (this.mState)
            {
                case State.Error:
                    this.TryReLoad();
                    break;
                case State.Show:
                    if (this.mIsAdsLoaded)
                    {
                        this.mState = State.Showing;
                        this.mBannerView.Show();
                    }
                    break;
            }
        }

        public override bool Show()
        {
            if (this.mBannerView == null) return false;
            if (base.Show() == false) return false;
            return true;
        }

        public override bool Hide()
        {
            if (this.mBannerView == null) return false;
            if (this.mState == State.Hide) return true;
            if (base.Hide() == false) return false;
            this.mBannerView.Hide();
            return true;
        }
    }

    //////////////////////////////////////
    //UnityAdmobVideo
    //////////////////////////////////////
    public class UnityAdmobVideo : AdsInterface
    {
        bool mIsAdsLoaded = false;
        bool mIsAdRewarded = false;
        bool mIsAdClosed = false;

        GoogleMobileAds.Api.RewardBasedVideoAd mRewardBasedVideo = null;

        public UnityAdmobVideo(string inId)
        {
            this.mId = inId;
        }

        public override void Load()
        {
            if (this.mRewardBasedVideo != null) return;
            this.mState = State.Hide;

            this.mRewardBasedVideo = GoogleMobileAds.Api.RewardBasedVideoAd.Instance;
            this.mRewardBasedVideo.OnAdFailedToLoad += (handler, EventArgs) =>
            {
                Debug.Log("UnityAdmobVideo failed to load:" + EventArgs.Message);
                this.mRewardBasedVideo = null;
                this.mState = State.Error;
            };
            this.mRewardBasedVideo.OnAdLoaded += (handler, EventArgs) =>
            {
                this.mIsAdsLoaded = true;
            };
            this.mRewardBasedVideo.OnAdRewarded += (handler, EventArgs) =>
            {
                this.mIsAdRewarded = true;
            };
            this.mRewardBasedVideo.OnAdClosed += (handler, EventArgs) =>
            {
                this.mIsAdClosed = true;
            };
            
            var builder = new GoogleMobileAds.Api.AdRequest.Builder();
            foreach(var deviceId in UnityAdsManager.TestDeviceList)
            {
                builder.AddTestDevice(deviceId);
            }
            this.mRewardBasedVideo.LoadAd(builder.Build(), this.mId);
            return;
        }

        public override void Update()
        {
            base.Update();
            switch (this.mState)
            {
                case State.Hide:
                    break;
                case State.Error:
                    this.TryReLoad();
                    break;
                case State.Show:
                    if (this.mIsAdsLoaded == false) break;
                    if (this.mRewardBasedVideo.IsLoaded() == false) break;
                    this.mIsAdClosed = false;
                    this.mIsAdRewarded = false;
                    this.mState = State.Showing;
                    this.mRewardBasedVideo.Show();
                    break;
                case State.Showing:
                    if (this.mIsAdClosed)
                    {
                        this.mState = State.Hide;
                        this.mRewardBasedVideo = null;
                        this.Load();
                    }
                    break;
            }
        }

        public override bool Show()
        {
            if (this.mRewardBasedVideo == null) return false;
            if (base.Show() == false) return false;
            return true;
        }

        public override bool IsReward()
        {
            return this.mIsAdRewarded;
        }

        public override bool Hide()
        {
            return true;
        }
    }
#endif //ENABLE_GOOGLE_ADS
}