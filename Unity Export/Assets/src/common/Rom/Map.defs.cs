using System;
using System.Collections.Generic;

namespace Yukar.Common.Rom
{
    public class ThirdPersonCameraSettings : RomItem
    {
        public float xAngle = -48;
        public float yAngle;
        public float zAngle; // 具合が悪かったら使わないかも
        public float distance = 70;
        public float fov = 3.5f;
        public float x = -10000;
        public float y;
        public float height;
        public float eyeHeight = 0.5f;

        public override void save(System.IO.BinaryWriter writer)
        {
            writer.Write(xAngle);
            writer.Write(yAngle);
            writer.Write(zAngle);
            writer.Write(distance);
            writer.Write(fov);
            writer.Write(x);
            writer.Write(y);
            writer.Write(height);
            writer.Write(eyeHeight);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            xAngle = reader.ReadSingle();
            yAngle = reader.ReadSingle();
            zAngle = reader.ReadSingle();
            distance = reader.ReadSingle();
            fov = reader.ReadSingle();
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            height = reader.ReadSingle();

            if (reader.BaseStream.Position == reader.BaseStream.Length)
            {
                if (x == -1) x = -10000;
            }
            else
            {
                eyeHeight = reader.ReadSingle();
            }
        }

        public bool compare(ThirdPersonCameraSettings rom)
        {
            return xAngle == rom.xAngle && yAngle == rom.yAngle && distance == rom.distance && fov == rom.fov;
        }

        public void copyFrom(ThirdPersonCameraSettings rom)
        {
            xAngle = rom.xAngle;
            yAngle = rom.yAngle;
            zAngle = rom.zAngle;
            distance = rom.distance;
            fov = rom.fov;
            x = rom.x;
            y = rom.y;
            height = rom.height;
        }
    }

    public class FirstPersonCameraSettings : RomItem
    {
        public float fov = 30;
        public float eyeHeight = 0.9f;

        public override void save(System.IO.BinaryWriter writer)
        {
            writer.Write(fov);
            writer.Write(eyeHeight);
        }

        public override void load(System.IO.BinaryReader reader)
        {
            fov = reader.ReadSingle();
            eyeHeight = reader.ReadSingle();
        }

        public bool compare(FirstPersonCameraSettings rom)
        {
            return eyeHeight == rom.eyeHeight && fov == rom.fov;
        }
    }

    public partial class Map : RomItem
    {
        public enum OutsideType
        {
            NOTHING,
            REPEAT,
            MAPCHIP,
        }

        public enum BgType
        {
            COLOR,
            MODEL,
        }

        public enum EnvironmentEffectType
        {
            NONE,
            RAIN,
            SNOW,
            STORM,
            MIST,
            COLD_WIND,
            CONFETTI,
        }

        public static readonly List<Guid> MAPBG_DICT_ID_TO_GUID = new List<Guid>()
        {
            Guid.Empty, //NONE
            new Guid("609eb589-9654-44b2-a1ad-697cf801e40c"), //DAY
            new Guid("c9ffcd55-d068-4f9b-b8bc-abca51877db4"), //EVENING
            new Guid("01aee58b-c33d-463d-8a8b-c6bbeb2b8b54"), //NIGHT
            new Guid("1144dcf8-8241-43cb-bb07-13cda28ec782"), //CLOUDY
            new Guid("b2886a53-ef64-4b75-bde4-52839bc894fc"), //HELL
            new Guid("7b0c731e-dc34-4561-a765-a93b2257916a"), //CLOUDY_NIGHT
        };

        public const int TERRAIN_DEFAULT_WIDTH = 30;
        public const int TERRAIN_DEFAULT_HEIGHT = 30;

        public const int DEFAULT_WIDTH = 20;
        public const int DEFAULT_HEIGHT = 12;

        public const int MAXMAPSIZE = 256;

        public const int ILLEGAL_STAIR_STAT = -100;

        public const string READ_ONLY_CATEGORY = "!ReadOnly";

        public static readonly Guid DEFAULT_3D_BATTLEBG = new Guid("bc5b2bff-f36d-44d2-a306-fafa11ef8a77");

        public static readonly Dictionary<Guid, Guid> BATTLEBG_DICT_2D_TO_3D = new Dictionary<Guid, Guid>()
        {
            {new Guid("4fc25e2d-f086-4152-90fe-20046fa51d8b"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("d9352417-a126-4306-a9a8-6d5c3fb12467"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("b30db9bb-3089-4b6c-aca6-40cb2d7eb95e"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("a31c3939-cdc6-4a04-bd3d-ac823097be37"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("6770b48c-579d-4fcc-aea2-455d5037bdaf"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("c02db15f-8841-4a30-bd53-8e6c92a664f9"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("a25dc5e2-ac24-4f09-9bff-e2091b950be6"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("3ed2c266-b0aa-42b2-b20d-3d94dfc1fc61"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("1bcf9ef9-5c7e-4360-85d1-3ab99fde91b3"), new Guid("2fa5391e-3333-4455-b2dc-3f48bf55302c")},
            {new Guid("ca74b28c-a80f-431f-a542-b39be3844bd6"), new Guid("2fa5391e-3333-4455-b2dc-3f48bf55302c")},
            {new Guid("19ed58ea-c086-42e6-b319-f82726e4a9b5"), new Guid("2fa5391e-3333-4455-b2dc-3f48bf55302c")},
            {new Guid("742d2da7-da90-40ff-9765-8ed929fdc80c"), new Guid("2fa5391e-3333-4455-b2dc-3f48bf55302c")},
            {new Guid("0a4cbcd3-97da-4857-83df-88d576d37f1a"), new Guid("3acf943d-d900-4262-b02e-16caaabab41d")},
            {new Guid("380d15bf-235f-46c9-9680-771e5cf10848"), new Guid("426b9544-412a-44de-87b6-1fc076b4211d")},
            {new Guid("c3fca8eb-4d96-42a3-af43-2a6cc6cafb12"), new Guid("426b9544-412a-44de-87b6-1fc076b4211d")},
            {new Guid("397027d5-5028-4369-ac87-3318fa2d6658"), new Guid("426b9544-412a-44de-87b6-1fc076b4211d")},
            {new Guid("9f1dd16d-0436-4513-a70c-5053b5962ebd"), new Guid("426b9544-412a-44de-87b6-1fc076b4211d")},
            {new Guid("69e39b6f-571a-4e50-8d74-f1deda2c6252"), new Guid("426b9544-412a-44de-87b6-1fc076b4211d")},
            {new Guid("ae3c6331-a4ca-4c4d-b3fa-03b98ef1cd52"), new Guid("3491b6e6-0054-4664-9805-bc6781220f1a")},
            {new Guid("c510d390-bc98-4491-8d37-666ec43825e0"), new Guid("3491b6e6-0054-4664-9805-bc6781220f1a")},
            {new Guid("a0de789a-1660-47e2-988e-78c466b96fa9"), new Guid("3491b6e6-0054-4664-9805-bc6781220f1a")},
            {new Guid("4eba59cd-eac5-4ae4-95db-c86f82511749"), new Guid("3491b6e6-0054-4664-9805-bc6781220f1a")},
            {new Guid("ff8f1a26-fd69-4c62-9a1d-ffa7b09d8667"), new Guid("16f91c50-9f93-4ef1-b98d-69f069138454")},
            {new Guid("88d3feb8-ad04-453c-89c7-aef7afc257bf"), new Guid("16f91c50-9f93-4ef1-b98d-69f069138454")},
            {new Guid("b7e97fe2-007e-4e9e-9443-9a7f4a61c86b"), new Guid("16f91c50-9f93-4ef1-b98d-69f069138454")},
            {new Guid("4ae1c218-922e-4171-8e04-0f3a34dd712e"), new Guid("16f91c50-9f93-4ef1-b98d-69f069138454")},
            {new Guid("29b6104b-9a4c-454b-8f00-c8664d308641"), new Guid("02581a36-cdb6-4360-a919-ba51f3358b70")},
            {new Guid("8b90911e-1bc3-4cb7-a1eb-196ff5f300c3"), new Guid("8be48803-c7f9-45d1-847e-e8dc3a5ccd81")},
            {new Guid("909796bb-aedc-4d58-a19c-02fe360e56ec"), new Guid("8be48803-c7f9-45d1-847e-e8dc3a5ccd81")},
            {new Guid("508e2250-3d51-4f61-983b-79e15d14514b"), new Guid("8be48803-c7f9-45d1-847e-e8dc3a5ccd81")},
            {new Guid("7f30da90-ad0e-4502-b010-d9f3c2fdc3b6"), new Guid("8be48803-c7f9-45d1-847e-e8dc3a5ccd81")},
            {new Guid("dfc92408-34e2-4327-9be6-7a2402fdddb9"), new Guid("8be48803-c7f9-45d1-847e-e8dc3a5ccd81")},
            {new Guid("133d4641-29e6-44dd-9186-a48159392397"), new Guid("2c37a119-3642-4b7b-b126-3f96c67f26f3")},
            {new Guid("a6b7e9a4-f956-44d5-91aa-83d061e52773"), new Guid("2c37a119-3642-4b7b-b126-3f96c67f26f3")},
            {new Guid("f9425fa3-adee-466b-8af4-a95abd567fd1"), new Guid("2c37a119-3642-4b7b-b126-3f96c67f26f3")},
            {new Guid("90a1817d-6a5c-4043-9804-d170757fa36e"), new Guid("2c37a119-3642-4b7b-b126-3f96c67f26f3")},
            {new Guid("64381a2a-69a1-4285-bf6a-8c94403359ea"), new Guid("2c37a119-3642-4b7b-b126-3f96c67f26f3")},
            {new Guid("a3dc1d62-3882-423d-8c75-016b483740e0"), new Guid("7e1741b3-371c-43c3-b9b2-049d39cd5980")},
            {new Guid("987d6d2b-d777-475f-825e-91d84bd90702"), new Guid("7e1741b3-371c-43c3-b9b2-049d39cd5980")},
            {new Guid("50636ca3-9e3f-4b6f-bf21-c8cdbce93a93"), new Guid("7e1741b3-371c-43c3-b9b2-049d39cd5980")},
            {new Guid("8106c59b-841e-4c09-9e57-69754388c60b"), new Guid("7e1741b3-371c-43c3-b9b2-049d39cd5980")},
            {new Guid("13ee4d1b-274b-422d-9531-7b13d30db4f2"), new Guid("704022b6-97a2-4f06-a071-59a29287e173")},
            {new Guid("7fc243a3-ebdf-49ec-b92e-1760004be599"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("15660036-7dd8-4b59-ae70-4a27f7ec806f"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("6605feb9-36ab-4e4c-8b25-a5febacbe90a"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("501e54c6-2dc1-43e0-88fc-fa0837ea3e42"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("38a567c5-265c-47ac-9c9b-b24574673d01"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("1ba5c9b3-63bd-4ce4-a0ae-80c77eed6920"), new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f")},
            {new Guid("3ed1d8e5-16ee-40f9-9ffd-b0909c4a17c5"), new Guid("0f2ca7ce-e199-48f8-beb4-861ba14ea44a")},
            {new Guid("7cd94cda-c4dd-4a15-9c75-155d1cd910e9"), new Guid("e312dec3-04fb-491b-af89-e1570dab6460")},
            {new Guid("998ba3a0-50e2-440c-93de-eed9125d704f"), new Guid("e312dec3-04fb-491b-af89-e1570dab6460")},
            {new Guid("a18499c3-8fbc-4c6b-b7b3-aab1a67a8ee6"), new Guid("e312dec3-04fb-491b-af89-e1570dab6460")},
            {new Guid("f6de5e8c-b37c-4c0f-904a-f3ed81d424d3"), new Guid("e312dec3-04fb-491b-af89-e1570dab6460")},
            {new Guid("c88108a3-fa45-4253-ae5f-22aacf4dcc25"), new Guid("e312dec3-04fb-491b-af89-e1570dab6460")},
            {new Guid("1a4eec0d-887c-4cc3-bdbd-54140fa6ca4b"), new Guid("c206dd8f-74a4-4dd2-9cfb-b38e9dc352d9")},
            {new Guid("86950b84-cf1f-463c-bb17-517a1f345938"), new Guid("c206dd8f-74a4-4dd2-9cfb-b38e9dc352d9")},
            {new Guid("75d744d0-7323-49a0-b52d-a19dbaa245ae"), new Guid("c206dd8f-74a4-4dd2-9cfb-b38e9dc352d9")},
            {new Guid("26147144-a0f3-468a-8ef8-dd6a13c7ed78"), new Guid("c206dd8f-74a4-4dd2-9cfb-b38e9dc352d9")},
            {new Guid("2f659b07-3766-4f1c-aae8-3660b2354764"), new Guid("753d0d06-30f4-435a-b996-d3ac860c2318")},
            {new Guid("33f9d684-2d80-44c7-bb05-c223df642c6a"), new Guid("753d0d06-30f4-435a-b996-d3ac860c2318")},
            {new Guid("eeb1e440-6cc9-4394-bcbf-76b6526fb37c"), new Guid("7a5928c8-5c74-4965-a44f-e90131ee3c6d")},
            {new Guid("619d663a-19b0-42a8-adc0-2d4db3bc0359"), new Guid("753d0d06-30f4-435a-b996-d3ac860c2318")},
            {new Guid("28a9ea76-1092-474e-911d-d0340f473eed"), new Guid("753d0d06-30f4-435a-b996-d3ac860c2318")},
            {new Guid("0c03778e-5a71-4bd3-9338-9fc0e7faa96f"), new Guid("7a5928c8-5c74-4965-a44f-e90131ee3c6d")},
            {new Guid("68f4a94f-5df9-4b2f-8925-15d98e46f69d"), new Guid("753d0d06-30f4-435a-b996-d3ac860c2318")},
            {new Guid("774467f9-cf47-41a6-a6e1-cb3c8e1bc380"), new Guid("753d0d06-30f4-435a-b996-d3ac860c2318")},
            {new Guid("ed78b8ca-7188-45dc-8766-49c3427d7899"), new Guid("4a33c11a-11ca-43e6-9079-c2b403348456")},
            {new Guid("53c2ae6e-b5e9-42d8-9884-10d5e77539e7"), new Guid("4a33c11a-11ca-43e6-9079-c2b403348456")},
            {new Guid("3d148f7d-b167-4511-aa5f-54641490bfd3"), new Guid("4a33c11a-11ca-43e6-9079-c2b403348456")},
            {new Guid("6a5ca315-852a-42f1-9312-361d4fa8dc43"), new Guid("4a33c11a-11ca-43e6-9079-c2b403348456")},
            {new Guid("a6711cce-5215-4594-95a8-fc1a9118a305"), new Guid("4a33c11a-11ca-43e6-9079-c2b403348456")},
        };

        public static readonly Dictionary<Guid, Guid> BATTLEBG_DICT_3D_TO_2D = new Dictionary<Guid, Guid>()
        {
            {new Guid("426b9544-412a-44de-87b6-1fc076b4211d"), new Guid("380d15bf-235f-46c9-9680-771e5cf10848")},
            {new Guid("2c37a119-3642-4b7b-b126-3f96c67f26f3"), new Guid("133d4641-29e6-44dd-9186-a48159392397")},
            {new Guid("c206dd8f-74a4-4dd2-9cfb-b38e9dc352d9"), new Guid("1a4eec0d-887c-4cc3-bdbd-54140fa6ca4b")},
            {new Guid("3491b6e6-0054-4664-9805-bc6781220f1a"), new Guid("ae3c6331-a4ca-4c4d-b3fa-03b98ef1cd52")},
            {new Guid("7a5928c8-5c74-4965-a44f-e90131ee3c6d"), new Guid("eeb1e440-6cc9-4394-bcbf-76b6526fb37c")},
            {new Guid("2fa5391e-3333-4455-b2dc-3f48bf55302c"), new Guid("1bcf9ef9-5c7e-4360-85d1-3ab99fde91b3")},
            {new Guid("16f91c50-9f93-4ef1-b98d-69f069138454"), new Guid("ff8f1a26-fd69-4c62-9a1d-ffa7b09d8667")},
            {new Guid("7e1741b3-371c-43c3-b9b2-049d39cd5980"), new Guid("a3dc1d62-3882-423d-8c75-016b483740e0")},
            {new Guid("583e5d5c-2f45-4561-a85a-57ab8b90164f"), new Guid("7fc243a3-ebdf-49ec-b92e-1760004be599")},
            {new Guid("e312dec3-04fb-491b-af89-e1570dab6460"), new Guid("7cd94cda-c4dd-4a15-9c75-155d1cd910e9")},
            {new Guid("3acf943d-d900-4262-b02e-16caaabab41d"), new Guid("0a4cbcd3-97da-4857-83df-88d576d37f1a")},
            {new Guid("02581a36-cdb6-4360-a919-ba51f3358b70"), new Guid("29b6104b-9a4c-454b-8f00-c8664d308641")},
            {new Guid("704022b6-97a2-4f06-a071-59a29287e173"), new Guid("13ee4d1b-274b-422d-9531-7b13d30db4f2")},
            {new Guid("0f2ca7ce-e199-48f8-beb4-861ba14ea44a"), new Guid("3ed1d8e5-16ee-40f9-9ffd-b0909c4a17c5")},
            {new Guid("6d2e2dd5-099e-4743-9098-350acfcdc541"), new Guid("7cd94cda-c4dd-4a15-9c75-155d1cd910e9")},
            {new Guid("2f823b5a-5701-4214-a981-293a195c8606"), new Guid("998ba3a0-50e2-440c-93de-eed9125d704f")},
            {new Guid("4fe4d044-5bf4-4399-bd65-2f59e896d452"), new Guid("998ba3a0-50e2-440c-93de-eed9125d704f")},
            {new Guid("753d0d06-30f4-435a-b996-d3ac860c2318"), new Guid("2f659b07-3766-4f1c-aae8-3660b2354764")},
            {new Guid("4a33c11a-11ca-43e6-9079-c2b403348456"), new Guid("ed78b8ca-7188-45dc-8766-49c3427d7899")},
            {new Guid("0d8ccc56-8415-4ad7-9cfd-067bd361ed67"), new Guid("619d663a-19b0-42a8-adc0-2d4db3bc0359")},
            {new Guid("eabd5723-42e6-4417-b614-9a3b727539d1"), new Guid("c510d390-bc98-4491-8d37-666ec43825e0")},
            {new Guid("8be48803-c7f9-45d1-847e-e8dc3a5ccd81"), new Guid("8b90911e-1bc3-4cb7-a1eb-196ff5f300c3")},
        };

        public static Guid doConvertBattleBg(Catalog catalog, Guid battleBg)
        {
            var result = battleBg;
            if (catalog.getGameSettings().battleType == GameSettings.BattleType.CLASSIC)
            {
                if (BATTLEBG_DICT_3D_TO_2D.ContainsKey(battleBg))
                    result = BATTLEBG_DICT_3D_TO_2D[battleBg];
                else if (catalog.getItemFromGuid(battleBg) is Common.Rom.Map)
                    result = Guid.Empty;
            }
            else
            {
                if (BATTLEBG_DICT_2D_TO_3D.ContainsKey(battleBg))
                    result = BATTLEBG_DICT_2D_TO_3D[battleBg];
                else if (battleBg == Guid.Empty || catalog.getItemFromGuid(battleBg) is Common.Resource.BattleBackground)
                    result = DEFAULT_3D_BATTLEBG;
            }
            return result;
        }
    }

    public class BattleBgSettings
    {
        public Common.Rom.RomItem bgRom;
        public int centerX;
        public int centerY;
    }
}
