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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Support;
using OpenRA.Widgets;
using OpenRA.GameRules;
using System.Net;

namespace OpenRA.Mods.Golf
{
	public class GolfLoadScreen : ILoadScreen
	{
		Dictionary<string, string> Info;
		static string[] Comments = new[] {	"Filling Crates...", "Charging Capacitors...", "Reticulating Splines...",
												"Planting Trees...", "Building Bridges...", "Aging Empires...",
												"Compiling EVA...", "Constructing Pylons...", "Activating Skynet...",
												"Splitting Atoms..."
		};

		Stopwatch lastLoadScreen = new Stopwatch();
		Rectangle StripeRect;
		Sprite Stripe, Logo;
		float2 LogoPos;

		Renderer r;
		public void Init(Dictionary<string, string> info)
		{
			Info = info;
			// Avoid standard loading mechanisms so we
			// can display loadscreen as early as possible
			r = Game.Renderer;
			if (r == null) return;

			var s = new Sheet("mods/golf/uibits/loadscreen.png");
			Logo = new Sprite(s, new Rectangle(0,0,256,256), TextureChannel.Alpha);
			Stripe = new Sprite(s, new Rectangle(256,0,256,256), TextureChannel.Alpha);
			StripeRect = new Rectangle(0, Renderer.Resolution.Height/2 - 128, Renderer.Resolution.Width, 256);
			LogoPos =  new float2(Renderer.Resolution.Width/2 - 128, Renderer.Resolution.Height/2 - 128);
		}

		public void Display()
		{
			if (r == null)
				return;

			// Update text at most every 0.5 seconds
			if (lastLoadScreen.ElapsedTime() < 0.5)
				return;

			lastLoadScreen.Reset();
			var text = Comments.Random(Game.CosmeticRandom);
			var textSize = r.Fonts["Bold"].Measure(text);

			r.BeginFrame(float2.Zero, 1f);
			WidgetUtils.FillRectWithSprite(StripeRect, Stripe);
			r.RgbaSpriteRenderer.DrawSprite(Logo, LogoPos);
			r.Fonts["Bold"].DrawText(text, new float2(Renderer.Resolution.Width - textSize.X - 20, Renderer.Resolution.Height - textSize.Y - 20), Color.White);
			r.EndFrame( new NullInputHandler() );
		}

		public void StartGame()
		{
			TestAndContinue();
			Game.JoinExternalGame();
		}

		void TestAndContinue()
		{
			Ui.ResetAll();
			if (!FileSystem.Exists(Info["TestFile"]))
			{
				var args = new WidgetArgs()
				{
					{ "continueLoading", () => TestAndContinue() },
					{ "installData", Info }
				};
				Ui.OpenWindow(Info["InstallerMenuWidget"], args);
			}
			else
			{
				//Game.LoadShellMap();
				//Ui.ResetAll();
				//Ui.OpenWindow("MAINMENU_BG");
                
                //Game.CreateLocalServer("a-path-beyond");
                Game.Settings.Server.Name = "Test";
                Game.Settings.Server.ListenPort = 1234;
                Game.Settings.Server.ExternalPort = 1234;
                Game.Settings.Server.AdvertiseOnline = false;
                Game.Settings.Server.AllowUPnP = false;
                //string path = "mods\\golf\\maps\\a-path-beyond.oramap";
                //Game.Settings.Server.Map = "4734332688a62194b173e812605e323df44c5b58";
                Game.Settings.Save();

                // Take a copy so that subsequent changes don't affect the server
                var settings = new ServerSettings(Game.Settings.Server);
                Game.CreateServer(settings);
                Game.JoinServer(IPAddress.Loopback.ToString(), Game.Settings.Server.ListenPort);
			}
		}
	}
}

