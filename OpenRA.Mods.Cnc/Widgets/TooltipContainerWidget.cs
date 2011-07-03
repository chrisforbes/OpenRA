#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Widgets;
using System;

namespace OpenRA.Mods.Cnc.Widgets
{
	class TooltipContainerWidget : Widget
	{
		public int2 CursorOffset = new int2(20, 0);

		Widget tooltip;
		public string TooltipId { get; private set; }
		public void SetTooltip(Widget tooltip, string id)
		{
			this.tooltip = tooltip;
			RemoveChildren();
			AddChild(tooltip);
			TooltipId = id;
		}

		public void RemoveTooltip()
		{
			RemoveChildren();
			TooltipId = null;
		}

		public override int2 ChildOrigin
		{
			get
			{
				var pos = Viewport.LastMousePos + CursorOffset;
				if (tooltip != null)
				{
					if (pos.X + tooltip.Bounds.Right > Game.viewport.Width)
						pos.X -= 2*CursorOffset.X + tooltip.Bounds.Right;
				}

				return pos;
			}
		}
        public override string GetCursor(int2 pos) { return null; }
        public override Widget Clone() { throw new NotImplementedException(); }
	}
}
