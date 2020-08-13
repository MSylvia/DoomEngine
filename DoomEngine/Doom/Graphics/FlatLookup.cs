﻿//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//

namespace DoomEngine.Doom.Graphics
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Runtime.ExceptionServices;
	using Wad;

	public sealed class FlatLookup : IReadOnlyList<Flat>
	{
		private Flat[] flats;

		private Dictionary<string, Flat> nameToFlat;
		private Dictionary<string, int> nameToNumber;

		private int skyFlatNumber;
		private Flat skyFlat;

		public FlatLookup(Wad wad)
			: this(wad, false)
		{
		}

		public FlatLookup(Wad wad, bool useDummy)
		{
			if (!useDummy)
			{
				var fStartCount = FlatLookup.CountLump(wad, "F_START");
				var fEndCount = FlatLookup.CountLump(wad, "F_END");
				var ffStartCount = FlatLookup.CountLump(wad, "FF_START");
				var ffEndCount = FlatLookup.CountLump(wad, "FF_END");

				// Usual case.
				var standard = fStartCount == 1 && fEndCount == 1 && ffStartCount == 0 && ffEndCount == 0;

				// A trick to add custom flats is used.
				// https://www.doomworld.com/tutorials/fx2.php
				var customFlatTrick = fStartCount == 1 && fEndCount >= 2;

				// Need deutex to add flats.
				var deutexMerge = fStartCount + ffStartCount >= 2 && fEndCount + ffEndCount >= 2;

				if (standard || customFlatTrick)
				{
					this.InitStandard(wad);
				}
				else if (deutexMerge)
				{
					this.InitDeuTexMerge(wad);
				}
				else
				{
					throw new Exception("Faild to read flats.");
				}
			}
			else
			{
				this.InitDummy(wad);
			}
		}

		private void InitStandard(Wad wad)
		{
			try
			{
				Console.Write("Load flats: ");

				var firstFlat = wad.GetLumpNumber("F_START") + 1;
				var lastFlat = wad.GetLumpNumber("F_END") - 1;
				var count = lastFlat - firstFlat + 1;

				this.flats = new Flat[count];

				this.nameToFlat = new Dictionary<string, Flat>();
				this.nameToNumber = new Dictionary<string, int>();

				for (var lump = firstFlat; lump <= lastFlat; lump++)
				{
					if (wad.GetLumpSize(lump) != 4096)
					{
						continue;
					}

					var number = lump - firstFlat;
					var name = wad.LumpInfos[lump].Name;
					var flat = new Flat(name, wad.ReadLump(lump));

					this.flats[number] = flat;
					this.nameToFlat[name] = flat;
					this.nameToNumber[name] = number;
				}

				this.skyFlatNumber = this.nameToNumber["F_SKY1"];
				this.skyFlat = this.nameToFlat["F_SKY1"];

				Console.WriteLine("OK (" + this.nameToFlat.Count + " flats)");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				ExceptionDispatchInfo.Throw(e);
			}
		}

		private void InitDeuTexMerge(Wad wad)
		{
			try
			{
				Console.Write("Load flats: ");

				var allFlats = new List<int>();
				var flatZone = false;

				for (var lump = 0; lump < wad.LumpInfos.Count; lump++)
				{
					var name = wad.LumpInfos[lump].Name;

					if (flatZone)
					{
						if (name == "F_END" || name == "FF_END")
						{
							flatZone = false;
						}
						else
						{
							allFlats.Add(lump);
						}
					}
					else
					{
						if (name == "F_START" || name == "FF_START")
						{
							flatZone = true;
						}
					}
				}

				allFlats.Reverse();

				var dupCheck = new HashSet<string>();
				var distinctFlats = new List<int>();

				foreach (var lump in allFlats)
				{
					if (!dupCheck.Contains(wad.LumpInfos[lump].Name))
					{
						distinctFlats.Add(lump);
						dupCheck.Add(wad.LumpInfos[lump].Name);
					}
				}

				distinctFlats.Reverse();

				this.flats = new Flat[distinctFlats.Count];

				this.nameToFlat = new Dictionary<string, Flat>();
				this.nameToNumber = new Dictionary<string, int>();

				for (var number = 0; number < this.flats.Length; number++)
				{
					var lump = distinctFlats[number];

					if (wad.GetLumpSize(lump) != 4096)
					{
						continue;
					}

					var name = wad.LumpInfos[lump].Name;
					var flat = new Flat(name, wad.ReadLump(lump));

					this.flats[number] = flat;
					this.nameToFlat[name] = flat;
					this.nameToNumber[name] = number;
				}

				this.skyFlatNumber = this.nameToNumber["F_SKY1"];
				this.skyFlat = this.nameToFlat["F_SKY1"];

				Console.WriteLine("OK (" + this.nameToFlat.Count + " flats)");
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed");
				ExceptionDispatchInfo.Throw(e);
			}
		}

		private void InitDummy(Wad wad)
		{
			var firstFlat = wad.GetLumpNumber("F_START") + 1;
			var lastFlat = wad.GetLumpNumber("F_END") - 1;
			var count = lastFlat - firstFlat + 1;

			this.flats = new Flat[count];

			this.nameToFlat = new Dictionary<string, Flat>();
			this.nameToNumber = new Dictionary<string, int>();

			for (var lump = firstFlat; lump <= lastFlat; lump++)
			{
				if (wad.GetLumpSize(lump) != 4096)
				{
					continue;
				}

				var number = lump - firstFlat;
				var name = wad.LumpInfos[lump].Name;
				var flat = name != "F_SKY1" ? Dummy.GetFlat() : Dummy.GetSkyFlat();

				this.flats[number] = flat;
				this.nameToFlat[name] = flat;
				this.nameToNumber[name] = number;
			}

			this.skyFlatNumber = this.nameToNumber["F_SKY1"];
			this.skyFlat = this.nameToFlat["F_SKY1"];
		}

		public int GetNumber(string name)
		{
			if (this.nameToNumber.ContainsKey(name))
			{
				return this.nameToNumber[name];
			}
			else
			{
				return -1;
			}
		}

		public IEnumerator<Flat> GetEnumerator()
		{
			return ((IEnumerable<Flat>) this.flats).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.flats.GetEnumerator();
		}

		private static int CountLump(Wad wad, string name)
		{
			var count = 0;

			foreach (var lump in wad.LumpInfos)
			{
				if (lump.Name == name)
				{
					count++;
				}
			}

			return count;
		}

		public int Count => this.flats.Length;
		public Flat this[int num] => this.flats[num];
		public Flat this[string name] => this.nameToFlat[name];
		public int SkyFlatNumber => this.skyFlatNumber;
		public Flat SkyFlat => this.skyFlat;
	}
}