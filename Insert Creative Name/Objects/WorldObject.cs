﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insert_Creative_Name.Objects
{
    internal abstract class WorldObject
    {
        protected internal uint Id { get; set; }
        protected internal string Name { get; set; }
        protected internal DateTime Creation { get; set; }

        internal WorldObject(uint id, string name)
        {
            Id = id;
            Name = name;
            Creation = DateTime.UtcNow;
        }
    }
}