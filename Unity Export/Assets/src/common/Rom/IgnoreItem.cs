using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Yukar.Common.Rom
{
    public class IgnoreItem : RomItem
    {
        public IgnoreItem()
        {

        }

        public IgnoreItem(string name) : base()
        {
            this.name = name;
        }

        public override void save(BinaryWriter writer)
        {
            base.save(writer);
        }

        public override void load(BinaryReader reader)
        {
            base.load(reader);
        }
    }
}
