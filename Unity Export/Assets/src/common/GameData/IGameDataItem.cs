using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace Yukar.Common.GameData
{
    interface IGameDataItem
    {
        void save(BinaryWriter writer);
        void load(Catalog catalog, BinaryReader reader);
    }
}
