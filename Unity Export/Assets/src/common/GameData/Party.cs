using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yukar.Common.GameData
{
    public class Party : IGameDataItem
    {
        public const int MAX_PARTY = 4;
        public const int MAX_ITEM = 99;
        public const int MAX_MONEY = 9999999;

        // パーティメンバー
        public List<Hero> members = new List<Hero>();

        // 控えメンバー(一度でもパーティに所属したことのあるメンバーはステータスや装備などを記憶する)
        public List<Hero> others = new List<Hero>();

        // アイテム袋
        public class ItemStack
        {
            public Common.Rom.Item item;
            public int num;
        }
        public List<ItemStack> items = new List<ItemStack>();

        // 変更済みグラフィック
        public class ChangedGraphic
        {
            public Guid face;
            public Guid model;
        }
        public Dictionary<Guid, ChangedGraphic> changedHeroGraphic = new Dictionary<Guid, ChangedGraphic>();

        // 変更済みの名前
        private Dictionary<Guid, string> changedHeroName = new Dictionary<Guid, string>();
        public string getHeroName(Guid guid)
        {
            if (changedHeroName.ContainsKey(guid))
                return changedHeroName[guid];
            var rom = catalog.getItemFromGuid(guid);
            if (rom == null)
                return "";
            return rom.name;
        }
        public void setHeroName(Guid guid, string newName)
        {
            changedHeroName[guid] = newName;
        }

        // 所持金
        private int money;

        private Catalog catalog;

        // 経験値テーブル
        public static int[,] expTable = new int[5, Common.GameData.Hero.MAX_LEVEL];

        // 既に手に入れたアイテム図鑑
        public Dictionary<Guid, int> itemDict = new Dictionary<Guid, int>();

        // インベントリ最大数
        private int inventoryMax = -1;

        public Party(Catalog catalog){
            this.catalog = catalog;
            createExpTable();
            inventoryMax = catalog.getGameSettings().inventoryMax;
        }

        public static void createExpTable()
        {
            int old = 0;
            for (int i = 1; i <= Common.GameData.Hero.MAX_LEVEL; i++)
            {
                expTable[0, i - 1] = (int)((old + Math.Min((int)(Math.Pow(i, 3)) + i * 25, 120000)) * 1.02);
                expTable[1, i - 1] = (int)((old + Math.Min((int)(Math.Pow(i, 3)) + i * 25, 120000)) * 1.01);
                expTable[2, i - 1] = old + Math.Min((int)(Math.Pow(i, 3)) + i * 25, 120000);
                expTable[3, i - 1] = (int)((old + Math.Min((int)(Math.Pow(i, 3)) + i * 25, 120000)) * 0.99);
                expTable[4, i - 1] = (int)((old + Math.Min((int)(Math.Pow(i, 3)) + i * 25, 120000)) * 0.98);
                old = expTable[2, i - 1];
            }
        }

        public Common.GameData.Hero AddMember(Common.Rom.Hero rom)
        {
            if (rom == null)
                return null;

            // 控えから見つかったら控え→パーティに加える
            foreach (var hero in others)
            {
                if (hero.rom == rom)
                {
                    others.Remove(hero);
                    members.Add(hero);
                    return null;
                }
            }

            // 見つからなかったら、ROMから新しくGameDataを生成する
            var data = createHeroFromRom(catalog, rom);

            members.Add(data);

            return data;
        }

        // マップキャラのステータスで使用されているキャラの取得用
        public Common.GameData.Hero GetHero(Guid inId)
        {
            var hero = GetMember(inId, true);

            if (hero != null)
            {
                return hero;
            }

            // 見つからなかったら、ROMから新しくGameDataを生成する
            var rom = catalog.getItemFromGuid(inId) as Common.Rom.Hero;

            if (rom == null)
            {
                return null;
            }

            var data = createHeroFromRom(catalog, rom);

            others.Add(data);

            return data;
        }

        // ROMから新しくGameDataを生成する
        public static Common.GameData.Hero createHeroFromRom(Catalog catalog, Rom.Hero rom)
        {
            var data = new Common.GameData.Hero();
            data.rom = rom;
            data.level = rom.level;
            if (rom.level > 1)
                data.exp = expTable[rom.levelGrowthRate, rom.level - 2];
            data.maxHitpoint = data.calcMaxHitPoint(rom);
            data.maxMagicpoint = data.calcMaxMagicPoint(rom);
            data.power = data.calcStatus(rom.power, rom.powerGrowth, rom.powerGrowthRate);
            data.vitality = data.calcStatus(rom.vitality, rom.vitalityGrowth, rom.vitalityGrowthRate);
            data.magic = data.calcStatus(rom.magic, rom.magicGrowth, rom.magicGrowthRate);
            data.speed = data.calcStatus(rom.speed, rom.speedGrowth, rom.speedGrowthRate);

            // スキルを反映
            data.skills.AddRange(rom.skillLearnLevelsList.Where(skill => skill.level <= data.level).Select(skill => catalog.getItemFromGuid(skill.skill) as Common.Rom.Skill));
            data.skills.RemoveAll(x => x == null);

            // 装備を反映
            if (rom.equipments.weapon != Guid.Empty)
                data.equipments[0] = catalog.getItemFromGuid(rom.equipments.weapon) as Common.Rom.Item;
            if (rom.equipments.shield != Guid.Empty)
                data.equipments[1] = catalog.getItemFromGuid(rom.equipments.shield) as Common.Rom.Item;
            if (rom.equipments.head != Guid.Empty)
                data.equipments[2] = catalog.getItemFromGuid(rom.equipments.head) as Common.Rom.Item;
            if (rom.equipments.body != Guid.Empty)
                data.equipments[3] = catalog.getItemFromGuid(rom.equipments.body) as Common.Rom.Item;
            if (rom.equipments.accessory[0] != Guid.Empty)
                data.equipments[4] = catalog.getItemFromGuid(rom.equipments.accessory[0]) as Common.Rom.Item;
            if (rom.equipments.accessory[1] != Guid.Empty)
                data.equipments[5] = catalog.getItemFromGuid(rom.equipments.accessory[1]) as Common.Rom.Item;
            data.refreshEquipmentEffect();

            // HP・MPは装備で上がっている可能性があるので、最後に反映する
            data.hitpoint = data.maxHitpoint;
            data.magicpoint = data.maxMagicpoint;

            data.consistency();

            return data;
        }

        public void RemoveMember(Guid guid)
        {
            // パーティから見つかったら控えに移す
            foreach (var hero in members)
            {
                if (hero.rom.guId == guid)
                {
                    members.Remove(hero);
                    others.Add(hero);
                    return;
                }
            }
        }

        public int GetMoney()
        {
            return money;
        }

        public void SetMoney(int value)
        {
            if (value < 0)
                value = 0;
            else if (value > MAX_MONEY)
                value = MAX_MONEY;

            money = value;
        }

        public void AddMoney(int value)
        {
            SetMoney(money + value);
        }

        public int GetItemNum(Guid guid, bool includeEquipped = false)
        {
            int result = 0;
            foreach (var item in items)
            {
                if (item.item.guId == guid)
                    result = item.num;
            }
            if (includeEquipped)
            {
                foreach (var member in members)
                {
                    foreach (var eq in member.equipments)
                    {
                        if (eq != null && eq.guId == guid)
                            result++;
                    }
                }
            }
            return result;
        }

        public void SetItemNum(Guid guid, int num)
        {
            var item = catalog.getItemFromGuid(guid) as Common.Rom.Item;
            if (item == null)
                return;

            if (num > item.maxNum)
                num = item.maxNum;

            // 持ってる場合はここで数値がセットされる
            foreach (var stack in items)
            {
                if (stack.item.guId == guid)
                {
                    if (num <= 0)
                        items.Remove(stack);
                    else
                    {
                        // 個数が増える場合は総取得数を更新する
                        if (stack.num < num)
                            AddItemToDict(guid, num - stack.num);

                        stack.num = num;
                    }
                    return;
                }
            }

            // 持ってない場合は AddItem する
            AddItem(guid, num);
        }

        public bool ExistMember(Guid guid)
        {
            foreach (var member in members)
            {
                if(member.rom.guId == guid)
                   return true;
            }
            return false;
        }

        public Hero GetMember(Guid guid, bool includeOthers = false)
        {
            foreach (var member in members)
            {
                if (member.rom.guId == guid)
                    return member;
            }

            if (includeOthers)
            {
                foreach (var member in others)
                {
                    if (member.rom.guId == guid)
                        return member;
                }
            }

            return null;
        }

        public void AddItem(Guid guid, int num)
        {
            var item = catalog.getItemFromGuid(guid) as Common.Rom.Item;
            if (item == null)
                return;

            if (num > item.maxNum)
                num = item.maxNum;

            foreach (var stack in items)
            {
                if (stack.item.guId == guid)
                {
                    if (stack.num + num > item.maxNum)
                        num = item.maxNum - stack.num;
                    stack.num += num;
                    if (num > 0)
                    {
                        AddItemToDict(guid, num);
                        itemDict[guid] += num;
                    }
                    if (stack.num <= 0)
                        items.Remove(stack);
                    return;
                }
            }

            if (num < 0)
                return;

            var newstack = new ItemStack();
            newstack.item = item;
            newstack.num = num;
            items.Add(newstack);

            AddItemToDict(guid, num);
        }

        private void AddItemToDict(Guid guid, int num)
        {
            if (!itemDict.ContainsKey(guid))
                itemDict.Add(guid, num);
            else
                itemDict[guid] += num;
        }

        void IGameDataItem.save(System.IO.BinaryWriter writer)
        {
            // パーティメンバーを保存
            writer.Write(members.Count);
            foreach (var hero in members)
            {
                GameDataManager.saveChunk(hero, writer);
            }

            // 控えメンバーを保存
            writer.Write(others.Count);
            foreach (var hero in others)
            {
                GameDataManager.saveChunk(hero, writer);
            }

            // アイテム袋を保存
            writer.Write(items.Count);
            foreach (var stack in items)
            {
                writer.Write(stack.item.guId.ToByteArray());
                writer.Write(stack.num);
            }

            // 所持金を保存
            writer.Write(money);

            // 手に入れたアイテム図鑑を保存
            writer.Write(itemDict.Count);
            foreach (var item in itemDict)
            {
                writer.Write(item.Key.ToByteArray());
                writer.Write(item.Value);
            }

            // 変更したグラフィックを保存
            writer.Write(changedHeroGraphic.Count);
            foreach (var entry in changedHeroGraphic)
            {
                writer.Write(entry.Key.ToByteArray());
                writer.Write(entry.Value.face.ToByteArray());
                writer.Write(entry.Value.model.ToByteArray());
            }

            // 変更した名前を保存
            writer.Write(changedHeroName.Count);
            foreach (var entry in changedHeroName)
            {
                writer.Write(entry.Key.ToByteArray());
                writer.Write(entry.Value);
            }

            // アイテム袋の最大値を保存
            writer.Write(inventoryMax);
        }

        void IGameDataItem.load(Catalog catalog, System.IO.BinaryReader reader)
        {
            // パーティメンバーを復帰
            members.Clear();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var hero = new Hero();
                GameDataManager.readChunk(catalog, hero, reader);
                if(hero.rom != null)
                    members.Add(hero);
            }

            // 控えメンバーを復帰
            others.Clear();
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var hero = new Hero();
                GameDataManager.readChunk(catalog, hero, reader);
                if (hero.rom != null)
                    others.Add(hero);
            }

            // アイテム袋を復帰
            items.Clear();
            itemDict.Clear();
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var stack = new ItemStack();
                stack.item = catalog.getItemFromGuid(Util.readGuid(reader)) as Common.Rom.Item;
                stack.num = reader.ReadInt32();
                if (stack.item != null)
                {
                    items.Add(stack);
                }
            }

            // 所持金を復帰
            money = reader.ReadInt32();

            // 今まで手に入れたアイテムを復元
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var guid = Util.readGuid(reader);
                var num = reader.ReadInt32();
                itemDict.Add(guid, num);
            }

            // 変更したグラフィックを反映
            changedHeroGraphic.Clear();
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var heroGuid = Util.readGuid(reader);
                var faceGuid = Util.readGuid(reader);
                var modelGuid = Util.readGuid(reader);
                var hero = catalog.getItemFromGuid(heroGuid) as Common.Rom.Hero;
                if (hero != null)
                {
                    AddChangedGraphic(hero.guId, faceGuid, modelGuid);
                }
            }

            // 変更した名前を反映
            changedHeroName.Clear();
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var heroGuid = Util.readGuid(reader);
                var name = reader.ReadString();
                setHeroName(heroGuid, name);
            }

            // アイテム袋の最大値を読み込み
            inventoryMax = reader.ReadInt32();
        }

        public bool isGameOver()
        {
            // 全員のHPが0か調べる
            foreach (var member in members)
            {
                if (member.hitpoint > 0)
                    return false;
            }
            return true;
        }

        public void AddChangedGraphic(Guid hero, Guid faceGuid, Guid modelGuid)
        {
            var entry = new ChangedGraphic();
            entry.face = faceGuid;
            entry.model = modelGuid;
            if (changedHeroGraphic.ContainsKey(hero))
            {
                changedHeroGraphic[hero] = entry;
            }
            else
            {
                changedHeroGraphic.Add(hero, entry);
            }
        }

        public Guid getMemberFace(int index)
        {
            var result = members[index].rom.face;
            if (changedHeroGraphic.ContainsKey(members[index].rom.guId))
                result = changedHeroGraphic[members[index].rom.guId].face;
            return result;
        }

        public Guid getMemberGraphic(int index)
        {
            var result = members[index].rom.graphic;
            if (changedHeroGraphic.ContainsKey(members[index].rom.guId))
                result = changedHeroGraphic[members[index].rom.guId].model;
            return result;
        }

        public Guid getMemberFace(Rom.Hero rom)
        {
            var result = rom.face;
            if (changedHeroGraphic.ContainsKey(rom.guId))
                result = changedHeroGraphic[rom.guId].face;
            return result;
        }

        public Guid getMemberGraphic(Rom.Hero rom)
        {
            var result = rom.graphic;
            if (changedHeroGraphic.ContainsKey(rom.guId))
                result = changedHeroGraphic[rom.guId].model;
            return result;
        }

        public int checkInventoryEmptyNum()
        {
            var max = inventoryMax;
            if (max < 0)
                max = catalog.getGameSettings().inventoryMax;
            return max - items.Count;
        }

        public bool isOKToConsumptionItem(Guid guid, int amount)
        {
            var item = catalog.getItemFromGuid(guid) as Common.Rom.Item;
            if (item == null)
                return true;

            return GetItemNum(guid) >= amount;
        }
    }
}
