using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yukar.Common.Rom;
using Microsoft.Xna.Framework;

namespace Yukar.Engine
{
    public class EffectDrawer
    {
        public Effect rom;

        internal class Image
        {
            internal Common.Resource.EffectSource source;
            internal int texId;
            internal int sizeX;
            internal int sizeY;
        }
        private List<Image> images = new List<Image>();

        public float nowFrame;
        public bool isEndPlaying = true;
        private Effect.Frame screenFlash;
        private Effect.Frame targetFlash;
        private Effect.Frame[] nodesColor;
        private Effect.Frame[] nodesPosScale;
        private Effect.Frame[] nodesRotate;

        private Effect.Frame screenFlashNext;
        private Effect.Frame targetFlashNext;
        private Effect.Frame[] nodesColorNext;
        private Effect.Frame[] nodesPosScaleNext;
        private Effect.Frame[] nodesRotateNext;

        private Effect.ColorDef DEFAULT_SCREENFLASH_COLORDEF = new Effect.ColorDef();
        private Effect.ColorDef DEFAULT_COLORDEF = new Effect.ColorDef();
        private Effect.PosScaleDef DEFAULT_POSSCALEDEF = new Effect.PosScaleDef();
        private Effect.RotateDef DEFAULT_ROTATEDEF = new Effect.RotateDef();

        private class SeTiming{
            internal int timing;
            internal int id;
        }
        private List<SeTiming> seTimingListForPlay;
        private SeTiming[] seTimingList;

        // 描画用
        private Effect.ColorDef screenColor = new Effect.ColorDef();
        private Effect.ColorDef targetColor = new Effect.ColorDef();
        private Effect.NodeDef[] nodes;

        public void load(Common.Rom.Effect rom, Common.Catalog catalog)
        {
            finalize();

            this.rom = rom;

            if (rom == null)
                return;

            // 画像の読み込み
            addSource(catalog, rom.graphic);
            foreach (var guid in rom.extraGraphic)
            {
                addSource(catalog, guid);
            }

            screenColor.a = screenColor.r = screenColor.g = screenColor.b = 0;

            DEFAULT_SCREENFLASH_COLORDEF.r = 0;
            DEFAULT_SCREENFLASH_COLORDEF.g = 0;
            DEFAULT_SCREENFLASH_COLORDEF.b = 0;
            DEFAULT_SCREENFLASH_COLORDEF.a = 0;

            // 効果音の読み込み
            seTimingListForPlay = new List<SeTiming>();
            foreach (var frame in rom.timeline)
            {
                if (frame.se != Guid.Empty)
                {
                    var se = new SeTiming();
                    se.timing = frame.timing;
                    se.id = Audio.LoadSound(catalog.getItemFromGuid(frame.se) as Common.Resource.Se);
                    seTimingListForPlay.Add(se);
                }
            }
            seTimingList = seTimingListForPlay.ToArray();
        }

        private void addSource(Common.Catalog catalog, Guid guid)
        {
            var source = catalog.getItemFromGuid(guid) as Common.Resource.EffectSource;

            var image = new Image();
            image.source = source;
            if (source != null)
            {
                image.texId = Graphics.LoadImageDiv(image.source.path, image.source.xDiv, image.source.yDiv);
                image.sizeX = Graphics.GetImageWidth(image.texId) / image.source.xDiv;
                image.sizeY = Graphics.GetImageHeight(image.texId) / image.source.yDiv;
            }
            images.Add(image);
        }

        public void initialize()
        {
            if (rom == null)
                return;

            if(rom.timeline.Count == 0)
                return;

            nodesColor = new Effect.Frame[rom.nodeNum];
            nodesPosScale = new Effect.Frame[rom.nodeNum];
            nodesRotate = new Effect.Frame[rom.nodeNum];
            nodesColorNext = new Effect.Frame[rom.nodeNum];
            nodesPosScaleNext = new Effect.Frame[rom.nodeNum];
            nodesRotateNext = new Effect.Frame[rom.nodeNum];
            nodes = new Effect.NodeDef[rom.nodeNum];
            for (int i = 0; i < rom.nodeNum; i++)
            {
                nodes[i] = new Effect.NodeDef();
                nodes[i].color = new Effect.ColorDef();
                nodes[i].posScale = new Effect.PosScaleDef();
                nodes[i].rotate = new Effect.RotateDef();
            }

            nowFrame = 0;
            isEndPlaying = false;
            nextFrameIsEnd = false;

            // 初期セルをセットする
            screenFlash = rom.timeline[0];
            targetFlash = rom.timeline[0];
            screenFlashNext = null;
            targetFlashNext = null;
            foreach (var node in rom.timeline[0].nodes)
            {
                nodes[node.index].cell = node.cell;
            }
            for (int i = 0; i < rom.nodeNum; i++)
            {
                nodesColor[i] = rom.timeline[0];
                nodesPosScale[i] = rom.timeline[0];
                nodesRotate[i] = rom.timeline[0];
                nodes[i].index = (byte)i;
            }

            foreach (var frame in rom.timeline)
            {
                if (screenFlashNext == null && frame.screenFlash != null)
                {
                    screenFlashNext = frame;
                }

                if (targetFlashNext == null && frame.targetFlash != null)
                {
                    targetFlashNext = frame;
                }

                foreach (var node in frame.nodes)
                {
                    if (nodesColorNext[node.index] == null && node.color != null)
                    {
                        nodesColorNext[node.index] = frame;
                    }
                    if (nodesPosScaleNext[node.index] == null && node.posScale != null)
                    {
                        nodesPosScaleNext[node.index] = frame;
                    }
                    if (nodesRotateNext[node.index] == null && node.rotate != null)
                    {
                        nodesRotateNext[node.index] = frame;
                    }
                }
            }

            // 効果音発音リストを初期化する
            seTimingListForPlay = new List<SeTiming>(seTimingList);
        }

        public void finalize()
        {
            rom = null;

            // 読み込み済みの画像を破棄
            foreach (var source in images)
            {
                Graphics.UnloadImage(source.texId);
            }
            images.Clear();

            // 既に読み込み済みの効果音があれば破棄
            if (seTimingList != null)
            {
                foreach (var se in seTimingList)
                {
                    Audio.UnloadSound(se.id);
                }
            }
            seTimingList = null;
        }

        public void update(bool playSE = true, float elapsed = -1)
        {
            if (rom == null)
                return;

            if (rom.timeline.Count == 0)
                return;

            // 今回が効果音発音タイミングだった場合は効果音を鳴らす
            if (seTimingListForPlay.Count != 0 && seTimingListForPlay[0].timing <= nowFrame)
            {
                if (playSE)
                    Audio.PlaySound(seTimingListForPlay[0].id);
                seTimingListForPlay.RemoveAt(0);
            }

            // 今回が切り替わりだった場合は次のを探す
            if (screenFlashNext != null && screenFlashNext.timing <= nowFrame)
            {
                screenFlash = screenFlashNext;

                foreach (var frame in rom.timeline)
                {
                    if (screenFlashNext == null && frame.screenFlash != null)
                    {
                        screenFlashNext = frame;
                        break;
                    }

                    if (frame == screenFlashNext)
                    {
                        screenFlashNext = null;
                    }
                }
            }

            // 今の色をとりあえず代入する
            Effect.ColorDef nowColor = null;
            if (screenFlash.screenFlash != null) nowColor = screenFlash.screenFlash.color;
            if (nowColor == null) nowColor = DEFAULT_SCREENFLASH_COLORDEF;
            screenColor.r = nowColor.r;
            screenColor.g = nowColor.g;
            screenColor.b = nowColor.b;
            screenColor.a = nowColor.a;

            // 画面フラッシュ処理
            if (screenFlashNext != null)
            {
                var nextColor = screenFlashNext.screenFlash.color;

                if (nextColor.interpolate)
                {
                    // デルタ算出
                    int max = screenFlashNext.timing - screenFlash.timing;
                    float cur = nowFrame - screenFlash.timing;
                    float delta = cur / max;

                    // 補間
                    screenColor.r = (byte)((float)screenColor.r * (1 - delta) + (float)nextColor.r * delta);
                    screenColor.g = (byte)((float)screenColor.g * (1 - delta) + (float)nextColor.g * delta);
                    screenColor.b = (byte)((float)screenColor.b * (1 - delta) + (float)nextColor.b * delta);
                    screenColor.a = (byte)((float)screenColor.a * (1 - delta) + (float)nextColor.a * delta);
                }
            }

            // 今回が切り替わりだった場合は次のを探す
            if (targetFlashNext != null && targetFlashNext.timing <= nowFrame)
            {
                targetFlash = targetFlashNext;

                foreach (var frame in rom.timeline)
                {
                    if (targetFlashNext == null && frame.targetFlash != null)
                    {
                        targetFlashNext = frame;
                        break;
                    }

                    if (frame == targetFlashNext)
                    {
                        targetFlashNext = null;
                    }
                }
            }

            nowColor = null;
            if (targetFlash.targetFlash != null) nowColor = targetFlash.targetFlash.color;
            if (nowColor == null) nowColor = DEFAULT_COLORDEF;
            targetColor.r = nowColor.r;
            targetColor.g = nowColor.g;
            targetColor.b = nowColor.b;
            targetColor.a = nowColor.a;

            // 対象フラッシュ処理
            if (targetFlashNext != null)
            {
                var nextColor = targetFlashNext.targetFlash.color;

                if (nextColor.interpolate)
                {
                    // デルタ算出
                    int max = targetFlashNext.timing - targetFlash.timing;
                    float cur = nowFrame - targetFlash.timing;
                    float delta = cur / max;

                    // 補間
                    targetColor.r = (byte)((float)targetColor.r * (1 - delta) + (float)nextColor.r * delta);
                    targetColor.g = (byte)((float)targetColor.g * (1 - delta) + (float)nextColor.g * delta);
                    targetColor.b = (byte)((float)targetColor.b * (1 - delta) + (float)nextColor.b * delta);
                    targetColor.a = (byte)((float)targetColor.a * (1 - delta) + (float)nextColor.a * delta);
                }
            }

            // 各ノード処理
            for (byte i = 0; i < rom.nodeNum; i++)
            {
                // 今回が切り替わりだった場合は次のを探す(移動)
                if (nodesPosScaleNext[i] != null && nodesPosScaleNext[i].timing <= nowFrame)
                {
                    nodesPosScale[i] = nodesPosScaleNext[i];
                    searchNextPosScale(i);
                }

                // 今回が切り替わりだった場合は次のを探す(回転)
                if (nodesRotateNext[i] != null && nodesRotateNext[i].timing <= nowFrame)
                {
                    nodesRotate[i] = nodesRotateNext[i];
                    searchNextRotate(i);
                }

                // 今回が切り替わりだった場合は次のを探す(色調)
                if (nodesColorNext[i] != null && nodesColorNext[i].timing <= nowFrame)
                {
                    nodesColor[i] = nodesColorNext[i];
                    searchNextColor(i);
                }

                var color = nodes[i].color;
                var colorOrg = nodesColor[i].getNodeForEngine(i).color;
                if (colorOrg == null) colorOrg = DEFAULT_COLORDEF;
                color.r = colorOrg.r;
                color.g = colorOrg.g;
                color.b = colorOrg.b;
                color.a = colorOrg.a;

                var posScale = nodes[i].posScale;
                var posScaleOrg = nodesPosScale[i].getNodeForEngine(i).posScale;
                if (posScaleOrg == null) posScaleOrg = DEFAULT_POSSCALEDEF;
                posScale.x = posScaleOrg.x;
                posScale.y = posScaleOrg.y;
                posScale.scaleX = posScaleOrg.scaleX;
                posScale.scaleY = posScaleOrg.scaleY;

                var rotate = nodes[i].rotate;
                var rotateOrg = nodesRotate[i].getNodeForEngine(i).rotate;
                if (rotateOrg == null) rotateOrg = DEFAULT_ROTATEDEF;
                rotate.angle = rotateOrg.angle;

                // ノードの色調補間
                if (nodesColorNext[i] != null)
                {
                    var nextColor = nodesColorNext[i].getNodeForEngine(i).color;

                    if (nextColor.interpolate)
                    {
                        // デルタ算出
                        int max = nodesColorNext[i].timing - nodesColor[i].timing;
                        float cur = nowFrame - nodesColor[i].timing;
                        float delta = cur / max;

                        // 補間
                        color.r = (byte)((float)color.r * (1 - delta) + (float)nextColor.r * delta);
                        color.g = (byte)((float)color.g * (1 - delta) + (float)nextColor.g * delta);
                        color.b = (byte)((float)color.b * (1 - delta) + (float)nextColor.b * delta);
                        color.a = (byte)((float)color.a * (1 - delta) + (float)nextColor.a * delta);
                    }
                }

                // ノードの移動補間
                if (nodesPosScaleNext[i] != null)
                {
                    var nextPosScale = nodesPosScaleNext[i].getNodeForEngine(i).posScale;

                    if (nextPosScale.interpolate != Effect.InterpolateType.NONE)
                    {
                        // デルタ算出
                        float delta = 0;
                        int max = nodesPosScaleNext[i].timing - nodesPosScale[i].timing;
                        float cur = nowFrame - nodesPosScale[i].timing;
                        switch (nextPosScale.interpolate)
                        {
                            case Effect.InterpolateType.LINEAR:
                                delta = (float)cur / max;
                                break;

                            case Effect.InterpolateType.EASE_IN:
                                delta = 1.0f - (float)cur / max;
                                delta = 1.0f - (delta * delta);
                                break;

                            case Effect.InterpolateType.EASE_OUT:
                                delta = (float)cur / max;
                                delta = delta * delta;
                                break;

                            case Effect.InterpolateType.EASE_IN_3X:
                                delta = 1.0f - (float)cur / max;
                                delta = 1.0f - (delta * delta * delta);
                                break;

                            case Effect.InterpolateType.EASE_OUT_3X:
                                delta = (float)cur / max;
                                delta = delta * delta * delta;
                                break;
                        }

                        posScale.x = (int)Math.Round((float)posScale.x * (1 - delta) + (float)nextPosScale.x * delta);
                        posScale.y = (int)Math.Round((float)posScale.y * (1 - delta) + (float)nextPosScale.y * delta);
                        posScale.scaleX = (int)Math.Round((float)posScale.scaleX * (1 - delta) + (float)nextPosScale.scaleX * delta);
                        posScale.scaleY = (int)Math.Round((float)posScale.scaleY * (1 - delta) + (float)nextPosScale.scaleY * delta);
                    }
                }

                // ノードの回転補間
                if (nodesRotateNext[i] != null)
                {
                    var nextRotate = nodesRotateNext[i].getNodeForEngine(i).rotate;

                    if (nextRotate.interpolate != Effect.InterpolateType.NONE)
                    {
                        // デルタ算出
                        float delta = 0;
                        int max = nodesRotateNext[i].timing - nodesRotate[i].timing;
                        float cur = nowFrame - nodesRotate[i].timing;
                        switch (nextRotate.interpolate)
                        {
                            case Effect.InterpolateType.LINEAR:
                                delta = cur / max;
                                break;

                            case Effect.InterpolateType.EASE_IN:
                                delta = 1.0f - cur / max;
                                delta = 1.0f - (delta * delta);
                                break;

                            case Effect.InterpolateType.EASE_OUT:
                                delta = cur / max;
                                delta = delta * delta;
                                break;

                            case Effect.InterpolateType.EASE_IN_3X:
                                delta = 1.0f - cur / max;
                                delta = 1.0f - (delta * delta * delta);
                                break;

                            case Effect.InterpolateType.EASE_OUT_3X:
                                delta = cur / max;
                                delta = delta * delta * delta;
                                break;
                        }

                        rotate.angle = (int)((float)rotate.angle * (1 - delta) + (float)nextRotate.angle * delta);
                    }
                }

                // セルの切り替えがあるかどうか調べる
                searchNextCell();
            }

            if (elapsed == -1)
                nowFrame += GameMain.getRelativeParam60FPS();
            else
                nowFrame += elapsed;

            if (nextFrameIsEnd)
            {
                isEndPlaying = true;
                nextFrameIsEnd = false;
            }

            // 再生終了を検出
            if (nowFrame > rom.timeline[rom.timeline.Count - 1].timing)
            {
                nextFrameIsEnd = true;
            }
        }

        private void searchNextCell()
        {
            Effect.Frame prev = null;
            foreach (var frame in rom.timeline)
            {
                if (frame.timing > nowFrame)
                {
                    foreach (var node in prev.nodes)
                    {
                        if(node.cell != null)
                            nodes[node.index].cell = node.cell;
                    }

                    return;
                }

                prev = frame;
            }

            // 最後のフレームの場合は明示的に適用してやる必要がある
            if (rom.timeline.Count > 0 && rom.timeline.Last().timing <= nowFrame)
            {
                foreach (var node in rom.timeline.Last().nodes)
                {
                    if (node.cell != null)
                        nodes[node.index].cell = node.cell;
                }
            }
        }

        private void searchNextColor(byte i)
        {
            foreach (var frame in rom.timeline)
            {
                foreach (var node in frame.nodes)
                {
                    if (node.index == i && nodesColorNext[i] == null && node.color != null)
                    {
                        nodesColorNext[i] = frame;
                        return;
                    }
                }

                if (frame == nodesColorNext[i])
                {
                    nodesColorNext[i] = null;
                }
            }
        }

        private void searchNextPosScale(byte i)
        {
            foreach (var frame in rom.timeline)
            {
                foreach (var node in frame.nodes)
                {
                    if (node.index == i && nodesPosScaleNext[i] == null && node.posScale != null)
                    {
                        nodesPosScaleNext[i] = frame;
                        return;
                    }
                }

                if (frame == nodesPosScaleNext[i])
                {
                    nodesPosScaleNext[i] = null;
                }
            }
        }

        private void searchNextRotate(byte i)
        {
            foreach (var frame in rom.timeline)
            {
                foreach (var node in frame.nodes)
                {
                    if (node.index == i && nodesRotateNext[i] == null && node.rotate != null)
                    {
                        nodesRotateNext[i] = frame;
                        return;
                    }
                }

                if (frame == nodesRotateNext[i])
                {
                    nodesRotateNext[i] = null;
                }
            }
        }

        public void draw(int targetX, int targetY, bool withFlash = true)
        {
            if (rom == null || nodes == null)
                return;

            // 画面フラッシュを描画
            if (withFlash)
                Graphics.DrawFillRect(0, 0, Graphics.ViewportWidth * 5, Graphics.ViewportHeight * 5, screenColor.r, screenColor.g, screenColor.b, screenColor.a);

            // 各ノードを描画
            foreach(var node in nodes){
                if (node.cell == null)
                    continue;

                int index = node.cell.srcX >> 6;
                if (images.Count <= index)
                    continue;

                int sizeX = images[index].sizeX;
                int sizeY = images[index].sizeY;
                int texId = images[index].texId;

                int scaledSizeX = (int)(sizeX * (float)node.posScale.scaleX / 100);
                int scaledSizeY = (int)(sizeY * (float)node.posScale.scaleY / 100);
                int x = targetX - scaledSizeX / 2 + node.posScale.x;
                int y = targetY - scaledSizeY / 2 + node.posScale.y;

                Graphics.DrawChipImage(texId, x, y, scaledSizeX, scaledSizeY, node.cell.srcX & 0x3F, node.cell.srcY, node.color.r, node.color.g, node.color.b, node.color.a, node.rotate.angle, 1);
            }
        }

        public void drawFlash()
        {
            // 画面フラッシュを描画
            Graphics.DrawFillRect(0, 0, Graphics.ViewportWidth, Graphics.ViewportHeight, screenColor.r, screenColor.g, screenColor.b, screenColor.a);
        }

        private Color tmpColor = new Color();
        private bool nextFrameIsEnd;
        public Color getNowTargetColor()
        {
            tmpColor.R = targetColor.r;
            tmpColor.G = targetColor.g;
            tmpColor.B = targetColor.b;
            tmpColor.A = targetColor.a;
            return tmpColor;
        }

        public class NodeState
        {
            public byte index;
            public int x;
            public int y;
            public int scaleX;
            public int scaleY;
            public bool hasPosScale;
        }
        public List<NodeState> getAvailableNodePosList()
        {
            var result = new List<NodeState>();

            Effect.Frame curFrame = null;
            
            foreach (var frame in rom.timeline)
            {
                if (frame.timing == nowFrame-1){
                    curFrame = frame;
                    break;
                }
                else if(frame.timing >= nowFrame)
                    break;
            }

            if (nodes == null)
                return result;

            foreach (var node in nodes)
            {
                if (node.cell == null)
                    continue;

                var item = new NodeState();
                item.index = node.index;
                item.x = node.posScale.x;
                item.y = node.posScale.y;
                item.scaleX = node.posScale.scaleX;
                item.scaleY = node.posScale.scaleY;
                if (curFrame != null && curFrame.getNode(node.index).posScale != null)
                {
                    item.hasPosScale = true;
                }

                result.Add(item);
            }

            return result;
        }
    }
}
