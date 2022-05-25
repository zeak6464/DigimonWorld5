using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.Rom
{
    public class Effect : RomItem
    {
        public enum InterpolateType
        {
            NONE,
            LINEAR,
            EASE_IN,
            EASE_OUT,
            EASE_IN_3X,
            EASE_OUT_3X,
        }

        public class ColorDef
        {
            public byte r;
            public byte g;
            public byte b;
            public byte a;
            public bool interpolate;

            public ColorDef()
            {
                r = 255;
                g = 255;
                b = 255;
                a = 255;
            }
        }

        public class FlashDef
        {
            public ColorDef color = new ColorDef();

            public void save(System.IO.BinaryWriter writer)
            {
                writer.Write(color.r);
                writer.Write(color.g);
                writer.Write(color.b);
                writer.Write(color.a);
                writer.Write(color.interpolate);
            }

            public void load(System.IO.BinaryReader reader)
            {
                color.r = reader.ReadByte();
                color.g = reader.ReadByte();
                color.b = reader.ReadByte();
                color.a = reader.ReadByte();
                color.interpolate = reader.ReadBoolean();
            }
        }

        public class PosScaleDef
        {
            public int x;
            public int y;
            public int scaleX;
            public int scaleY;
            public InterpolateType interpolate;
        }

        public class RotateDef
        {
            public int angle;
            public InterpolateType interpolate;
            public int centerX;
            public int centerY;
        }

        public class CellDef
        {
            public byte srcX;
            public byte srcY;
        }

        public class NodeDef
        {
            public byte index;
            public ColorDef color;
            public PosScaleDef posScale;
            public CellDef cell;
            public RotateDef rotate;

            private byte USE_COLOR = 0x1;
            private byte USE_POSSCALE = 0x2;
            private byte USE_CELL = 0x4;
            private byte USE_ROTATE = 0x8;

            public void save(System.IO.BinaryWriter writer)
            {
                writer.Write(index);

                byte flag = 0;
                if (color != null) flag |= USE_COLOR;
                if (posScale != null) flag |= USE_POSSCALE;
                if (cell != null) flag |= USE_CELL;
                if (rotate != null) flag |= USE_ROTATE;
                writer.Write(flag);

                if (color != null)
                {
                    writer.Write(color.r);
                    writer.Write(color.g);
                    writer.Write(color.b);
                    writer.Write(color.a);
                    writer.Write(color.interpolate);
                }

                if (posScale != null)
                {
                    writer.Write(posScale.x);
                    writer.Write(posScale.y);
                    writer.Write(posScale.scaleX);
                    writer.Write(posScale.scaleY);
                    writer.Write((int)posScale.interpolate);
                }

                if (cell != null)
                {
                    writer.Write(cell.srcX);
                    writer.Write(cell.srcY);
                }

                if (rotate != null)
                {
                    writer.Write(rotate.angle);
                    writer.Write((int)rotate.interpolate);
                    writer.Write(rotate.centerX);
                    writer.Write(rotate.centerY);
                }
            }

            public void load(System.IO.BinaryReader reader)
            {
                index = reader.ReadByte();

                int flag = reader.ReadByte();

                if ((flag & USE_COLOR) != 0)
                {
                    color = new ColorDef();
                    color.r = reader.ReadByte();
                    color.g = reader.ReadByte();
                    color.b = reader.ReadByte();
                    color.a = reader.ReadByte();
                    color.interpolate = reader.ReadBoolean();
                }

                if ((flag & USE_POSSCALE) != 0)
                {
                    posScale = new PosScaleDef();
                    posScale.x = reader.ReadInt32();
                    posScale.y = reader.ReadInt32();
                    posScale.scaleX = reader.ReadInt32();
                    posScale.scaleY = reader.ReadInt32();
                    posScale.interpolate = (InterpolateType)reader.ReadInt32();
                }

                if ((flag & USE_CELL) != 0)
                {
                    cell = new CellDef();
                    cell.srcX = reader.ReadByte();
                    cell.srcY = reader.ReadByte();
                }

                if ((flag & USE_ROTATE) != 0)
                {
                    rotate = new RotateDef();
                    rotate.angle = reader.ReadInt32();
                    rotate.interpolate = (InterpolateType)reader.ReadInt32();
                    rotate.centerX = reader.ReadInt32();
                    rotate.centerY = reader.ReadInt32();
                }
            }
        }

        public class Frame : RomItem
        {
            public int timing;
            public FlashDef screenFlash;
            public FlashDef targetFlash;
            public Guid se;
            public List<NodeDef> nodes = new List<NodeDef>();

            private int USE_SCR_FLASH = 0x10000000;
            private int USE_TGT_FLASH = 0x20000000;
            private int USE_SE = 0x40000000;
            private int NODENUM_MASK = 0x0FFFFFFF;

            public override void save(System.IO.BinaryWriter writer)
            {
                writer.Write(timing);

                int flag = nodes.Count;
                if (screenFlash != null) flag |= USE_SCR_FLASH;
                if (targetFlash != null) flag |= USE_TGT_FLASH;
                if (se != Guid.Empty) flag |= USE_SE;
                writer.Write(flag);

                if (screenFlash != null) screenFlash.save(writer);
                if (targetFlash != null) targetFlash.save(writer);
                if (se != Guid.Empty) writer.Write(se.ToByteArray());

                foreach (var node in nodes)
                {
                    node.save(writer);
                }
            }

            public override void load(System.IO.BinaryReader reader)
            {
                timing = reader.ReadInt32();

                int flag = reader.ReadInt32();
                if ((flag & USE_SCR_FLASH) != 0)
                {
                    screenFlash = new FlashDef();
                    screenFlash.load(reader);
                }

                if ((flag & USE_TGT_FLASH) != 0)
                {
                    targetFlash = new FlashDef();
                    targetFlash.load(reader);
                }

                if ((flag & USE_SE) != 0)
                {
                    se = Util.readGuid(reader);
                }

                nodes.Clear();
                int count = flag & NODENUM_MASK;
                for (int i = 0; i < count; i++)
                {
                    var node = new NodeDef();
                    node.load(reader);
                    nodes.Add(node);
                }
            }

            public NodeDef getNode(byte index, bool create = true)
            {
                // 指定したインデックスのノードがあるか探す
                foreach (var node in nodes)
                {
                    // あったら返す
                    if (node.index == index)
                        return node;
                }

                if (create)
                {
                    // なければ作って返す
                    var newnode = new NodeDef();
                    newnode.index = index;
                    nodes.Add(newnode);
                    sort();

                    return newnode;
                }

                return null;
            }

            private void sort()
            {
                nodes.Sort(
                    delegate(NodeDef a, NodeDef b)
                    {
                        if (a.index == b.index) return 0;
                        return a.index > b.index ? 1 : -1;
                    }
                );
            }

            public void setNode(NodeDef node)
            {
                // 指定したインデックスのノードがあるか探す
                for (int i = 0; i < nodes.Count; i++)
                {
                    // あったら代入
                    if (nodes[i].index == node.index)
                    {
                        nodes[i] = node;
                        break;
                    }
                }

                // ない場合は何もしない
            }

            public NodeDef getNodeForEngine(byte index)
            {
                // 指定したインデックスのノードがあるか探す
                foreach (var node in nodes)
                {
                    // あったら返す
                    if (node.index == index)
                        return node;
                }

                // なければ作って返す
                var newnode = new NodeDef();
                newnode.index = index;
                newnode.color = new ColorDef();
                newnode.posScale = new PosScaleDef();

                return newnode;
            }

            public void removeNode(byte index)
            {
                // 指定したインデックスのノードがあるか探す
                for (int i = 0; i < nodes.Count; i++)
                {
                    // あったら削除
                    if (nodes[i].index == index)
                    {
                        nodes.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public Guid graphic;
        public int nodeNum = 1;
        public int numTiming;
        public List<Frame> timeline = new List<Frame>();

        public List<Guid> extraGraphic = new List<Guid>();

        public enum OrigPos{
            ORIG_CENTER,
            ORIG_FOOT,
            ORIG_SCREEN,
        }
        public OrigPos origPos = OrigPos.ORIG_CENTER;

        public Frame getFrame(int index, bool create = true)
        {
            if (index < 0)
                return null;

            if (create)
            {
                while (index >= timeline.Count)
                {
                    var frame = new Frame();
                    if (timeline.Count > 0)
                        frame.timing = timeline[timeline.Count - 1].timing + 1;
                    timeline.Add(frame);
                }
            }
            else
            {
                if (timeline.Count <= index)
                    return null;
            }
            return timeline[index];
        }

        public Frame getFrameByTiming(int timing, bool create = true)
        {
            if (timing < 0)
                return null;

            // タイミング数ベースでフレームを探す
            int insertIndex = 0;
            foreach (var frame in timeline)
            {
                if (frame.timing == timing)
                {
                    return frame;
                }
                else if (frame.timing > timing)
                {
                    break;
                }

                insertIndex++;
            }

            // 存在しない場合は作る
            if (create)
            {
                var frame = new Common.Rom.Effect.Frame();
                frame.timing = timing;
                timeline.Insert(insertIndex, frame);
                return frame;
            }

            return null;
        }

        public override void save(System.IO.BinaryWriter writer)
        {
            base.save(writer);

            writer.Write(graphic.ToByteArray());
            writer.Write(nodeNum);
            writer.Write(numTiming);

            writer.Write(timeline.Count);
            foreach (var frame in timeline)
            {
                writeChunk(writer, frame);
            }

            writer.Write(extraGraphic.Count);
            foreach (var exGuid in extraGraphic)
            {
                writer.Write(exGuid.ToByteArray());
            }

            writer.Write((int)origPos);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            base.load(reader);

            graphic = Util.readGuid(reader);
            nodeNum = reader.ReadInt32();
            numTiming = reader.ReadInt32();

            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var frame = new Frame();
                readChunk(reader, frame);
                timeline.Add(frame);
            }

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var guid = Util.readGuid(reader);
                extraGraphic.Add(guid);
            }

            origPos = (OrigPos)reader.ReadInt32();
        }

        public void setTiming(int index, int value)
        {
            // フレーム数が 0 以外なのに value に 0 が入っているのはおかしい
            if (index != 0 && value == 0)
                return;

            // 他のフレームとかぶってたら駄目です
            for (int i = 0; i < timeline.Count; i++)
            {
                if (i == index)
                    continue;

                if (timeline[i].timing == value)
                    return;
            }

            getFrame(index).timing = value;

            // ソートしてフレーム順に並べる
            timeline.Sort(
                delegate(Frame a, Frame b)
                {
                    if (a.timing == b.timing) return 0;
                    return a.timing > b.timing ? 1 : -1;
                }
            );
        }
    }
}
