#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Widgets;
using System.Collections.Generic;

namespace OpenRA.Mods.Golf
{
	public class OpenWidgetAtGameStartInfo : ITraitInfo
	{
		public readonly string Widget = "INGAME_ROOT";
		public readonly string ObserverWidget = null;

		public object Create(ActorInitializer init) { return new OpenWidgetAtGameStart(this); }
	}

	public class OpenWidgetAtGameStart: IWorldLoaded
	{
		readonly OpenWidgetAtGameStartInfo Info;
		public OpenWidgetAtGameStart(OpenWidgetAtGameStartInfo Info)
		{
			this.Info = Info;
		}

		public void WorldLoaded(World world)
		{
			Ui.ResetAll();
			Game.OpenWindow(world, Info.Widget);
		}
	}
}