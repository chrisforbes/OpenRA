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
using System.Linq;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ObserveAsLogic
	{
		[ObjectCreator.UseCtor]
		public ObserveAsLogic(World world)
		{
			var r = Ui.Root;
			var gameRoot = r.GetWidget("OBSERVER_ROOT") ?? r.GetWidget("INGAME_ROOT");
			var selector = gameRoot.GetWidget<DropDownButtonWidget>("OBSERVEAS_DROPDOWN");
			selector.OnMouseDown = _ => ShowWindowModeDropdown(selector, world);
			selector.GetText = () => world.RenderedPlayer != null
				? world.RenderedPlayer.PlayerName : "[Global View]";
		}

		public static bool ShowWindowModeDropdown(DropDownButtonWidget selector, World world)
		{
			var options = world.Players.Where(a => !a.NonCombatant).ToList();
			options.Insert(0, null);

			Func<Player, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate, 
					() => world.RenderedPlayer == o,
					() => { world.RenderedPlayer = o; world.RenderedShroud.SetDirty(); }
				);
				item.GetWidget<LabelWidget>("LABEL").GetText = () => o != null ? o.PlayerName : "[Global View]";
				return item;
			};

			selector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options, setupItem);
			return true;
		}
	}
}