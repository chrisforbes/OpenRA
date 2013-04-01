#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Missions
{
	class Allies07ScriptInfo : TraitInfo<Allies07Script>, Requires<SpawnMapActorsInfo> { }

	class Allies07Script : IHasObjectives, IWorldLoaded, ITick
	{
		public event Action<bool> OnObjectivesUpdated = notify => { };

		public IEnumerable<Objective> Objectives { get { return objectives.Values; } }

		Dictionary<int, Objective> objectives = new Dictionary<int, Objective>
		{
			{ InfiltrateID, new Objective(ObjectiveType.Primary, "", ObjectiveStatus.InProgress) },
		};

		const int InfiltrateID = 0;
		const string Infiltrate = "Sneaky sneaky";

		World world;
		string difficulty;
		Actor startPoint;
		Actor hero;
		Player allies1;

		public void Tick(Actor self)
		{
			if (world.FrameNumber == 1)
				InsertStartingUnits();

			if (hero.IsDead())
			{
				// bad stuff
			}
		}

		void InsertStartingUnits()
		{
			hero = world.CreateActor("e7", new TypeDictionary {
				new OwnerInit(allies1),
				new LocationInit(startPoint.Location)
			});
		}

		public void WorldLoaded(World w)
		{
			world = w;
			allies1 = w.Players.Single(p => p.InternalName == "Allies1");
			allies1.PlayerActor.Trait<PlayerResources>().Cash = 0;
			difficulty = w.LobbyInfo.GlobalSettings.Difficulty;
			Game.Debug("{0} difficulty selected".F(difficulty));
			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;

			startPoint = actors["StartPoint"];

			Sound.PlayLooped("rain.aud");
			Game.MoveViewport(startPoint.Location.ToFloat2());
		}
	}

	class NightPaletteEffectInfo : TraitInfo<NightPaletteEffect> { }

	class NightPaletteEffect : IPaletteModifier, ITick
	{
		static readonly string[] ExcludePalettes = { "cursor", "chrome", "colorpicker", "fog", "shroud" };

		bool lightning = false;

		public void Tick(Actor self)
		{
			lightning = self.World.SharedRandom.NextFloat() > 0.99f;
			if (lightning)
				Sound.Play("thunder.aud");		// todo: position randomly?
		}

		public void AdjustPalette(Dictionary<string, Palette> palettes)
		{
			foreach (var pal in palettes)
			{
				if (ExcludePalettes.Contains(pal.Key))
					continue;

				for (var x = 0; x < 256; x++)
				{
					var from = pal.Value.GetColor(x);
					var lum = from.GetBrightness();

					if (lightning)
						pal.Value.SetColor(x, Color.White);
					else
					{
						var to = Color.FromArgb(from.A, (int)(lum * 30), (int)(lum * 30), (int)(lum * 90));
						pal.Value.SetColor(x, Exts.ColorLerp(0.8f, from, to));
					}
				}
			}
		}
	}
}
