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
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ZombiesScriptInfo : TraitInfo<ZombiesScript>, Requires<SpawnMapActorsInfo> {}

	public class ZombiesScript : IWorldLoaded, ITick
	{
		Dictionary<string, Actor> actors;
		string[] InitialUnits = { "e1", "e1", "e1" };
		List<Actor> SpawnedDudes = new List<Actor>();
		
		string[] briefing = {
			"We've lost contact with the nearby research lab.",
			"Their last transmission mentioned some kind of infection transforming our people.",
			"Secure the lab, and extract anyone who is still alive."
		};
		
		void PlayBriefing(Actor self, int start, int delay)
		{
			for( var i = 0; i < briefing.Length; i++ )
				if (self.World.FrameNumber == start + i * delay)
					Game.AddChatLine( Color.LimeGreen, "<Command>", briefing[i] );
		}

		public void Tick(Actor self)
		{
			// do things
			var spawnHeli = actors["tran"];
			if (self.World.FrameNumber == 1)
			{
				spawnHeli.QueueActivity( new HeliFly( actors["tran_drop"].CenterLocation ) );
				spawnHeli.QueueActivity( new Turn(0) );
				spawnHeli.QueueActivity( new HeliLand(true) );
				spawnHeli.QueueActivity( new UnloadCargo() );
			}

			if (self.World.FrameNumber == 200)
				spawnHeli.QueueActivity( new HeliFly( actors["tran_exit"].CenterLocation ) );

			if (self.World.FrameNumber == 300)
				foreach( var a in SpawnedDudes )
					a.QueueActivity( new Move.Move( a.Location + new int2(0,1) ) );
			
			PlayBriefing( self, 40, 20 );
		}

		public void WorldLoaded(World w)
		{
			// do other things
			actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			var spawnHeli = actors["tran"];
			
			// todo: split infantry setup for 2p
			var p = w.LocalPlayer;		/* FIXME, this will desync in multi */
			
			foreach( var u in InitialUnits )
			{	// load up the heli with the player's starting units.
				var actor = w.CreateActor(false, u,
					new TypeDictionary { new OwnerInit(p) });
				spawnHeli.Trait<Cargo>().Load(spawnHeli, actor);
				SpawnedDudes.Add(actor);
			}
		}
	}
}

