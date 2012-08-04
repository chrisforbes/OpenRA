#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class DamagedByTerrainInfo : ITraitInfo
	{
		[WeaponReference] public readonly string Weapon = null;
		public readonly string[] Resources = { };

		public object Create(ActorInitializer init) { return new DamagedByTerrain(this); }
	}

	class DamagedByTerrain : ITick, ISync
	{
        DamagedByTerrainInfo info;
		[Sync] int weaponTicks;

        public DamagedByTerrain(DamagedByTerrainInfo info) { this.info = info; }

		public void Tick(Actor self)
		{
            if (--weaponTicks > 0) return;

			var rl = self.World.WorldActor.Trait<ResourceLayer>();
			var r = rl.GetResource(self.Location);
			if( r == null ) return;
			if( !info.Resources.Contains(r.info.Name) ) return;
            if (info.Weapon == null) return;

			var weapon = Rules.Weapons[info.Weapon.ToLowerInvariant()];

			self.InflictDamage( self.World.WorldActor, weapon.Warheads[ 0 ].Damage, weapon.Warheads[ 0 ] );
            weaponTicks = weapon.ROF;
		}
	}
}
