#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Golf.Server
{
	public class MasterServerPinger : ServerTrait, ITick, INotifySyncLobbyInfo, IStartGame
	{
        int firstTick = 0;
        public int TickTimeout { get { return 10000 * 60 * 3; } }
		public void Tick(S server) {
            if (this.firstTick == 0)
            {
                string path = server.ModData.AvailableMaps.First().Value.Path;
                server.Map = new Map(server.ModData.AvailableMaps.First().Value.Path);
                server.StartGame();
                this.firstTick = 1;
            }
        }
        public void LobbyInfoSynced(S server) { }
        public void GameStarted(S server) {
            Game.AddChatLine(Color.Red, "Info", "Game started");
        }
	}
}
