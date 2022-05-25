using System;
using System.Collections.Generic;
using System.IO;

namespace Yukar.Common.GameData
{
    public class CameraSettings : IGameDataItem
    {
        public Common.Rom.Map.CameraControlMode cameraControlMode;
        public float yangle;
        public float xangle;
        public float fovy;
        public float dist;
        public float eyeHeight;
        public SharpKmyMath.Vector3 lookAtTarget;
        public bool useLookAtTargetPos;

        void IGameDataItem.save(System.IO.BinaryWriter writer)
        {
            writer.Write((int)cameraControlMode);
            writer.Write(xangle);
            writer.Write(yangle);
            writer.Write(fovy);
            writer.Write(dist);
            writer.Write(eyeHeight);
            writer.Write(lookAtTarget.x);
            writer.Write(lookAtTarget.y);
            writer.Write(lookAtTarget.z);
            writer.Write(useLookAtTargetPos);
        }

        void IGameDataItem.load(Catalog catalog, System.IO.BinaryReader reader)
        {
            cameraControlMode = (Rom.Map.CameraControlMode)reader.ReadInt32();
            xangle = reader.ReadSingle();
            yangle = reader.ReadSingle();
            fovy = reader.ReadSingle();
            dist = reader.ReadSingle();
            eyeHeight = reader.ReadSingle();
            lookAtTarget = new SharpKmyMath.Vector3();
            lookAtTarget.x = reader.ReadSingle();
            lookAtTarget.y = reader.ReadSingle();
            lookAtTarget.z = reader.ReadSingle();
            useLookAtTargetPos = reader.ReadBoolean();
        }
    }

    public class Followers : IGameDataItem
    {
        public class Entry : IGameDataItem
        {
            public enum FollowerType
            {
                PARTY_MEMBER,
                GRAPHIC,
            }

            public Guid graphic;
            public FollowerType type;
            public int partyIndex = -1;

            public Entry()
            {
                graphic = Guid.Empty;
            }

            void IGameDataItem.load(Catalog catalog, BinaryReader reader)
            {
                graphic = Util.readGuid(reader);
                type = (FollowerType)reader.ReadInt32();
                partyIndex = reader.ReadInt32();
            }

            void IGameDataItem.save(BinaryWriter writer)
            {
                writer.Write(graphic.ToByteArray());
                writer.Write((int)type);
                writer.Write(partyIndex);
            }
        }

        public List<Entry> list = new List<Entry>();
        public bool visible = false;

        void IGameDataItem.load(Catalog catalog, BinaryReader reader)
        {
            visible = reader.ReadBoolean();
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var entry = new Entry();
                GameDataManager.readChunk(catalog, entry, reader);
                list.Add(entry);
            }
        }

        void IGameDataItem.save(BinaryWriter writer)
        {
            writer.Write(visible);
            writer.Write(list.Count);
            foreach (var entry in list)
            {
                GameDataManager.saveChunk(entry, writer);
            }
        }
    }

    public class GameOverSettings : IGameDataItem
    {
        public enum GameOverType
        {
            DEFAULT,
            RIVIVAL,
            ADVANCED_RIVIVAL,
        }
        public enum RivivalType
        {
            SOLO,
            ALL,
        }

        public GameOverType gameOverType;
        public Guid mapGuid;
        public int x;
        public int y;
        public RivivalType rivivalType;
        public int rivivalHp;
        public int rivivalMp;
        public Guid eventGuid;

        public GameOverSettings()
        {
            gameOverType = GameOverType.DEFAULT;
            mapGuid = Guid.Empty;
            x = 0;
            y = 0;
            rivivalType = RivivalType.SOLO;
            rivivalHp = -1;
            rivivalMp = -1;
            eventGuid = Guid.Empty;
        }

        void IGameDataItem.save(System.IO.BinaryWriter writer)
        {
            writer.Write((int)gameOverType);
            writer.Write(mapGuid.ToByteArray());
            writer.Write(x);
            writer.Write(y);
            writer.Write((int)rivivalType);
            writer.Write(rivivalHp);
            writer.Write(rivivalMp);
            writer.Write(eventGuid.ToByteArray());
        }

        void IGameDataItem.load(Catalog catalog, System.IO.BinaryReader reader)
        {
            gameOverType = (GameOverType)reader.ReadInt32();
            mapGuid = Util.readGuid(reader);
            x = reader.ReadInt32();
            y = reader.ReadInt32();
            rivivalType = (RivivalType)reader.ReadInt32();
            rivivalHp = reader.ReadInt32();
            rivivalMp = reader.ReadInt32();
            eventGuid = Util.readGuid(reader);
        }
    }

    public class BgmPlaySettings : IGameDataItem
    {
        public Guid currentBgm = Resource.Bgm.sNotChangeItem.guId;
        public float pan = 0f;
        public float volume = 1.0f;
        public float tempo = 1.0f;

        public void load(Catalog catalog, BinaryReader reader)
        {
            currentBgm = Util.readGuid(reader);
            pan = reader.ReadSingle();
            volume = reader.ReadSingle();
            tempo = reader.ReadSingle();
        }

        public void save(BinaryWriter writer)
        {
            writer.Write(currentBgm.ToByteArray());
            writer.Write(pan);
            writer.Write(volume);
            writer.Write(tempo);
        }
    }

    public class StartSettings : IGameDataItem
    {
        // 初期位置情報
        public Guid map;
        public int x;
        public int y;
        public float height;
        public bool heightAvailable;    // 古いセーブデータとの互換用
        public int dir = -1;

        public List<Event> events;      // イベントの状態
        public CameraSettings camera;   // カメラの状態
        public List<Sprite> sprites;    // スプライトの状態
        public Followers followers = new Followers();     // 隊列の状態
        public GameOverSettings gameOverSettings = new GameOverSettings(); // ゲームオーバーの状態
        public BgmPlaySettings currentBgm = new BgmPlaySettings();  // セーブ時のBGMの状態
        public BgmPlaySettings currentBgs = new BgmPlaySettings();

        // プレイヤーとカメラの操作禁止状態
        public bool plLock;
        public bool camLockedByEvent;
        public bool camLock;
        public bool camModeLock;
        public bool camLockX;
        public bool camLockY;
        public bool camLockZoom;

        void IGameDataItem.save(BinaryWriter writer)
        {
            writer.Write(map.ToByteArray());
            writer.Write(x);
            writer.Write(y);

            // イベント情報
            if (events != null)
            {
                writer.Write(events.Count);
                foreach (var ev in events)
                {
                    GameDataManager.saveChunk(ev, writer);
                }
            }
            else
            {
                writer.Write(0);
            }

            // プレイヤーの高さ
            writer.Write(height);

            // カメラ設定
            GameDataManager.saveChunk(camera, writer);

            // 表示中のスプライト情報
            if (sprites != null)
            {
                writer.Write(sprites.Count);
                foreach (var sp in sprites)
                {
                    GameDataManager.saveChunk(sp, writer);
                }
            }
            else
            {
                writer.Write(0);
            }

            // プレイヤーの向き
            writer.Write(dir);

            // プレイヤーとカメラの操作禁止状態
            writer.Write(plLock);
            writer.Write(camLockedByEvent);
            writer.Write(camLock);
            writer.Write(camModeLock);

            // 隊列の状態
            GameDataManager.saveChunk(followers, writer);

            // ゲームオーバー時の挙動設定
            GameDataManager.saveChunk(gameOverSettings, writer);

            // セーブ時に再生していたBGM
            GameDataManager.saveChunk(currentBgm, writer);
            GameDataManager.saveChunk(currentBgs, writer);

            // カメラ操作禁止の個別指定
            writer.Write(camLockX);
            writer.Write(camLockY);
            writer.Write(camLockZoom);
        }

        void IGameDataItem.load(Catalog catalog, BinaryReader reader)
        {
            map = Util.readGuid(reader);
            x = reader.ReadInt32();
            y = reader.ReadInt32();

            int count = reader.ReadInt32();
            if (count > 0)
                events = new List<Event>();
            for (int i = 0; i < count; i++)
            {
                var ev = new Event();
                GameDataManager.readChunk(catalog, ev, reader);
                events.Add(ev);
            }

            height = reader.ReadSingle();
            heightAvailable = true;

            var cam = new CameraSettings();
            GameDataManager.readChunk(catalog, cam, reader);
            camera = cam;

            count = reader.ReadInt32();
            if (count > 0)
                sprites = new List<Sprite>();
            for (int i = 0; i < count; i++)
            {
                var sp = new Sprite();
                GameDataManager.readChunk(catalog, sp, reader);
                sprites.Add(sp);
            }

            dir = reader.ReadInt32();

            plLock = reader.ReadBoolean();
            camLockedByEvent = reader.ReadBoolean();
            camLock = reader.ReadBoolean();
            camModeLock = reader.ReadBoolean();

            GameDataManager.readChunk(catalog, followers, reader);

            // ゲームオーバー時の挙動設定
            var loadGameOverSetting = new GameOverSettings();
            GameDataManager.readChunk(catalog, loadGameOverSetting, reader);
            gameOverSettings = loadGameOverSetting;

            // セーブ時に再生していたBGM
            GameDataManager.readChunk(catalog, currentBgm, reader);
            GameDataManager.readChunk(catalog, currentBgs, reader);

            camLockX = reader.ReadBoolean();
            camLockY = reader.ReadBoolean();
            camLockZoom = reader.ReadBoolean();
        }
    }
}
