using System;
using System.Collections.Generic;
using System.Linq;
namespace OpenRA
{
	public static class HeadlessConfig
	{
		class MenuItem
		{
			public string Desc;
			public Action Action;
			public MenuItem(string desc, Action act)
			{
				Desc = desc;
				Action = act;
			}
		}
		
		
		public static void DoHeadlessConfig()
		{
			if(Game.Settings.Server.Map==null)
				Game.Settings.Server.Map=Game.modData.AvailableMaps.Values
					.Where(m => m.Selectable).First().Uid;
			bool exit=false;
			while(!exit)
			{
				var items = new[]
				{
					new MenuItem
					(
						"Start now",
						()=>
						{
							exit=true;
						}
					),
					new MenuItem
					(
						"Change name [{0}]".F(Game.Settings.Server.Name),
						()=>
						{
							Console.Write("Enter name: ");
							Game.Settings.Server.Name=Console.ReadLine();
						}
					),
					new MenuItem
					(
						"Change port [{0}]".F(Game.Settings.Server.ListenPort),
						()=>
						{
							Game.Settings.Server.ListenPort = ReadPort();
						}
					),
					new MenuItem
					(
						"Change external port [{0}]".F(Game.Settings.Server.ExternalPort),
						()=>
						{
							Game.Settings.Server.ExternalPort = ReadPort();
						}
					),
					new MenuItem
					(
						"Advertise online [{0}]".F(Game.Settings.Server.AdvertiseOnline.ToString(
							System.Globalization.CultureInfo.InvariantCulture)),
						()=>
						{
							Game.Settings.Server.AdvertiseOnline =! Game.Settings.Server.AdvertiseOnline;
						}
					),
					new MenuItem
					(
						"ChangeMap [{0}]".F(Game.modData.AvailableMaps[Game.Settings.Server.Map].Title),
						()=>
						{
							Game.Settings.Server.Map = SelectMap();
						}
					)
					//
				};
				
				Console.Clear();
				items[Choose(from i in items select i.Desc)-1].Action();
				Game.Settings.Save();
			}
			Console.WriteLine("Starting game...");
		}
		
		static string SelectMap()
		{
			int page=0;
			var maps = (from m in Game.modData.AvailableMaps.Values where m.Selectable select m).ToList();
			while(true)
			{
				Console.Clear();
				int offset=page*8;
				List<string> menu=new List<string>();
				menu.Add((page==0)?"Cancel":"Previous page");
				for (int c=0; ((c<8)&&(maps.Count>offset+c));c++)
				{
					menu.Add(maps[offset+c].Title);
				}
				if (offset+8<=maps.Count)
					menu.Add("Next page");
				int rv=Choose(menu, 0);
				if(rv==0)
				{
					if(page==0)
						return Game.Settings.Server.Map;
					else 
						page--;
				}
				else if(rv==9)
					page++;
				else
					return maps[offset+rv-1].Uid;
				
			}
			
		}
		
		static int ReadPort()
		{
			int rv=0;
			while((rv<=0)||(rv>65535))
				rv=ReadInt("Enter port: ");
			return rv;
		}
		
		static int ReadInt (string promt)
		{
			while(true)
			{
				Console.Write(promt);
				int rv;
				if(!int.TryParse(Console.ReadLine(), out rv))
					continue;
				return rv;
			}
		}
		
		static int Choose(IEnumerable<string> variants, int baseIndex=1)
		{
			int last=baseIndex;
			foreach(var s in variants)
			{
				Console.WriteLine("{0}. {1}", last, s);
				last++;
			}
			while(true)
			{
				var key=Console.ReadKey(true);
				if((key.KeyChar<'0')||(key.KeyChar>'9'))
					continue;
				int k = (key.KeyChar-'0');
				if(k<baseIndex)
					continue;
				if(k>=last)
					continue;
				return k;
			}
		}
		
		
		
	}
}

