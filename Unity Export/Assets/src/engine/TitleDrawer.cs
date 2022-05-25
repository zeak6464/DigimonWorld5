using System;
using System.Collections.Generic;
using System.Linq;
using Yukar.Common.Rom;
using Microsoft.Xna.Framework;
using System.Collections;

using ContentAlignment = Yukar.Common.Rom.GameSettings.TitleScreen.ContentAlignment;

namespace Yukar.Engine
{
    public class TitleDrawer
    {
        enum AnimationState
        {
            Wait,
            TitleLogo,
            SelectItem,
            End,
        }

        public enum SelectItemKind
        {
            NewGame,
            Continue,
            Option,
        }

        abstract class TitleObjectBase
        {
            private TitleObjectAnimation CurrentAnimation;
            private IEnumerator AnimationThread;

            public Color color;
            public Vector2 viewport;
            public Vector2 position;
            public Vector2 animationOffset;
            public Vector2 animationScale;
            public ContentAlignment origin;
            public bool isPlayAnimation;
            public float animationFrameCount;

            public abstract Point Size { get; }

            public delegate IEnumerator TitleObjectAnimation(TitleObjectBase titleObject);

            protected TitleObjectBase()
            {
                color = Color.White;

                ResetAnimationValue();
            }

            public abstract void Release();

            public void ResetAnimationValue()
            {
                animationScale = Vector2.One;
                animationOffset = Vector2.Zero;

                animationFrameCount = 0;
                color = Color.White;
            }

            public Vector2 CalcPositionAddAnimation(int screenWidth, int screenHeight)
            {
                Vector2 pos = position;

                if (animationOffset.X > 0) pos.X -= Math.Abs(animationOffset.X) * ( position.X + ( screenWidth * 0.5f ) );
                if (animationOffset.X < 0) pos.X += Math.Abs(animationOffset.X) * ( screenWidth - position.X + ( screenWidth * 0.5f ) );

                if (animationOffset.Y > 0) pos.Y -= Math.Abs(animationOffset.Y) * ( position.Y + ( screenHeight * 0.5f ) );
                if (animationOffset.Y < 0) pos.Y += Math.Abs(animationOffset.Y) * ( screenHeight - position.Y + ( screenHeight * 0.5f ) );

                return pos;
            }

            public void StartAnimation(TitleObjectAnimation animation)
            {
                CurrentAnimation = animation;

                AnimationThread = animation(this);
            }

            public void UpdateAnimation()
            {
                if (AnimationThread != null) AnimationThread.MoveNext();
            }

            public void RestartAnimation()
            {
                if (CurrentAnimation != null) StartAnimation(CurrentAnimation);
            }
        }

        class BackgroundImage : TitleObjectBase
        {
            public int imageId;

            public override Point Size
            {
                get { return new Point(Graphics.GetImageWidth(imageId), Graphics.GetImageHeight(imageId)); }
            }

            public BackgroundImage()
            {
                imageId = -1;
            }

            public override void Release()
            {
                if (imageId >= 0) Graphics.UnloadImage(imageId);

                imageId = -1;
            }
        }

        class TitleLogo : TitleObjectBase
        {
            public int imageId;
            public SharpKmyGfx.Font mainTitleFont;
            public SharpKmyGfx.Font subTitleFont;

            public override Point Size
            {
                get { return new Point(Graphics.GetImageWidth(imageId), Graphics.GetImageHeight(imageId)); }
            }

            public TitleLogo()
            {
                imageId = -1;

                color = Color.White;
            }

            public override void Release()
            {
                if (imageId >= 0) Graphics.UnloadImage(imageId);

                imageId = -1;

                if (mainTitleFont != null) mainTitleFont.Release();

                mainTitleFont = null;

                if (subTitleFont != null) subTitleFont.Release();

                subTitleFont = null;
            }
        }

        class SelectItem : TitleObjectBase
        {
            public SelectItemKind selectItemKind;
            public string itemText;
            public TextDrawer drawer;
            public bool isSelectable;

            public override Point Size
            {
                get { var length = drawer.MeasureString(itemText); return new Point((int)length.X, (int)length.Y); }
            }

            public override void Release()
            {
            }
        }

        class Cursor : TitleObjectBase
        {
            public int imageId;

            public override Point Size
            {
                get { return new Point(Graphics.GetImageWidth(imageId), Graphics.GetImageHeight(imageId)); }
            }

            public Cursor()
            {
                imageId = -1;
            }

            public override void Release()
            {
                if (imageId >= 0) Graphics.UnloadImage(imageId);

                imageId = -1;
            }
        }

        private GameSettings.TitleScreen titleScreen;
        //private TextDrawer titleTextDrawer;
        private WindowDrawer titleTextWindowDrawer;
        private TextDrawer selectItemDrawer;
        private WindowDrawer cursorDrawer;
        private int currentSelectItemIndex;

        private AnimationState animationState;
        private BackgroundImage backgroundImage;
        private TitleLogo titleLogo;
        private SelectItem[] selectItems;
        private Cursor cursor;
        private int frameCount;

        public bool IsPlayAnimation { get { return animationState != AnimationState.End; } }
        public bool IsEnableCurrentSelectItem { get { return selectItems[ currentSelectItemIndex ].isSelectable; } }
        public SelectItemKind CurrentSelectItemKind { get { return selectItems[ currentSelectItemIndex ].selectItemKind; } }

        public TitleDrawer()
        {
            //titleTextDrawer = new TextDrawer(1);
            selectItemDrawer = new TextDrawer(1);

            backgroundImage = new BackgroundImage();
            titleLogo = new TitleLogo();
            cursor = new Cursor();

            Reset();
        }

        public void SetTitleData(GameSettings.TitleScreen title, Yukar.Common.Catalog catalog)
        {
            titleScreen = title;

            if (title.titleBackgroundImage != Guid.Empty)
            {
                var image = catalog.getItemFromGuid(title.titleBackgroundImage) as Yukar.Common.Resource.Sprite;

                if (image != null) backgroundImage.imageId = Graphics.LoadImage(image.path);
            }

            switch(title.titleMode)
            {
                case GameSettings.TitleScreen.TitleMode.Graphics:
                    if (title.titleGraphics != Guid.Empty)
                    {
                        var image = catalog.getItemFromGuid(title.titleGraphics) as Yukar.Common.Resource.Sprite;

                        if (image != null) titleLogo.imageId = Graphics.LoadImage(image.path);
                    }

                    titleLogo.color = Color.White;
                    break;

                case GameSettings.TitleScreen.TitleMode.Text:
                    int mainFontSize = 1;
                    int subFontSize = 1;

                    switch (titleScreen.titleTextSize)
                    {
                        case GameSettings.TitleScreen.TitleTextSize.Small:  mainFontSize = 24; subFontSize = 24; break;
                        case GameSettings.TitleScreen.TitleTextSize.Middle: mainFontSize = 48; subFontSize = 24; break;
                        case GameSettings.TitleScreen.TitleTextSize.Large:  mainFontSize = 72; subFontSize = 48; break;
                    }

                    if (titleLogo.mainTitleFont == null)    // 保険
                    {
                        titleLogo.mainTitleFont = Graphics.sInstance.createFont(mainFontSize);
                        titleLogo.subTitleFont = Graphics.sInstance.createFont(subFontSize);
                    }

                    if (title.titleTextDisplayWindow != Guid.Empty)
                    {
                        var window = catalog.getItemFromGuid(title.titleTextDisplayWindow) as Yukar.Common.Resource.Window;

                        if (window != null) titleTextWindowDrawer = new WindowDrawer(window, Graphics.LoadImage(window.path));
                    }

                    titleLogo.color = Color.White;
                    break;
            }

            List<SelectItem> selectItemsData = new List<SelectItem>();

            selectItemsData.Add(new SelectItem() { selectItemKind = SelectItemKind.NewGame,  itemText = title.selectItemNewGameText, });
            selectItemsData.Add(new SelectItem() { selectItemKind = SelectItemKind.Continue, itemText = title.selectItemContinueText, });
            selectItemsData.Add(new SelectItem() { selectItemKind = SelectItemKind.Option,   itemText = title.selectItemOptionText, });

            foreach (var item in selectItemsData)
            {
                item.isSelectable = true;
                item.drawer = selectItemDrawer;
            }

            selectItems = selectItemsData.ToArray();

            if (title.cursorGraphics != Guid.Empty)
            {
                var cursorWindow = catalog.getItemFromGuid(title.cursorGraphics) as Yukar.Common.Resource.Window;

                if (cursorWindow != null)
                {
                    cursor.imageId = Graphics.LoadImage(cursorWindow.path);

                    cursorDrawer = new WindowDrawer(cursorWindow, cursor.imageId);
                }
            }
        }

        public void Release()
        {
            if (backgroundImage != null) backgroundImage.Release();

            if (titleLogo != null) titleLogo.Release();

            if (cursor != null) cursor.Release();

            if (titleTextWindowDrawer != null && titleTextWindowDrawer.WindowImageID >= 0)
            {
                Graphics.UnloadImage(titleTextWindowDrawer.WindowImageID);

                titleTextWindowDrawer = null;
            }

            if (cursorDrawer != null && cursorDrawer.WindowImageID >= 0)
            {
                Graphics.UnloadImage(cursorDrawer.WindowImageID);

                cursorDrawer = null;
            }
        }

        public void Update()
        {
            frameCount++;

            switch (animationState)
            {
                case AnimationState.Wait:
                    switch (titleScreen.titleAnimation)
                    {
                        case GameSettings.TitleScreen.TitleAnimation.None:
                            titleLogo.StartAnimation(AnimationNothing);
                            break;

                        case GameSettings.TitleScreen.TitleAnimation.FadeIn:
                            titleLogo.StartAnimation(AnimationFadeIn);
                            break;

                        case GameSettings.TitleScreen.TitleAnimation.PopUp:
                            titleLogo.StartAnimation(AnimationPopUp);
                            break;

                        case GameSettings.TitleScreen.TitleAnimation.SlideInFromTop:
                            titleLogo.StartAnimation(AnimationSlideInFromTop);
                            break;

                        case GameSettings.TitleScreen.TitleAnimation.SlideInFromBottom:
                            titleLogo.StartAnimation(AnimationSlideInFromBottom);
                            break;

                        case GameSettings.TitleScreen.TitleAnimation.SlideInFromLeft:
                            titleLogo.StartAnimation(AnimationSlideInFromLeft);
                            break;

                        case GameSettings.TitleScreen.TitleAnimation.SlideInFromRight:
                            titleLogo.StartAnimation(AnimationSlideInFromRight);
                            break;
                    }

                    titleLogo.ResetAnimationValue();

                    animationState = AnimationState.TitleLogo;
                    break;

                case AnimationState.TitleLogo:
                    titleLogo.UpdateAnimation();

                    if (!titleLogo.isPlayAnimation)
                    {
                        animationState = AnimationState.SelectItem;
                    }
                    break;

                case AnimationState.SelectItem:
                    foreach (var selectItem in selectItems)
                    {
                        selectItem.UpdateAnimation();
                    }

                    if (selectItems.Count(selectItem => !selectItem.isPlayAnimation) == selectItems.Length)
                    {
                        cursor.ResetAnimationValue();

                        switch (titleScreen.cursorAnimation)
                        {
                            case GameSettings.TitleScreen.CursorAnimation.Flash:
                                cursor.StartAnimation(AnimationLoopFlash);
                                break;

                            case GameSettings.TitleScreen.CursorAnimation.Blink:
                                cursor.StartAnimation(AnimationLoopFade);
                                break;

                            case GameSettings.TitleScreen.CursorAnimation.Pan:
                                cursor.StartAnimation(AnimationLoopPan);
                                break;
                        }

                        animationState = AnimationState.End;
                    }

                    break;

                case AnimationState.End:
                    cursor.UpdateAnimation();
                    break;
            }

            // 表示位置を計算
            switch (titleScreen.titleMode)
            {
                case GameSettings.TitleScreen.TitleMode.Graphics:
                case GameSettings.TitleScreen.TitleMode.Text:
                    titleLogo.viewport = ToViewportGridRect(titleScreen.titleLogoDisplayPosition, titleScreen.titleLogoDisplayPosition);
                    break;
            }

            ContentAlignment[] positionAlignment = new ContentAlignment[selectItems.Length];
            //ContentAlignment[] originAlignment = new ContentAlignment[selectItems.Length];

            switch (titleScreen.selectItemSortOrientation)
            {
                case GameSettings.TitleScreen.SelectItemSortOrientation.Vertical:
                    positionAlignment[ 0 ] = ContentAlignment.TopCenter;
                    positionAlignment[ 1 ] = ContentAlignment.MiddleCenter;
                    positionAlignment[ 2 ] = ContentAlignment.BottomCenter;
                    break;

                case GameSettings.TitleScreen.SelectItemSortOrientation.Horizontal:
                    positionAlignment = null;
                    //originAlignment = null;
                    break;

                case GameSettings.TitleScreen.SelectItemSortOrientation.DiagonalRightUp:
                    positionAlignment[ 0 ] = ContentAlignment.TopRight;
                    positionAlignment[ 1 ] = ContentAlignment.MiddleCenter;
                    positionAlignment[ 2 ] = ContentAlignment.BottomLeft;
                    break;

                case GameSettings.TitleScreen.SelectItemSortOrientation.DiagonalRightDown:
                    positionAlignment[ 0 ] = ContentAlignment.TopLeft;
                    positionAlignment[ 1 ] = ContentAlignment.MiddleCenter;
                    positionAlignment[ 2 ] = ContentAlignment.BottomRight;
                    break;
            }

            if (positionAlignment != null)
            {
                for (int i = 0; i < selectItems.Length; i++)
                {
                    selectItems[ i ].viewport = ToViewportGridRect(titleScreen.selectItemPosition, positionAlignment[ i ], 0.8f);
                    selectItems[ i ].origin = positionAlignment[ i ];
                }
            }

        }

        public void Draw(int renderWidth, int renderHeight, bool drawMenuItem = true)
        {
            bool isDrawableTitleLogo = false;
            bool isDrawableSelectItems = false;
            bool isDrawableCursor = false;

            switch (animationState)
            {
                case AnimationState.TitleLogo:
                    isDrawableTitleLogo = true;
                    break;

                case AnimationState.SelectItem:
                    isDrawableTitleLogo = true;
                    isDrawableSelectItems = true;
                    break;

                case AnimationState.End:
                    isDrawableTitleLogo = true;
                    isDrawableSelectItems = true;
                    isDrawableCursor = true;
                    break;
            }

            float scaleX = (float)renderWidth / Graphics.ScreenWidth;
            float scaleY = (float)renderHeight / Graphics.ScreenHeight;
            float textScale = Math.Min(scaleX, scaleY);

            if (backgroundImage.imageId >= 0)
            {
                Graphics.DrawImage(backgroundImage.imageId, new Rectangle(0, 0, renderWidth, renderHeight), new Rectangle(0, 0, backgroundImage.Size.X, backgroundImage.Size.Y));
            }
            else
            {
                Graphics.DrawFillRect(0, 0, renderWidth, renderHeight, 0, 0, 255, 255);
            }

            if (isDrawableTitleLogo)
            {
                Vector2 scale;

                scale.X = scaleX * titleLogo.animationScale.X;
                scale.Y = scaleY * titleLogo.animationScale.Y;

                titleLogo.position = titleLogo.viewport * new Vector2(renderWidth, renderHeight);

                titleLogo.position = titleLogo.CalcPositionAddAnimation(renderWidth, renderHeight);

                switch (titleScreen.titleMode)
                {
                    case GameSettings.TitleScreen.TitleMode.Graphics:
                        if (titleLogo.imageId >= 0)
                        {
                            var destinationRect = new Rectangle((int)( titleLogo.position.X - ( ToGridRectPosition(titleScreen.titleLogoDisplayPosition, titleLogo.Size.X, titleLogo.Size.Y, titleScreen.titleLogoDisplayPosition).X * scale.X ) ), (int)( titleLogo.position.Y - ( ToGridRectPosition(titleScreen.titleLogoDisplayPosition, titleLogo.Size.X, titleLogo.Size.Y, titleScreen.titleLogoDisplayPosition).Y * scale.Y ) ), (int)( titleLogo.Size.X * scale.X ), (int)( titleLogo.Size.Y * scale.Y ));
                            var sourceRect = new Rectangle(0, 0, titleLogo.Size.X, titleLogo.Size.Y);

                            //Graphics.DrawImage(titleLogo.imageId, new Rectangle((int)( titleLogo.position.X - (Graphics.GetImageWidth(titleLogo.imageId) * scale.X * 0.5f) ), (int)( titleLogo.position.Y - (Graphics.GetImageHeight(titleLogo.imageId) * scale.Y * 0.5f) ), (int)( Graphics.GetImageWidth(titleLogo.imageId) * scale.X ), (int)( Graphics.GetImageHeight(titleLogo.imageId) * scale.Y )), new Rectangle(0, 0, Graphics.GetImageWidth(titleLogo.imageId), Graphics.GetImageHeight(titleLogo.imageId)), titleLogo.color);
                            Graphics.DrawImage(titleLogo.imageId, destinationRect, sourceRect, titleLogo.color);
                        }
                        break;

                    case GameSettings.TitleScreen.TitleMode.Text:
                        if (titleLogo.mainTitleFont != null && titleLogo.subTitleFont != null)
                        {
                            float titleTextScale = titleLogo.animationScale.X * textScale;

                            var size = Graphics.MeasureString(titleLogo.mainTitleFont, titleScreen.TitleMainText) * titleTextScale;
                            var pos = titleLogo.position;

                            if (!string.IsNullOrEmpty(titleScreen.TitleSubText))
                            {
                                var subTitleSize = Graphics.MeasureString(titleLogo.subTitleFont, titleScreen.TitleSubText) * titleTextScale;

                                size.X = Math.Max(size.X, subTitleSize.X);
                                size.Y += subTitleSize.Y * 0.75f;
                            }

                            var windowSize = size;
                            if (titleTextWindowDrawer != null)
                            {
                                var window = titleTextWindowDrawer.WindowResource;
                                windowSize.X += (window.left + window.right) * titleTextScale;
                                windowSize.Y += (window.top + window.bottom) * titleTextScale;
                            }

                            switch (titleScreen.titleLogoDisplayPosition)
                            {
                                case ContentAlignment.TopLeft:
                                case ContentAlignment.MiddleLeft:
                                case ContentAlignment.BottomLeft:
                                    pos.X += windowSize.X * 0.1f;
                                    break;

                                case ContentAlignment.TopCenter:
                                case ContentAlignment.MiddleCenter:
                                case ContentAlignment.BottomCenter:
                                    pos.X -= windowSize.X * 0.5f;
                                    break;

                                case ContentAlignment.TopRight:
                                case ContentAlignment.MiddleRight:
                                case ContentAlignment.BottomRight:
                                    pos.X -= windowSize.X * 1.1f;
                                    break;
                            }

                            switch (titleScreen.titleLogoDisplayPosition)
                            {
                                case ContentAlignment.TopLeft:
                                case ContentAlignment.TopCenter:
                                case ContentAlignment.TopRight:
                                    pos.Y += windowSize.Y * 0.1f;
                                    break;

                                case ContentAlignment.MiddleLeft:
                                case ContentAlignment.MiddleCenter:
                                case ContentAlignment.MiddleRight:
                                    pos.Y -= windowSize.Y * 0.5f;
                                    break;

                                case ContentAlignment.BottomLeft:
                                case ContentAlignment.BottomCenter:
                                case ContentAlignment.BottomRight:
                                    pos.Y -= windowSize.Y * 1.1f;
                                    break;
                            }

                            if (titleTextWindowDrawer != null)
                            {
                                titleTextWindowDrawer.Draw(pos, windowSize, Vector2.One * titleTextScale, titleLogo.color);

                                var window = titleTextWindowDrawer.WindowResource;
                                pos.X += (window.left * titleTextScale);
                                pos.Y += (window.top * titleTextScale);
                            }

                            var textPos = pos;
                            var areaSize = size;

                            //int lineCount = 1 + (titleScreen.titleText.Length - titleScreen.titleText.Replace(Environment.NewLine, "").Length) / Environment.NewLine.Length;
                            var font = titleLogo.mainTitleFont;

                            foreach (string text in titleScreen.titleText.Split(Environment.NewLine.ToCharArray()))
                            {
                                pos = textPos;
                                size = areaSize;
                                var textSize = Graphics.MeasureString(font, text) * titleTextScale;

                                switch (titleScreen.titleTextPosition)
                                {
                                    case GameSettings.TitleScreen.TitleTextPosition.Left:
                                        break;

                                    case GameSettings.TitleScreen.TitleTextPosition.Center:
                                        pos.X -= textSize.X * 0.5f - size.X * 0.5f;
                                        break;

                                    case GameSettings.TitleScreen.TitleTextPosition.Right:
                                        pos.X -= textSize.X - size.X;
                                        break;
                                }

                                //pos.Y += areaSize.Y * 0.5f;
                                //pos.Y -= textSize.Y * (0.5f * lineCount);

                                var color = new Color(
                                    titleScreen.titleTextColor.R * titleLogo.color.A >> 8,
                                    titleScreen.titleTextColor.G * titleLogo.color.A >> 8,
                                    titleScreen.titleTextColor.B * titleLogo.color.A >> 8,
                                    titleLogo.color.A);

                                var shadowColor = new Color(
                                    0, 0, 0, titleLogo.color.A / 2);

                                switch (titleScreen.titleTextEffect)
                                {
                                    case GameSettings.TitleScreen.TitleTextEffect.None:
                                        Graphics.DrawStringSoloColor(font, text, pos, color, titleTextScale);
                                        break;

                                    case GameSettings.TitleScreen.TitleTextEffect.Shadow:
                                        Graphics.DrawStringSoloColor(font, text, pos + new Vector2(scaleX, scaleY) * titleTextScale * 3.0f,
                                            shadowColor, titleTextScale);
                                        Graphics.DrawStringSoloColor(font, text, pos, color, titleTextScale);
                                        break;

                                    case GameSettings.TitleScreen.TitleTextEffect.Outline:
                                        if (titleLogo.color.A == 255)
                                        {
                                            Graphics.DrawString(font, text, pos, color, titleTextScale);
                                        }
                                        else
                                        {
                                            Graphics.DrawStringSoloColor(font, text, pos, color, titleTextScale);
                                        }
                                        break;
                                }

                                textPos.Y += textSize.Y * 0.75f;// * titleTextScale;
                                font = titleLogo.subTitleFont;
                            }
                        }
                        break;
                }
            }

            if (!drawMenuItem)
                return;

            if (titleScreen.selectItemSortOrientation == GameSettings.TitleScreen.SelectItemSortOrientation.Horizontal)
            {
                int cursorPadding = (cursorDrawer == null) ? 0 : cursorDrawer.paddingLeft + cursorDrawer.paddingRight;
                int padding = Math.Max(cursorPadding, (int)(selectItemDrawer.MeasureString("  ").X * textScale));
                int totalLength = ( (int)( selectItems.Select(item => selectItemDrawer.MeasureString(item.itemText).X).Sum() * textScale ) + ( padding * (selectItems.Length - 1 ) ));
                Vector2 pos = ToGridRectPosition(titleScreen.selectItemPosition, renderWidth, renderHeight, ContentAlignment.MiddleCenter);

                switch (titleScreen.selectItemPosition)
                {
                    case ContentAlignment.TopLeft:
                    case ContentAlignment.MiddleLeft:
                    case ContentAlignment.BottomLeft:
                        break;

                    case ContentAlignment.TopCenter:
                    case ContentAlignment.MiddleCenter:
                    case ContentAlignment.BottomCenter:
                        pos.X -= totalLength * 0.5f;
                        break;

                    case ContentAlignment.TopRight:
                    case ContentAlignment.MiddleRight:
                    case ContentAlignment.BottomRight:
                        pos.X -= totalLength;
                        break;
                }

                /*
                int takeCount = 0;

                switch (titleScreen.selectItemPosition)
                {
                    case ContentAlignment.TopLeft:
                    case ContentAlignment.MiddleLeft:
                    case ContentAlignment.BottomLeft:
                        break;

                    case ContentAlignment.TopCenter:
                    case ContentAlignment.MiddleCenter:
                    case ContentAlignment.BottomCenter:
                        takeCount = 1;
                        break;

                    case ContentAlignment.TopRight:
                    case ContentAlignment.MiddleRight:
                    case ContentAlignment.BottomRight:
                        takeCount = 2;
                        break;
                }

                if (takeCount > 0)
                {
                    pos.X -= ( ( selectItems.Take(takeCount).Select(item => selectItemDrawer.MeasureString(item.itemText).X).Sum() * textScale ) + ( padding * takeCount ) );
                }
                */

                foreach (var selectItem in selectItems)
                {
                    selectItem.viewport = new Vector2(pos.X / renderWidth, pos.Y / renderHeight);
                    selectItem.origin = ContentAlignment.MiddleLeft;

                    pos.X += ( selectItemDrawer.MeasureString(selectItem.itemText).X * textScale ) + padding;
                }
            }

            if (isDrawableCursor && cursorDrawer != null && cursor.imageId >= 0)
            {
                var selectedItem = selectItems[ currentSelectItemIndex ];

                Vector2 pos = selectedItem.viewport * new Vector2(renderWidth, renderHeight);
                Vector2 size = selectItemDrawer.MeasureString(selectedItem.itemText) * textScale;

                switch (selectedItem.origin)
                {
                    case ContentAlignment.TopLeft:
                    case ContentAlignment.TopCenter:
                    case ContentAlignment.TopRight:
                        break;

                    case ContentAlignment.MiddleLeft:
                    case ContentAlignment.MiddleCenter:
                    case ContentAlignment.MiddleRight:
                        pos.Y -= size.Y * 0.5f;
                        break;

                    case ContentAlignment.BottomLeft:
                    case ContentAlignment.BottomCenter:
                    case ContentAlignment.BottomRight:
                        pos.Y -= size.Y;
                        break;
                }

                switch (selectedItem.origin)
                {
                    case ContentAlignment.TopLeft:
                    case ContentAlignment.MiddleLeft:
                    case ContentAlignment.BottomLeft:
                        break;

                    case ContentAlignment.TopCenter:
                    case ContentAlignment.MiddleCenter:
                    case ContentAlignment.BottomCenter:
                        pos.X -= size.X * 0.5f;
                        break;

                    case ContentAlignment.TopRight:
                    case ContentAlignment.MiddleRight:
                    case ContentAlignment.BottomRight:
                        pos.X -= size.X;
                        break;
                }

                var window = cursorDrawer.WindowResource;

                // カーソルの中心を基準にスケールをかける
                size *= cursor.animationScale;

                pos -= ToGridRectPosition(ContentAlignment.MiddleCenter, (int)(size.X * cursor.animationScale.X - size.X), (int)(size.Y * cursor.animationScale.Y - size.Y), ContentAlignment.MiddleCenter);

                cursor.position = pos - new Vector2(window.left * textScale, window.top * textScale);

                // 選択肢のテキストを囲むようにカーソルを表示する
                cursorDrawer.Draw(cursor.position, size + new Vector2(window.left + window.right, window.top + window.bottom) * textScale, new Vector2(textScale, textScale), cursor.color);
            }

            if (isDrawableSelectItems)
            {
                foreach (var selectItem in selectItems)
                {
                    var horizontal = TextDrawer.HorizontalAlignment.Center;
                    var vertical = TextDrawer.VerticalAlignment.Center;

                    switch (selectItem.origin)
                    {
                        case ContentAlignment.TopLeft:
                        case ContentAlignment.TopCenter:
                        case ContentAlignment.TopRight:
                            vertical = TextDrawer.VerticalAlignment.Top;
                            break;

                        case ContentAlignment.MiddleLeft:
                        case ContentAlignment.MiddleCenter:
                        case ContentAlignment.MiddleRight:
                            vertical = TextDrawer.VerticalAlignment.Center;
                            break;

                        case ContentAlignment.BottomLeft:
                        case ContentAlignment.BottomCenter:
                        case ContentAlignment.BottomRight:
                            vertical = TextDrawer.VerticalAlignment.Bottom;
                            break;
                    }

                    switch (selectItem.origin)
                    {
                        case ContentAlignment.TopLeft:
                        case ContentAlignment.MiddleLeft:
                        case ContentAlignment.BottomLeft:
                            horizontal = TextDrawer.HorizontalAlignment.Left;
                            break;

                        case ContentAlignment.TopCenter:
                        case ContentAlignment.MiddleCenter:
                        case ContentAlignment.BottomCenter:
                            horizontal = TextDrawer.HorizontalAlignment.Center;
                            break;

                        case ContentAlignment.TopRight:
                        case ContentAlignment.MiddleRight:
                        case ContentAlignment.BottomRight:
                            horizontal = TextDrawer.HorizontalAlignment.Right;
                            break;
                    }

                    selectItem.position = selectItem.viewport * new Vector2(renderWidth, renderHeight);
                    var itemTextColor = titleScreen.selectItemTextColor;

                    if (!selectItem.isSelectable) itemTextColor = Color.Gray;

                    switch (titleScreen.selectItemTextOutline)
                    {
                        case GameSettings.TitleScreen.SelectItemTextOutline.Enable:
                            selectItemDrawer.DrawString(selectItem.itemText, selectItem.position, horizontal, vertical, itemTextColor, textScale);
                            break;

                        case GameSettings.TitleScreen.SelectItemTextOutline.Disable:
                            selectItemDrawer.DrawStringSoloColor(selectItem.itemText, selectItem.position, horizontal, vertical, itemTextColor, textScale);
                            break;
                    }

#if WINDOWS
#else
                    //バージョンの表示
                    if(UnityEngine.Application.version != "1.0")
                    {
                        var scale = 0.5f;
                        var pos = new Vector2(3, Yukar.Engine.Graphics.ScreenHeight);
                        var ver = "Version:" + UnityEngine.Application.version;
                        horizontal = TextDrawer.HorizontalAlignment.Left;
                        vertical = TextDrawer.VerticalAlignment.Bottom;
                        selectItemDrawer.DrawStringSoloColor(ver, pos + new Vector2(1, 0), horizontal, vertical, Color.Black, scale);
                        selectItemDrawer.DrawStringSoloColor(ver, pos + new Vector2(0, 1), horizontal, vertical, Color.Black, scale);
                        selectItemDrawer.DrawStringSoloColor(ver, pos + new Vector2(0, 0), horizontal, vertical, Color.White, scale);
                    }
#endif

                }
            }
        }

        public void DrawEditGridAndPoint(int renderWidth, int renderHeight)
        {
            // 区切り線を表示
            for (int x = 0; x <= 3; x++)    // 縦線
            {
                int xx = (int)(renderWidth / 3.0f * x);

                xx = Math.Max(xx, 0);
                xx = Math.Min(xx, renderWidth - 1);

                Graphics.DrawFillRect(xx, 1, 1, renderHeight - 2, 0, 255, 0, 255);
            }

            for (int y = 0; y <= 3; y++)    // 横線
            {
                int yy = (int)(renderHeight / 3.0f * y);

                yy = Math.Max(yy, 0);
                yy = Math.Min(yy, renderHeight - 1);

                Graphics.DrawFillRect(1, yy, renderWidth - 2, 1, 0, 255, 0, 255);
            }

            // タイトルロゴの中心位置を表示
            Graphics.DrawFillRect((int)titleLogo.position.X - 4, (int)titleLogo.position.Y - 4, 8, 8, 255, 0, 0, 255);

            // 選択肢の中心位置を表示
            foreach (var selectItem in selectItems)
            {
                Graphics.DrawFillRect((int)selectItem.position.X - 4, (int)selectItem.position.Y - 4, 8, 8, 0, 0, 255, 255);
            }

            // 各区画の中心位置を表示
            foreach (ContentAlignment alingment in Enum.GetValues(typeof(ContentAlignment)))
            {
                var pos = ToGridRectPosition(alingment, renderWidth, renderHeight, ContentAlignment.MiddleCenter);

                Graphics.DrawRect((int)pos.X - 4, (int)pos.Y - 4, 9, 9, 0, 255, 0, 255);
            }

            Graphics.DrawFillRect((int)cursor.position.X - 4, (int)cursor.position.Y - 4, 8, 8, 0, 255, 255, 255);
        }

        public void Reset()
        {
            animationState = AnimationState.Wait;

            frameCount = 0;

            currentSelectItemIndex = 0;
        }

        public void ResetCursorAnimation()
        {
            cursor.RestartAnimation();

            cursor.ResetAnimationValue();
        }

        public void SetSelectItem(SelectItemKind selectItemKind)
        {
            var selectItem = selectItems.FirstOrDefault(item => item.selectItemKind == selectItemKind);

            if (selectItem == null) return;

            currentSelectItemIndex = Array.IndexOf(selectItems, selectItem);
        }

        public void SetSelectItemEnable(SelectItemKind selectItemKind, bool enable)
        {
            var selectItem = selectItems.FirstOrDefault(item => item.selectItemKind == selectItemKind);

            if (selectItem == null) return;

            selectItem.isSelectable = enable;
        }

        public void MoveCursorPrevItem()
        {
            if (currentSelectItemIndex > 0)
            {
                currentSelectItemIndex--;
            }
            else
            {
                currentSelectItemIndex = selectItems.Length - 1;
            }
        }

        public void MoveCursorNextItem()
        {
            if (currentSelectItemIndex < selectItems.Length - 1)
            {
                currentSelectItemIndex++;
            }
            else
            {
                currentSelectItemIndex = 0;
            }
        }

        private Vector2 ToViewportGridRect(ContentAlignment viewAlignment, ContentAlignment gridAlignment, float viewportScale = 1.0f)
        {
            int x = 0;
            int y = 0;

            switch (viewAlignment)
            {
                case ContentAlignment.TopLeft:
                    x = 0;
                    y = 0;
                    break;

                case ContentAlignment.TopCenter:
                    x = 1;
                    y = 0;
                    break;

                case ContentAlignment.TopRight:
                    x = 2;
                    y = 0;
                    break;

                case ContentAlignment.MiddleLeft:
                    x = 0;
                    y = 1;
                    break;

                case ContentAlignment.MiddleCenter:
                    x = 1;
                    y = 1;
                    break;

                case ContentAlignment.MiddleRight:
                    x = 2;
                    y = 1;
                    break;

                case ContentAlignment.BottomLeft:
                    x = 0;
                    y = 2;
                    break;

                case ContentAlignment.BottomCenter:
                    x = 1;
                    y = 2;
                    break;

                case ContentAlignment.BottomRight:
                    x = 2;
                    y = 2;
                    break;
            }

            float x2 = 0;
            float y2 = 0;

            switch (gridAlignment)
            {
                case ContentAlignment.TopLeft:
                    x2 = -0.5f;
                    y2 = -0.5f;
                    break;

                case ContentAlignment.TopCenter:
                    x2 = 0;
                    y2 = -0.5f;
                    break;

                case ContentAlignment.TopRight:
                    x2 = 0.5f;
                    y2 = -0.5f;
                    break;

                case ContentAlignment.MiddleLeft:
                    x2 = -0.5f;
                    y2 = 0;
                    break;

                case ContentAlignment.MiddleCenter:
                    x2 = 0;
                    y2 = 0;
                    break;

                case ContentAlignment.MiddleRight:
                    x2 = 0.5f;
                    y2 = 0;
                    break;

                case ContentAlignment.BottomLeft:
                    x2 = -0.5f;
                    y2 = 0.5f;
                    break;

                case ContentAlignment.BottomCenter:
                    x2 = 0;
                    y2 = 0.5f;
                    break;

                case ContentAlignment.BottomRight:
                    x2 = 0.5f;
                    y2 = 0.5f;
                    break;
            }

            return new Vector2((x + (x2 * viewportScale + 0.5f)) / 3.0f, (y + (y2 * viewportScale + 0.5f)) / 3.0f);
        }

        private Vector2 ToGridRectPosition(ContentAlignment viewAlignment, int width, int height, ContentAlignment gridAlignment)
        {
            Vector2 pos;
            Vector2 p = ToViewportGridRect(viewAlignment, gridAlignment);

            pos.X = (p.X * width);
            pos.Y = (p.Y * height);

            return pos;
        }

        private IEnumerator Animation(TitleObjectBase titleObject, int animationFrame, Action loopProc, Action exitProc = null)
        {
            titleObject.animationFrameCount = 0;

            while (titleObject.animationFrameCount < animationFrame)
            {
                loopProc();

                titleObject.animationFrameCount += GameMain.getRelativeParam60FPS();

                yield return null;
            }

            titleObject.isPlayAnimation = false;

            if (exitProc != null) exitProc();
        }
        private IEnumerator AnimationLoop(TitleObjectBase titleObject, Action loopProc)
        {
            while (true)
            {
                loopProc();

                titleObject.animationFrameCount += GameMain.getRelativeParam60FPS();

                yield return null;
            }
        }

        private IEnumerator AnimationNothing(TitleObjectBase titleObject)
        {
            titleObject.isPlayAnimation = false;

            while (true) yield return null;
        }

        private IEnumerator AnimationFadeIn(TitleObjectBase titleObject)
        {
            var startColor = Color.White;
            const int FadeFrame = 60;

            var animation = Animation(titleObject, FadeFrame,
                () =>
                {
                    int c = (int)MathHelper.Lerp(0, 255, titleObject.animationFrameCount / FadeFrame);

                    c = Math.Min(c, 255);

                    titleObject.color.R = (byte)c;
                    titleObject.color.G = (byte)c;
                    titleObject.color.B = (byte)c;
                    titleObject.color.A = (byte)c;
                },
                () =>
                {
                    titleObject.color = startColor;
                });

            titleObject.isPlayAnimation = true;

            while (titleObject.isPlayAnimation) yield return animation.MoveNext();
        }

        private IEnumerator AnimationPopUp(TitleObjectBase titleObject)
        {
            int state = 0;

            var animation = Animation(titleObject, 60,
                () =>
                {
                    switch (state)
                    {
                        case 0:
                            titleObject.animationScale = Vector2.One * MathHelper.Lerp(0.5f, 1.2f, titleObject.animationFrameCount / 16);
                            titleObject.color.R = (byte)MathHelper.Lerp(0, 255, titleObject.animationFrameCount / 16);
                            titleObject.color.G = (byte)MathHelper.Lerp(0, 255, titleObject.animationFrameCount / 16);
                            titleObject.color.B = (byte)MathHelper.Lerp(0, 255, titleObject.animationFrameCount / 16);
                            titleObject.color.A = (byte)MathHelper.Lerp(0, 255, titleObject.animationFrameCount / 16);

                            if (titleObject.animationFrameCount >= 16)
                            {
                                state++;
                                titleObject.animationFrameCount = 0;
                                titleObject.color.R = 255;
                                titleObject.color.G = 255;
                                titleObject.color.B = 255;
                                titleObject.color.A = 255;
                            }
                            break;

                        case 1:
                            titleObject.animationScale = Vector2.One * MathHelper.Lerp(1.2f, 1.0f, titleObject.animationFrameCount / 6);

                            if (titleObject.animationFrameCount >= 6)
                            {
                                state++;
                                titleObject.animationFrameCount = 0;
                            }
                            break;

                        case 2:
                            titleObject.animationScale = Vector2.One;
                            break;
                    }
                });

            titleObject.isPlayAnimation = true;

            while (titleObject.isPlayAnimation) yield return animation.MoveNext();
        }

        private IEnumerator AnimationSlideInFromLeft(TitleObjectBase titleObject)
        {
            const int SlideFrame = 90;

            var animation = Animation(titleObject, SlideFrame,
                () =>
                {
                    titleObject.animationOffset.X = MathHelper.Lerp(1.0f, 0, titleObject.animationFrameCount / SlideFrame);

                    if (titleObject.animationOffset.X < 0) titleObject.animationOffset.X = 0;
                });

            titleObject.isPlayAnimation = true;

            while (titleObject.isPlayAnimation) yield return animation.MoveNext();
        }

        private IEnumerator AnimationSlideInFromRight(TitleObjectBase titleObject)
        {
            const int SlideFrame = 90;

            var animation = Animation(titleObject, SlideFrame,
                () =>
                {
                    titleObject.animationOffset.X = MathHelper.Lerp(-1.0f, 0, titleObject.animationFrameCount / SlideFrame);

                    if (titleObject.animationOffset.X > 0) titleObject.animationOffset.X = 0;
                });

            titleObject.isPlayAnimation = true;

            while (titleObject.isPlayAnimation) yield return animation.MoveNext();
        }

        private IEnumerator AnimationSlideInFromTop(TitleObjectBase titleObject)
        {
            const int SlideFrame = 90;

            var animation = Animation(titleObject, SlideFrame,
                () =>
                {
                    titleObject.animationOffset.Y = MathHelper.Lerp(1.0f, 0, titleObject.animationFrameCount / SlideFrame);

                    if (titleObject.animationOffset.Y < 0) titleObject.animationOffset.Y = 0;
                });

            titleObject.isPlayAnimation = true;

            while (titleObject.isPlayAnimation) yield return animation.MoveNext();
        }

        private IEnumerator AnimationSlideInFromBottom(TitleObjectBase titleObject)
        {
            const int SlideFrame = 90;

            var animation = Animation(titleObject, SlideFrame,
                () =>
                {
                    titleObject.animationOffset.Y = MathHelper.Lerp(-1.0f, 0, titleObject.animationFrameCount / SlideFrame);

                    if (titleObject.animationOffset.Y > 0) titleObject.animationOffset.Y = 0;
                });

            titleObject.isPlayAnimation = true;

            while (titleObject.isPlayAnimation) yield return animation.MoveNext();
        }

        private IEnumerator AnimationLoopFade(TitleObjectBase titleObject)
        {
            int origin = 255;
            int target = 0;
            const int SwitchFrame = 60;

            titleObject.color.R = (byte)origin;
            titleObject.color.G = (byte)origin;
            titleObject.color.B = (byte)origin;
            titleObject.color.A = (byte)origin;

            var animation = AnimationLoop(titleObject,
                () =>
                {
                    if (titleObject.animationFrameCount >= SwitchFrame)
                    {
                        int temp = origin;
                        origin = target;
                        target = temp;

                        titleObject.animationFrameCount = 0;
                    }

                    int alpha = (int)MathHelper.Lerp(origin, target, titleObject.animationFrameCount / SwitchFrame);

                    titleObject.color.R = (byte)alpha;
                    titleObject.color.G = (byte)alpha;
                    titleObject.color.B = (byte)alpha;
                    titleObject.color.A = (byte)alpha;
                });

            titleObject.isPlayAnimation = true;

            while (true) yield return animation.MoveNext(); 
        }

        private IEnumerator AnimationLoopFlash(TitleObjectBase titleObject)
        {
            int origin = 255;
            int target = 0;
            const int SwitchFrame = 30;

            titleObject.color.R = (byte)origin;
            titleObject.color.G = (byte)origin;
            titleObject.color.B = (byte)origin;
            titleObject.color.A = (byte)origin;

            var animation = AnimationLoop(titleObject,
                () =>
                {
                    if (titleObject.animationFrameCount >= SwitchFrame)
                    {
                        titleObject.color.R = (byte)target;
                        titleObject.color.G = (byte)target;
                        titleObject.color.B = (byte)target;
                        titleObject.color.A = (byte)target;

                        int temp = origin;
                        origin = target;
                        target = temp;

                        titleObject.animationFrameCount = 0;
                    }
                });

            titleObject.isPlayAnimation = true;

            while (true) yield return animation.MoveNext(); 
        }

        private IEnumerator AnimationLoopPan(TitleObjectBase titleObject)
        {
            float origin = 1.0f;
            float target = 1.2f;
            const int SwitchFrame = 20;

            titleObject.animationScale = Vector2.One * origin;

            var animation = AnimationLoop(titleObject,
                () =>
                {
                    if (titleObject.animationFrameCount >= SwitchFrame)
                    {
                        float temp = origin;
                        origin = target;
                        target = temp;

                        titleObject.animationFrameCount = 0;
                    }

                    titleObject.animationScale = Vector2.One * MathHelper.Lerp(origin, target, titleObject.animationFrameCount / SwitchFrame);
                });

            titleObject.isPlayAnimation = true;

            while (true) yield return animation.MoveNext(); 
        }

        public List<SharpKmyMath.Vector2> getSelectItemPos()
        {
            List<SharpKmyMath.Vector2> posList = new List<SharpKmyMath.Vector2>();
            foreach (var selectItem in selectItems)
            {
                posList.Add(new SharpKmyMath.Vector2(selectItem.position.X, selectItem.position.Y));
            }
            return posList;
        }

        public List<SharpKmyMath.Vector2> getSelectItemSize()
        {
            List<SharpKmyMath.Vector2> sizeList = new List<SharpKmyMath.Vector2>();
            foreach (var selectItem in selectItems)
            {
                sizeList.Add(new SharpKmyMath.Vector2(selectItem.Size.X, selectItem.Size.Y));
            }
            return sizeList;
        }
    }
}
