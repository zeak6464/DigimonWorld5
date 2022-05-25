using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Yukar.Engine
{
    public enum TweenStyle
    {
        Liner,
        PingPong,
    }

    public class TweenListManager<T, TweenClass> where TweenClass : TweenBase<T>, new()
    {
        private TweenBase<T> tween;

        private int tweenDataCount;
        private List<T> froms;
        private List<T> tos;
        private List<int> frameCounts;
        private List<int> tweenCounts;
        private List<TweenStyle> tweenStyles;

        public bool IsPlayTween { get; private set; }
        public T CurrentValue { get; private set; }

        public TweenListManager()
        {
            tween = new TweenClass();

            tweenDataCount = 0;

            froms = new List<T>();
            tos = new List<T>();
            frameCounts = new List<int>();
            tweenCounts = new List<int>();
            tweenStyles = new List<TweenStyle>();
        }

        public void Add(T fromValue, T toValue, int frameCount, int tweenCount = 1, TweenStyle tweenStyle = TweenStyle.Liner)
        {
            tweenDataCount++;

            froms.Add(fromValue);
            tos.Add(toValue);
            frameCounts.Add(frameCount);
            tweenCounts.Add(tweenCount);
            tweenStyles.Add(tweenStyle);
        }

        public void Clear()
        {
            tweenDataCount = 0;

            froms.Clear();
            tos.Clear();
            frameCounts.Clear();
            tweenCounts.Clear();
            tweenStyles.Clear();
        }

        public void Begin()
        {
            if (tweenDataCount == 0) return;

            tween.Begin(froms[0], tos[0], frameCounts[0], tweenCounts[0], tweenStyles[0]);

            IsPlayTween = true;
            CurrentValue = tween.CurrentValue;
        }

        public void Update()
        {
            if (tweenDataCount == 0)
            {
                IsPlayTween = false;
                return;
            }

            tween.Update();

            if (!tween.IsPlayTween)
            {
                tweenDataCount--;

                froms.RemoveAt(0);
                tos.RemoveAt(0);
                frameCounts.RemoveAt(0);
                tweenCounts.RemoveAt(0);
                tweenStyles.RemoveAt(0);

                if (tweenDataCount > 0)
                {
                    tween.Begin(froms[0], tos[0], frameCounts[0], tweenCounts[0], tweenStyles[0]);
                }
            }

            IsPlayTween = tween.IsPlayTween;
            CurrentValue = tween.CurrentValue;
        }
    }

    public abstract class TweenBase<T>
    {
        protected TweenStyle tweenStyle;
        protected float frameCount;
        protected int tweenCount;
        protected int tweenCountLimit;
        protected float duration;

        protected T from;
        protected T to;

        public bool IsPlayTween { get; protected set; }
        public T CurrentValue { get; protected set; }

        protected TweenBase()
        {
            IsPlayTween = false;
        }

        protected abstract T GetTweenValue(float parcent);

        protected static void Swap(ref T value1, ref T value2)
        {
            T temp;

            temp = value1;
            value1 = value2;
            value2 = temp;
        }

        public void Begin(T to, int frameCount, int tweenCount = 1, TweenStyle tweenStyle = TweenStyle.Liner)
        {
            Begin(CurrentValue, to, frameCount, tweenCount, tweenStyle);
        }

        public void Begin(T fromValue, T toValue, int frameCount, int tweenCount = 1, TweenStyle tweenStyle = TweenStyle.Liner)
        {
            IsPlayTween = true;

            this.tweenStyle = tweenStyle;
            this.from = fromValue;
            this.to = toValue;
            this.duration = frameCount;
            this.frameCount = 0;
            this.tweenCount = 0;
            this.tweenCountLimit = tweenCount;

            CurrentValue = fromValue;
        }

        public virtual void Update()
        {
            if (!IsPlayTween) return;

            frameCount += GameMain.getRelativeParam60FPS();

            bool isTweenEnd = (frameCount >= duration);

            float parcent = (float)frameCount / duration;

            //Console.WriteLine(string.Format("TW F={0}, P={1}", frameCount, parcent));

            switch (tweenStyle)
            {
                case TweenStyle.Liner:
                    if (isTweenEnd)
                    {
                        CurrentValue = to;
                    }
                    else
                    {
                        CurrentValue = GetTweenValue(parcent);
                    }
                    break;
                case TweenStyle.PingPong:
                    if (isTweenEnd)
                    {
                        CurrentValue = to;

                        Swap(ref from, ref to);
                    }
                    else
                    {
                        CurrentValue = GetTweenValue(parcent);
                    }
                    break;
            }

            if (isTweenEnd)
            {
                tweenCount++;
                frameCount = 0;
            }

            if (tweenCount >= tweenCountLimit)
            {
                IsPlayTween = false;
            }
        }
    }

    public class TweenFloat : TweenBase<float>
    {
        protected override float GetTweenValue(float parcent)
        {
            return ( from + ( ( to - from ) * parcent ) );
        }
    }

    public class TweenVector2 : TweenBase<Vector2>
    {
        protected override Vector2 GetTweenValue(float parcent)
        {
            return ( from + ( ( to - from ) * parcent ) );
        }
    }

    public class TweenColor : TweenBase<Color>
    {
        protected override Color GetTweenValue(float parcent)
        {
            return new Color(from.ToVector4() + ( ( to.ToVector4() - from.ToVector4() ) * parcent ));
        }
    }
}
