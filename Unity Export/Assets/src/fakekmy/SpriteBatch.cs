using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SharpKmyGfx
{
    public class SpriteBatch
    {
        public const int DEFAULT_SCREEN_X = 960;
        public const int DEFAULT_SCREEN_Y = 540;
        public static float offsetX;
        public static float offsetY;

        internal struct DrawCall
        {
            internal enum CallType
            {
                TEXTURE,
                TEXT,
            }
            internal CallType type;
            internal Texture2D tex;
            internal UnityEngine.Rect src;
            internal UnityEngine.Rect dest;
            internal UnityEngine.Color col;
            internal float rot;
            internal int zOrder;

            internal string text;
            internal GUIStyle style;
        }
        private static List<DrawCall> drawCalls = new List<DrawCall>();
        private static Texture2D rectTex = null;
        internal static UnityEngine.Font defaultFont;
        private static Material mtl;
        private static bool sIsFont_textureRebuilt = false;

        public SpriteBatch()
        {
            if (rectTex == null) {
                rectTex = new Texture2D(1, 1);
                rectTex.SetPixel(0, 0, new UnityEngine.Color(1, 1, 1, 1));
                rectTex.Apply();

            }

            if (mtl == null)
            {
                mtl = new Material(UnityEngine.Shader.Find("Custom/Premultiplied"));
                rectTex.SetPixel(0, 0, new UnityEngine.Color(1, 1, 1, 1));
            }
        }

        static SpriteBatch()
        {
            defaultFont = Resources.Load<UnityEngine.Font>("font");
            UnityEngine.Font.textureRebuilt += (UnityEngine.Font obj) =>
            {
                sIsFont_textureRebuilt = true;
            };
        }

        internal void Release()
        {
            // Dummy
        }

        internal void draw(Render scn)
        {
            // Dummy
        }

        internal void drawSpriteVr(Texture t, int v1, int v2, int v3, int v4, int w, int h, int v5, int v6, int v7, int v8, int v9, int v10, int v11, int v12, int v13, int zOrder)
        {
            // Dummy
        }

        internal void setLayer(int v)
        {
            // Dummy
        }

        internal void drawLineRect(int x, int y, int w, int h, int v1, float v2, float v3, float v4, float v5, int v6, int zOrder)
        {
            // Dummy
        }

        internal void drawSprite(Texture tex, int x, int y, int ofsx, int ofsy, int w, int h, float rot, float u0, float v0, float u1, float v1, float r, float g, float b, float a, int zOrder)
        {
            DrawCall call = new DrawCall();
            call.type = DrawCall.CallType.TEXTURE;
            call.tex = tex.obj;
            call.dest = new UnityEngine.Rect(x + ofsx, y + ofsy, w, h);
            call.src = new UnityEngine.Rect(u0, 1.0f - v1, u1 - u0, v1 - v0);
            call.rot = rot;
            call.zOrder = zOrder;
            call.col = createUnityColor(r, g, b, a);
            drawCalls.Add(call);
        }

        public static UnityEngine.Color createUnityColor(float r, float g, float b, float a)
        {
            /*
            if (a == 0 && r + g + b > 0)	// 乗算済みアルファを使わないと正しく描画出来ない組み合わせに暫定対応
                a = (r + g + b) / 3;
            return new UnityEngine.Color(r / a / 2, g / a / 2, b / a / 2, a / 2);
            */
            return new UnityEngine.Color(r, g, b, a);
        }

        internal void drawText(Font font, byte[] bytes, int x, int y, float r, float g, float b, float a, float scale, int zOrder)
        {
            DrawCall call = new DrawCall();
            call.type = DrawCall.CallType.TEXT;
            call.text = System.Text.Encoding.UTF8.GetString(bytes);
            call.style = new GUIStyle();
            // アスペクト比に応じてテキストのスケーリングを調整する
            float scl = calcScale();
            //
            call.style.font = defaultFont;
            call.style.fontSize = (int)(font.fontSize * scl * scale);
            GUIStyleState styleState = new GUIStyleState();
            styleState.textColor = new UnityEngine.Color(r, g, b, a);
            call.style.normal = styleState;
            call.dest = new UnityEngine.Rect(x, y, 1000, 1000);
            call.zOrder = zOrder;
            drawCalls.Add(call);
        }

        internal void drawText(Font font, byte[] bytes, int x, int y, float r, float g, float b, float a, float scale, int zOrder, bool italic)
        {
            DrawCall call = new DrawCall();
            call.type = DrawCall.CallType.TEXT;
            call.text = System.Text.Encoding.UTF8.GetString(bytes);
            call.style = new GUIStyle();
            // アスペクト比に応じてテキストのスケーリングを調整する
            float scl = calcScale();
            //
            call.style.font = defaultFont;
            call.style.fontSize = (int)(font.fontSize * scl * scale);
            if(italic)
                call.style.fontStyle = FontStyle.Italic;
            GUIStyleState styleState = new GUIStyleState();
            styleState.textColor = new UnityEngine.Color(r, g, b, a);
            call.style.normal = styleState;
            call.dest = new UnityEngine.Rect(x, y, 1000, 1000);
            call.zOrder = zOrder;
            drawCalls.Add(call);
        }

        internal void drawRect(int x, int y, int w, int h, float rot, float r, float g, float b, float a, float scale, int zOrder)
        {
            DrawCall call = new DrawCall();
            call.type = DrawCall.CallType.TEXTURE;
            call.tex = rectTex;
            call.dest = new UnityEngine.Rect(x, y, w, h);
            call.src = new UnityEngine.Rect(0, 0, 1, 1);
            call.rot = rot;
            call.zOrder = zOrder;
            call.col = new UnityEngine.Color(r, g, b, a);
            drawCalls.Add(call);
        }

#if ENABLE_VR_UNITY
        static GameObject sUIRoot = null;
        static GameObject sUIImage = null;
        static GameObject sUIText = null;
        static List<GameObject> sUITextureList = new List<GameObject>();
        static List<GameObject> sUITextList = new List<GameObject>();
#endif

        internal static void DrawAll()
        {
            // キャプチャしたレンダーテクスチャを表示
            UnityEntry.drawCapturedFB();

#if !ENABLE_VR_UNITY
            // アスペクト比に応じて3Dオブジェクト(マップ等)をスケーリングする
            Camera camera = Camera.main;
            if (camera != null && camera.gameObject.layer != LayerMask.NameToLayer("UI"))
            {
                float defaultAspect = (float)DEFAULT_SCREEN_Y / (float)DEFAULT_SCREEN_X;
                float screenAspect = (float)Screen.height / (float)Screen.width;
                if (screenAspect < defaultAspect)
                {
                    // スクリーンの左右に黒帯
                    float fixRatio = screenAspect / defaultAspect;
                    camera.rect = new Rect(0.5f - fixRatio / 2f, 0f, fixRatio, 1f);
                }
                else
                {
                    // スクリーンの上下に黒帯
                    float fixRatio = defaultAspect / screenAspect;
                    camera.rect = new Rect(0f, 0.5f - fixRatio / 2f, 1f, fixRatio);
                }
            }
            //

            // アスペクト比に応じて画面の上下左右に黒帯を描画する
            drawLetterBox();
            //

            //事前に使用する文字列を通知する
            {
                //表示される文字だけ重要なのでフラグを落とす
                sIsFont_textureRebuilt = false;
                //一度コールする
                foreach (var call in drawCalls)
                {
                    if (call.type != DrawCall.CallType.TEXT) continue;
                    call.style.font.RequestCharactersInTexture(call.text, call.style.fontSize, call.style.fontStyle);
                }
                //コールし終わってリビルドされていたらもう一度コールする
                if (sIsFont_textureRebuilt)
                {
                    sIsFont_textureRebuilt = false;
                    foreach (var call in drawCalls)
                    {
                        if (call.type != DrawCall.CallType.TEXT) continue;
                        call.style.font.RequestCharactersInTexture(call.text, call.style.fontSize, call.style.fontStyle);
                    }
                }
            }

            // ためてたドローコールをすべて実行する
            foreach (var call in drawCalls)
            {
                var dest = expandDestRect(call.dest);
                switch (call.type)
                {
                    case DrawCall.CallType.TEXTURE:
                        if (call.rot != 0)
                        {
                            float pivotX = call.dest.center.x * calcScale() + offsetX; // 回転中心のx座標
                            float pivotY = call.dest.center.y * calcScale() + offsetY; // 回転中心のy座標
                            GUIUtility.RotateAroundPivot(call.rot, new Vector2(pivotX, pivotY));
                            Graphics.DrawTexture(dest, call.tex, call.src, 0, 0, 0, 0, call.col, mtl);
                            GUI.matrix = Matrix4x4.identity; // 行列を初期値に戻す必要がある
                        }
                        else
                        {
                            Graphics.DrawTexture(dest, call.tex, call.src, 0, 0, 0, 0, call.col, mtl);
                        }
                        break;
                    case DrawCall.CallType.TEXT:
                        GUI.Label(dest, call.text, call.style);
                        break;
                }
            }
            drawCalls.Clear();


#else
            GameObject root = sUIRoot = GameObject.Find("UIRoot");

            //テクスチャ表示用のテンプレオブジェクト
            if (sUIImage == null)
            {
                sUIImage = GameObject.Find("UIImage");
                sUIImage.SetActive(false);
            }
            if (sUIText == null)
            {
                sUIText = GameObject.Find("UIText");
                sUIText.SetActive(false);
            }

            Camera mainCamera = Camera.main;

            var rootPos = root.transform.position;
            var rootScale = root.transform.localScale;
            var rootRotation = root.transform.rotation = Quaternion.identity;
            int drawTextureCount = 0;
            int drawTextCount = 0;

            //drawCalls.Sort((a, b) => a.zOrder - b.zOrder);
            {
                Dictionary<int, List<DrawCall>> drawCallMap = new Dictionary<int, List<DrawCall>>();
                for (var i1 = 0; i1 < drawCalls.Count; ++i1)
                {
                    var call = drawCalls[i1];
                    var zOrder = call.zOrder;
                    if(drawCallMap.ContainsKey( zOrder ) == false){
                        drawCallMap.Add( zOrder, new List<DrawCall>() );
                    }
                    drawCallMap[zOrder].Add(call);
                }
                drawCalls.Clear();
                foreach (var callList in drawCallMap)
                {
                    drawCalls.AddRange(callList.Value);
                }
            }

            //float posZ = 0;
            for(var i1 = 0; i1 < drawCalls.Count; ++i1){
                var call = drawCalls[drawCalls.Count - i1 - 1];
                //posZ = 1f;
                switch (call.type)
                {
                    case DrawCall.CallType.TEXTURE:
                        {
                            GameObject obj = null;
                            //初期化
                            for (int i2 = sUITextureList.Count; i2 <= 256 || i2 <= drawTextureCount; ++i2)
                            {
                                obj = UnityEngine.Object.Instantiate(sUIImage) as GameObject;
                                obj.transform.SetParent(sUIRoot.transform);
                                sUITextureList.Add(obj);
                            }
                            obj = sUITextureList[drawTextureCount];
                            drawTextureCount++;

                            //配置
                            obj.SetActive(true);

                            var dest = call.dest;
                            var trans = obj.GetComponent<RectTransform>();

                            //フェード用のUIの場合は引き延ばす
                            if (dest.x == 0 && dest.y == 0
                            && DEFAULT_SCREEN_X == dest.size.x
                            && DEFAULT_SCREEN_Y == dest.size.y
                            && rectTex == call.tex)
                            {
                                dest.x -= DEFAULT_SCREEN_X * 5;
                                dest.y -= DEFAULT_SCREEN_X * 5;
                                dest.size = new Vector2(DEFAULT_SCREEN_X * 20, DEFAULT_SCREEN_Y * 20);
                            }

                            var pos = trans.position;
                            pos.x = (dest.x + dest.size.x / 2) - (DEFAULT_SCREEN_X / 2);
                            pos.x *= rootScale.x;
                            pos.x += rootPos.x;
                            pos.y = (-dest.y - dest.size.y / 2) + (DEFAULT_SCREEN_Y / 2);
                            pos.y *= rootScale.y;
                            pos.y += rootPos.y;
                            pos.z = rootPos.z;// - ((posZ * 0.001f + ((float)call.zOrder) * 0.1f)) * 0.1f;
                            trans.position = pos;
                            trans.sizeDelta = dest.size;
                            trans.localScale = new Vector3(1,1,1);
                            trans.SetSiblingIndex(call.zOrder);

                            var image = obj.GetComponent<UnityEngine.UI.RawImage>();
                            image.texture = call.tex;
                            image.uvRect = call.src;

                            var col = call.col * 2;//TODO なぜか2倍にすると落ち着く謎
                            image.color = col;
                            break;
                        }
                    case DrawCall.CallType.TEXT:
                        {
                            //初期化
                            GameObject obj = null;
                            //初期化
                            for (int i2 = sUITextList.Count; i2 <= 256 || i2 <= drawTextCount; ++i2)
                            {
                                obj = UnityEngine.Object.Instantiate(sUIText) as GameObject;
                                obj.transform.SetParent(sUIRoot.transform);
                                sUITextList.Add(obj);
                            }

                            obj = sUITextList[drawTextCount];
                            drawTextCount++;

                            //配置
                            obj.SetActive(true);

#if false
                            var pos = obj.transform.localPosition;
                            var scale = obj.transform.localScale;
                            //var dest = expandDestRect(call.dest);
                            var dest = call.dest;
                            pos.x = dest.x;
                            pos.y = -dest.y;
                            pos.z = drawZ;
                            scale.x = call.style.font.fontSize / 2;
                            scale.y = call.style.font.fontSize / 2;
                            scale.z = call.style.font.fontSize / 2;

                            obj.transform.localPosition = pos;
                            obj.transform.localScale = scale;
                            
                            TextMesh textMesh = obj.GetComponent<TextMesh>();
                            textMesh.text = call.text;
                            textMesh.color = call.style.normal.textColor;
                            textMesh.fontSize = call.style.fontSize;
#else
                            var dest = call.dest;
#if UNITY_EDITOR
                            var fontScale = 1f;
#else
                            var fontScale = 0.4f;
#endif
                            var trans = obj.GetComponent<RectTransform>();
                            var pos = trans.position;
                            pos.x = (dest.x + dest.size.x / 2) - (DEFAULT_SCREEN_X / 2);
                            pos.x += (call.style.fontSize * fontScale) / 4.0f;
                            pos.x *= rootScale.x;
                            pos.x += rootPos.x;
                            pos.y = (-dest.y - dest.size.y / 2) + (DEFAULT_SCREEN_Y / 2) + rootPos.y;
                            pos.y += -(call.style.fontSize * fontScale) / 4.0f;
                            //pos.y -= (call.style.font.lineHeight - call.style.font.fontSize) * 2;
                            pos.y *= rootScale.y;
                            pos.y += rootPos.y;
                            pos.z = rootPos.z;// - ((posZ * 0.001f + ((float)call.zOrder) * 0.1f)) * 0.1f;
                            trans.position = pos;
                            trans.sizeDelta = dest.size;
                            //trans.localScale = rootScale;//
                            trans.localScale = new Vector3(1,1,1);
                            trans.SetSiblingIndex(call.zOrder);

                            var text = obj.GetComponent<UnityEngine.UI.Text>();
                            text.text = call.text;
                            text.fontSize = (int)(call.style.fontSize * fontScale);//TODO 
                            text.color = call.style.normal.textColor;
#endif
                            break;
                        }
                }
            }

            root.transform.rotation = mainCamera.transform.rotation;

            //仕様していないのは非表示に
            for (var i1 = drawTextureCount; i1 < sUITextureList.Count; ++i1)
            {
                sUITextureList[i1].SetActive(false);
            }
            for (var i1 = drawTextCount; i1 < sUITextList.Count; ++i1)
            {
                sUITextList[i1].SetActive(false);
            }
            drawCalls.Clear();
#endif
        }

        private static Rect expandDestRect(Rect dest)
        {
#if UNITY_2018_2_OR_NEWER
            // アスペクト比に応じて2Dオブジェクト(キャライラスト等)をスケーリングする
            float scale = calcScale();
            offsetX = (int)(((float)Screen.width - calcContentWidth()) / 2);   // 2Dオブジェクトのx方向のずらし値
            offsetY = (int)(((float)Screen.height - calcContentHeight()) / 2); // 2Dオブジェクトのy方向のずらし値
            float destX = dest.x * scale + offsetX; // 2Dオブジェクトのx座標
            float destY = dest.y * scale + offsetY; // 2Dオブジェクトのy座標
            float destW = dest.width * scale;  // 2Dオブジェクトの横幅
            float destH = dest.height * scale; // 2Dオブジェクトの縦幅
            
            var result = new Rect((int)destX, (int)destY, 0, 0);
            result.xMax = (int)(destX + destW);
            result.yMax = (int)(destY + destH);
            return result;
#else
            // アスペクト比に応じて2Dオブジェクト(キャライラスト等)をスケーリングする
            float scale = calcScale();
            offsetX = ((float)Screen.width - calcContentWidth()) / 2;   // 2Dオブジェクトのx方向のずらし値
            offsetY = ((float)Screen.height - calcContentHeight()) / 2; // 2Dオブジェクトのy方向のずらし値
            float destX = dest.x * scale + offsetX; // 2Dオブジェクトのx座標
            float destY = dest.y * scale + offsetY; // 2Dオブジェクトのy座標
            float destW = dest.width * scale;  // 2Dオブジェクトの横幅
            float destH = dest.height * scale; // 2Dオブジェクトの縦幅
            return new Rect(destX, destY, destW, destH);
#endif
        }

        // アスペクト比に応じたゲーム内容描画領域の横幅を計算する
        private static float calcContentWidth()
        {
            float defaultAspect = (float)DEFAULT_SCREEN_Y / (float)DEFAULT_SCREEN_X; // デフォルトのアスペクト比
            float screenAspect = (float)Screen.height / (float)Screen.width; // 現在実行中環境の画面全体のアスペクト比
            if (screenAspect < defaultAspect)
            {
                // スクリーンの左右に黒帯
                return (float)Screen.height / defaultAspect;
            }
            else
            {
                // スクリーンの上下に黒帯
                return (float)Screen.width;
            }
        }

        // アスペクト比に応じたゲーム内容描画領域の縦幅を計算する
        private static float calcContentHeight()
        {
            float defaultAspect = (float)DEFAULT_SCREEN_Y / (float)DEFAULT_SCREEN_X; // デフォルトのアスペクト比
            float screenAspect = (float)Screen.height / (float)Screen.width; // 現在実行中環境の画面全体のアスペクト比
            if (screenAspect < defaultAspect)
            {
                // スクリーンの左右に黒帯
                return (float)Screen.height;
            }
            else
            {
                // スクリーンの上下に黒帯
                return (float)Screen.width * defaultAspect;
            }
        }

        // アスペクト比に応じたオブジェクトの拡縮倍率を計算する
        private static float calcScale()
        {
            float contentWidth = calcContentWidth();  // ゲーム内容が表示される領域の横幅
            return contentWidth / (float)DEFAULT_SCREEN_X; // 拡縮倍率
        }

        // レターボックス(画面の上下左右に出る黒帯)の四角形オブジェクトを描画する
        private static void drawLetterBox()
        {
            SpriteBatch sb = new SpriteBatch();
            float defaultAspect = (float)DEFAULT_SCREEN_Y / (float)DEFAULT_SCREEN_X;
            float screenAspect = (float)Screen.height / (float)Screen.width;
            if (screenAspect < defaultAspect)
            {
                // スクリーンの左右に黒帯
                sb.drawRect(-256, 0, 256, DEFAULT_SCREEN_Y, 0, 0, 0, 0, 1, 1, 0);             // 左の黒帯
                sb.drawRect(DEFAULT_SCREEN_X, 0, 256, DEFAULT_SCREEN_Y, 0, 0, 0, 0, 1, 1, 0); // 右の黒帯
            }
            else
            {
                // スクリーンの上下に黒帯
                sb.drawRect(0, -256, DEFAULT_SCREEN_X, 256, 0, 0, 0, 0, 1, 1, 0);             // 上の黒帯
                sb.drawRect(0, DEFAULT_SCREEN_Y, DEFAULT_SCREEN_X, 256, 0, 0, 0, 0, 1, 1, 0); // 下の黒帯
            }
        }
    }
}