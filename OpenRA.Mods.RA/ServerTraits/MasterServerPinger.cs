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
using System.Net;
using OpenRA.Server;
using S = OpenRA.Server.Server;
using System.Linq;

namespace OpenRA.Mods.RA.Server
{
	public class MasterServerPinger : ServerTrait, ITick, INotifySyncLobbyInfo, IStartGame
	{
		const int MasterPingInterval = 60 * 3;	// 3 minutes. server has a 5 minute TTL for games, so give ourselves a bit
												// of leeway.
		public int TickTimeout { get { return MasterPingInterval * 10000; } }
		public void Tick(S server)
		{
			if (Environment.TickCount - lastPing > MasterPingInterval * 1000)
				PingMasterServer(server);
			else
				lock (masterServerMessages)
					while (masterServerMessages.Count > 0)
						server.SendChat(null, masterServerMessages.Dequeue());

		}


		public void LobbyInfoSynced(S server) { PingMasterServer(server); }
		public void GameStarted(S server) { PingMasterServer(server); }

		int lastPing = 0;
		bool isInitialPing = true;

		volatile bool isBusy;
		Queue<string> masterServerMessages = new Queue<string>();
		public void PingMasterServer(S server)
		{
			if (isBusy || !server.Settings.AdvertiseOnline) return;

			lastPing = Environment.TickCount;
			isBusy = true;

			Action a = () =>
				{
					try
					{
						string playerStr = "";
						var cs = from cli in server.lobbyInfo.Clients select cli;
						int i = 0;
						foreach(var c in cs)
						{
							playerStr += "&playerName"+i.ToString()+"="+c.Name;
							playerStr += "&playerFaction"+i.ToString()+"="+c.Country;
							playerStr += "&playerTeam"+i.ToString()+"="+c.Team.ToString();
							i++;
						}
						var url = "ping.php?port={0}&name={1}&state={2}&players={3}&mods={4}&map={5}&maxplayers={6}&title={7}&description={8}&type={9}&width={10}&height={11}&tileset={12}&author={13}";
						if (isInitialPing) url += "&new=1";

						using (var wc = new WebClient())
						{
							wc.Proxy = null;
							 wc.DownloadData(
								server.Settings.MasterServer + url.F(
								server.Settings.ExternalPort, Uri.EscapeUriString(server.Settings.Name),
								server.GameStarted ? 2 : 1,	// todo: post-game states, etc.
								server.lobbyInfo.Clients.Count,
								string.Join(",", Game.CurrentMods.Select(f => "{0}@{1}".F(f.Key, f.Value.Version)).ToArray()),
								server.lobbyInfo.GlobalSettings.Map,
								server.Map.PlayerCount,
								server.Map.Title,
								server.Map.Description,
								server.Map.Type,
								server.Map.MapSize.X,
								server.Map.MapSize.Y,
								server.Map.Tileset,
								server.Map.Author) + playerStr);

							if (isInitialPing)
							{
								isInitialPing = false;
								lock (masterServerMessages)
									masterServerMessages.Enqueue("Master server communication established.");
							}
						}
					}
					catch(Exception ex)
					{
						Log.Write("server", ex.ToString());
						lock( masterServerMessages )
							masterServerMessages.Enqueue( "Master server communication failed." );
					}

					isBusy = false;
				};

			a.BeginInvoke(null, null);
		}
	}
}
