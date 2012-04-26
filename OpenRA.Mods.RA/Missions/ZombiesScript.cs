#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Air;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ZombiesScriptInfo : TraitInfo<ZombiesScript> {}

	public class ZombiesScript : IWorldLoaded, ITick
	{
		Dictionary<string, Actor> actors;

		public void Tick(Actor self)
		{
			// do things
		}

		public void WorldLoaded(World w)
		{
			// do other things
			actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			
			
		}
	}
}

