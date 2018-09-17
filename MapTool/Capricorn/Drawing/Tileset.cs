﻿// Decompiled with JetBrains decompiler
// Type: Capricorn.Drawing.Tileset
// Assembly: Accolade, Version=1.4.1.0, Culture=neutral, PublicKeyToken=null
// MVID: A987DE0D-CB54-451E-92F3-D381FD0B091A
// Assembly location: D:\Dropbox\Ditto (1)\Other Bots and TOols\Kyle's Known Bots\Accolade\Accolade.exe

using Capricorn.IO;
using System.Collections.Generic;
using System.IO;

namespace Capricorn.Drawing
{
    public class Tileset
    {
        private List<byte[]> tiles = new List<byte[]>();
        public const int TileWidth = 56;
        public const int TileHeight = 27;
        public const int TileSize = 1512;
        private string name;
        private string filename;
        private int tileCount;

        public byte[] this[int index]
        {
            get => tiles[index];
            set => tiles[index] = value;
        }

        public string FileName => filename;

        public string Name
        {
            get => name;
            set => name = value;
        }

        public byte[][] Tiles => tiles.ToArray();

        public int TileCount => tileCount;

        public static Tileset FromFile(string file)
        {
            var tileset = Tileset.LoadTiles(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read));
            tileset.name = Path.GetFileNameWithoutExtension(file).ToUpper();
            tileset.filename = file;
            return tileset;
        }

        public static Tileset FromRawData(byte[] data)
        {
            var tileset = Tileset.LoadTiles(new MemoryStream(data));
            tileset.name = "Unknown Tileset";
            return tileset;
        }

        public static Tileset FromArchive(string file, DATArchive archive)
        {
            if (!archive.Contains(file))
                return null;
            var tileset = Tileset.LoadTiles(new MemoryStream(archive.ExtractFile(file)));
            tileset.name = Path.GetFileNameWithoutExtension(file).ToUpper();
            tileset.filename = file;
            return tileset;
        }

        public static Tileset FromArchive(string file, bool ignoreCase, DATArchive archive)
        {
            if (!archive.Contains(file, ignoreCase))
                return null;
            var tileset = Tileset.LoadTiles(new MemoryStream(archive.ExtractFile(file, ignoreCase)));
            tileset.name = Path.GetFileNameWithoutExtension(file).ToUpper();
            tileset.filename = file;
            return tileset;
        }

        private static Tileset LoadTiles(Stream stream)
        {
            stream.Seek(0L, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(stream);
            var tileset = new Tileset
            {
                tileCount = (int)(binaryReader.BaseStream.Length / 1512L)
            };
            for (int index = 0; index < tileset.tileCount; ++index)
            {
                byte[] numArray = binaryReader.ReadBytes(1512);
                tileset.tiles.Add(numArray);
            }
            binaryReader.Close();
            return tileset;
        }

        public override string ToString() => "{Name = " + name + ", Tiles = " + tiles.Count.ToString() + "}";
    }
}