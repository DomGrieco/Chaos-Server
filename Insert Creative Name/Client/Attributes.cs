﻿using System;

namespace Insert_Creative_Name
{
    [Serializable]
    internal sealed class Attributes
    {
        //baseValues
        internal byte BaseStr;
        internal byte BaseInt;
        internal byte BaseWis;
        internal byte BaseCon;
        internal byte BaseDex;
        internal uint BaseHP;
        internal uint BaseMP;

        //Primary
        internal byte Level;
        internal byte Ability;
        internal uint MaximumHP;
        internal uint MaximumMP;
        internal byte CurrentStr;
        internal byte CurrentInt;
        internal byte CurrentWis;
        internal byte CurrentCon;
        internal byte CurrentDex;
        internal bool HasUnspentPoints;
        internal byte UnspentPoints;
        internal short MaximumWeight;
        internal short CurrentWeight;

        //Vitality
        internal uint CurrentHP;
        internal uint CurrentMP;

        //Experience
        internal uint Experience;
        internal uint ToNextLevel;
        internal uint AbilityExp;
        internal uint ToNextAbility;
        internal uint GamePoints;
        internal uint Gold;

        //Secondary
        internal byte Blind;
        internal MailFlag MailFlags;
        internal Element OffenseElement;
        internal Element DefenseElement;
        internal byte MagicResistance;
        internal sbyte ArmorClass;
        internal byte Dmg;
        internal byte Hit;
    }
}
