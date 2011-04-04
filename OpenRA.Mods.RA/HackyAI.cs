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
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;
using XRandom = OpenRA.Thirdparty.Random;


//TODO:
// [y] never give harvesters orders
// maybe move rally points when a rally point gets blocked (by units or buildings)
// Don't send attack forces to your own spawn point
// effectively clear the area around the production buildings' spawn points.
// don't spam the build unit button, only queue one unit then wait for the backoff period.
//    just make the build unit action only occur once every second.
// build defense buildings

// later:
// don't build units randomly, have a method to it.
// explore spawn points methodically
// once you find a player, attack the player instead of spawn points.

namespace OpenRA.Mods.RA
{
	class HackyAIInfo : IBotInfo, ITraitInfo
	{
		[FieldLoader.Load]
		public readonly string Name = "Unnamed Bot";

		[FieldLoader.Load]
		public readonly int SquadSize = 8;

		string IBotInfo.Name { get { return this.Name; } }

		[FieldLoader.LoadUsing("LoadUnits")]
		public readonly Dictionary<string, float> UnitsToBuild = null;

		[FieldLoader.LoadUsing("LoadBuildings")]
		public readonly Dictionary<string, float> BuildingFractions = null;

		static object LoadUnits(MiniYaml y)
		{
			Dictionary<string, float> ret = new Dictionary<string, float>();
			foreach (var t in y.NodesDict["UnitsToBuild"].Nodes)
				ret.Add(t.Key, (float)FieldLoader.GetValue("units", typeof(float), t.Value.Value));
			return ret;
		}

		static object LoadBuildings(MiniYaml y)
		{
			Dictionary<string, float> ret = new Dictionary<string, float>();
			foreach (var t in y.NodesDict["BuildingFractions"].Nodes)
				ret.Add(t.Key, (float)FieldLoader.GetValue("units", typeof(float), t.Value.Value));
			return ret;
		}

		public object Create(ActorInitializer init) { return new HackyAI(this); }
	}

	/* a pile of hacks, which control a local player on the host. */

	class HackyAI : ITick, IBot
	{
		bool enabled;
		int ticks;
		Player p;
		PowerManager playerPower;

		int2 baseCenter;
		XRandom random = new XRandom(); //we do not use the synced random number generator.
		BaseBuilder[] builders;

		World world { get { return p.PlayerActor.World; } }
		IBotInfo IBot.Info { get { return this.Info; } }

		readonly HackyAIInfo Info;
		public HackyAI(HackyAIInfo Info)
		{
			this.Info = Info;
		}

		enum BuildState
		{
			ChooseItem,
			WaitForProduction,
			WaitForFeedback,
		}

		const int MaxBaseDistance = 15;

		public static void BotDebug(string s, params object[] args)
		{
			if (Game.Settings.Debug.BotDebug)
				Game.Debug(s, args);
		}

		/* called by the host's player creation code */
		public void Activate(Player p)
		{
			this.p = p;
			enabled = true;
			playerPower = p.PlayerActor.Trait<PowerManager>();
            builders = new BaseBuilder[] {
				new BaseBuilder( this, "Building", q => ChooseBuildingToBuild(q, true) ),
				new BaseBuilder( this, "Defense", q => ChooseBuildingToBuild(q, false) ) };
		}

		int GetPowerProvidedBy(ActorInfo building)
		{
			var bi = building.Traits.GetOrDefault<BuildingInfo>();
			if (bi == null) return 0;
			return bi.Power;
		}

		ActorInfo ChooseRandomUnitToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems();
			if (buildableThings.Count() == 0) return null;
			return buildableThings.ElementAtOrDefault(random.Next(buildableThings.Count()));
		}

		bool HasAdequatePower()
		{
			/* note: CNC `fact` provides a small amount of power. don't get jammed because of that. */
			return playerPower.PowerProvided > 50 &&
				playerPower.PowerProvided > playerPower.PowerDrained * 1.2;
		}

		ActorInfo ChooseBuildingToBuild(ProductionQueue queue, bool buildPower)
		{
			var buildableThings = queue.BuildableItems();

			if (!HasAdequatePower())	/* try to maintain 20% excess power */
			{
                if (!buildPower) return null;

				/* find the best thing we can build which produces power */
				return buildableThings.Where(a => GetPowerProvidedBy(a) > 0)
					.OrderByDescending(a => GetPowerProvidedBy(a)).FirstOrDefault();
			}

            var myBuildings = p.World
                .ActorsWithTrait<Building>()
                .Where( a => a.Actor.Owner == p )
                .Select(a => a.Actor.Info.Name).ToArray();

			foreach (var frac in Info.BuildingFractions)
				if (buildableThings.Any(b => b.Name == frac.Key))
					if (myBuildings.Count(a => a == frac.Key) < frac.Value * myBuildings.Length)
						return Rules.Info[frac.Key];

			return null;
		}

		IEnumerable<int2> Neighbours(int2 c)
		{
			/* only return 4-neighbors for now, maybe add 8s later. */
			yield return c;
			yield return new int2(c.X - 1, c.Y);
			yield return new int2(c.X + 1, c.Y);
			yield return new int2(c.X, c.Y - 1);
			yield return new int2(c.X, c.Y + 1);
		}

		IEnumerable<int2> ExpandFootprint(IEnumerable<int2> cells)
		{
			var result = new Dictionary<int2, bool>();
			foreach (var c in cells.SelectMany(c => Neighbours(c)))
				result[c] = true;
			return result.Keys;
		}

		bool NoBuildingsUnder(IEnumerable<int2> cells)
		{
			var bi = world.WorldActor.Trait<BuildingInfluence>();
			return cells.All(c => bi.GetBuildingAt(c) == null);
		}

		int2? ChooseBuildLocation(ProductionItem item)
		{
			var bi = Rules.Info[item.Item].Traits.Get<BuildingInfo>();

			for (var k = 0; k < MaxBaseDistance; k++)
				foreach (var t in world.FindTilesInCircle(baseCenter, k))
					if (world.CanPlaceBuilding(item.Item, bi, t, null))
						if (bi.IsCloseEnoughToBase(world, p, item.Item, t))
							if (NoBuildingsUnder(ExpandFootprint( 
								FootprintUtils.Tiles( item.Item, bi, t ))))
								return t;

			return null;		// i don't know where to put it.
		}

		const int feedbackTime = 30;		// ticks; = a bit over 1s. must be >= netlag.

		public void Tick(Actor self)
		{
			if (!enabled)
				return;

			ticks++;

			if (ticks == 10)
			{
				DeployMcv(self);
			}

			if (ticks % feedbackTime == 0)
			{
				//about once every second, perform unintelligent cleanup tasks.
				//e.g. ClearAreaAroundSpawnPoints();
				//e.g. start repairing damaged buildings.
				BuildRandom("Vehicle");
                BuildRandom("Vehicle");
				BuildRandom("Infantry");
				BuildRandom("Plane");
			}

			AssignRolesToIdleUnits(self);
			SetRallyPointsForNewProductionBuildings(self);

			foreach (var b in builders)
				b.Tick();
		}

		//hacks etc sigh mess.
		//A bunch of hardcoded lists to keep track of which units are doing what.
		List<Actor> unitsHangingAroundTheBase = new List<Actor>();
		List<Actor> attackForce = new List<Actor>();
        int2? attackTarget;

		//Units that the ai already knows about. Any unit not on this list needs to be given a role.
		List<Actor> activeUnits = new List<Actor>();

		bool IsHumanPlayer(Player p) { return !p.IsBot && !p.NonCombatant; }

		bool HasHumanPlayers()
		{
			return p.World.players.Any(a => !a.Value.IsBot && !a.Value.NonCombatant);
		}

		int2? ChooseEnemyTarget()
		{
			// Criteria for picking an enemy:
			// 1. not ourself.
			// 2. human.
			// 3. not dead.

            

			var possibleTargets = world.WorldActor.Trait<MPStartLocations>().Start
					.Where(kv => kv.Key != p && (!HasHumanPlayers() || IsHumanPlayer(kv.Key))
						&& p.WinState == WinState.Undefined)
					.Select(kv => kv.Value);

			return possibleTargets.Any() ? possibleTargets.Random(random) : (int2?)null;
		}

		void AssignRolesToIdleUnits(Actor self)
		{
			//HACK: trim these lists -- we really shouldn't be hanging onto all this state
			//when it's invalidated so easily, but that's Matthew/Alli's problem.
			activeUnits.RemoveAll(a => a.Destroyed);
			unitsHangingAroundTheBase.RemoveAll(a => a.Destroyed);
			attackForce.RemoveAll(a => a.Destroyed);
            
			// don't select harvesters.
			var newUnits = self.World.ActorsWithTrait<IMove>()
				.Where(a => a.Actor.Owner == p && a.Actor.Info != Rules.Info["harv"] && a.Actor.Info != Rules.Info["mcv"]
					&& !activeUnits.Contains(a.Actor))
                    .Select(a => a.Actor).ToArray();

			foreach (var a in newUnits)
			{
				BotDebug("AI: Found a newly built unit");
				unitsHangingAroundTheBase.Add(a);
				activeUnits.Add(a);
			}

			/* Create an attack force when we have enough units around our base. */
			// (don't bother leaving any behind for defense.)
			if (unitsHangingAroundTheBase.Count >= Info.SquadSize)
			{
				BotDebug("Launch an attack.");

                if (attackForce.Count == 0)
                {
                    attackTarget = ChooseEnemyTarget();
                    if (attackTarget == null)
                        return;
                }

				foreach (var a in unitsHangingAroundTheBase)
					if (TryToAttackMove(a, attackTarget.Value))
						attackForce.Add(a);

				unitsHangingAroundTheBase.Clear();
			}

            // If we have any attackers, let them scan for enemy units and stop and regroup if they spot any
            if (attackForce.Count > 0 )
            {
                bool foundEnemy = false;
                foreach (var a1 in attackForce)
                {
                    List<Actor> enemyUnits = world.FindUnitsInCircle(a1.CenterLocation, Game.CellSize * 10).Where(unit => IsHumanPlayer(unit.Owner) ).ToList();
                    if (enemyUnits.Count > 0)
                    {
                        //BotDebug("Found enemy "+enemyUnits.First().Info.Name);
                        // Found enemy units nearby.
                        foundEnemy = true;
                        var enemy = enemyUnits.OrderBy( e => (e.Location - a1.Location).LengthSquared ).First();
                        
                        // Check how many own units we have gathered nearby...
                        List<Actor> ownUnits = world.FindUnitsInCircle(a1.CenterLocation, Game.CellSize * 2).Where(unit => unit.Owner == p).ToList();
                        if (ownUnits.Count < Info.SquadSize)
                        {
                            // Not enough to attack. Send more units.
                            world.IssueOrder(new Order("Stop", a1, false) { });
                            foreach (var a2 in attackForce)
                            {
                                if (a2 != a1)
                                    world.IssueOrder(new Order("AttackMove", a2, false) { TargetLocation = a1.Location });
                            }
                        }
                        else
                        {
                            // We have gathered sufficient units. Attack the nearest enemy unit.
                            foreach (var a2 in attackForce)
                            {
                                world.IssueOrder(new Order("Attack", a2, false) { TargetActor = enemy });

                            }
                        }
                        return;
                    }
                }
                
                if (foundEnemy == false)
                {
                    attackTarget = ChooseEnemyTarget();
                    foreach (var a in attackForce)
                        TryToAttackMove(a, attackTarget.Value);
                }
            }
		}

		bool IsRallyPointValid(int2 x)
		{
			return world.IsCellBuildable(x, false);
		}

		void SetRallyPointsForNewProductionBuildings(Actor self)
		{
			var buildings = self.World.ActorsWithTrait<RallyPoint>()
				.Where(rp => rp.Actor.Owner == p && 
                    !IsRallyPointValid(rp.Trait.rallyPoint)).ToArray();

			if (buildings.Length > 0)
				BotDebug("Bot {0} needs to find rallypoints for {1} buildings.",
					p.PlayerName, buildings.Length);


			foreach (var a in buildings)
			{
				int2 newRallyPoint = ChooseRallyLocationNear(a.Actor.Location);
				world.IssueOrder(new Order("SetRallyPoint", a.Actor, false) { TargetLocation = newRallyPoint });
			}
		}

		//won't work for shipyards...
		int2 ChooseRallyLocationNear(int2 startPos)
		{
			var possibleRallyPoints = world.FindTilesInCircle(startPos, 8).Where(x => world.IsCellBuildable(x, false)).ToArray();
			if (possibleRallyPoints.Length == 0)
			{
				Game.Debug("Bot Bug: No possible rallypoint near {0}", startPos);
				return startPos;
			}

			return possibleRallyPoints.Random(random);
		}

		int2? ChooseDestinationNear(Actor a, int2 desiredMoveTarget)
		{
			if (!a.HasTrait<IMove>())
				return null;

			int2 xy;
			int loopCount = 0; //avoid infinite loops.
			int range = 2;
			do
			{
				//loop until we find a valid move location
				xy = new int2(desiredMoveTarget.X + random.Next(-range, range), desiredMoveTarget.Y + random.Next(-range, range));
				loopCount++;
				range = Math.Max(range, loopCount / 2);
				if (loopCount > 10) return null;
			} while (!a.Trait<IMove>().CanEnterCell(xy) && xy != a.Location);

			return xy;
		}

		//try very hard to find a valid move destination near the target.
		//(Don't accept a move onto the subject's current position. maybe this is already not allowed? )
		bool TryToMove(Actor a, int2 desiredMoveTarget)
		{
			var xy = ChooseDestinationNear(a, desiredMoveTarget);
			if (xy == null)
				return false;
			world.IssueOrder(new Order("Move", a, false) { TargetLocation = xy.Value });
			return true;
		}

		//try very hard to find a valid move destination near the target.
		//(Don't accept a move onto the subject's current position. maybe this is already not allowed? )
		bool TryToAttackMove(Actor a, int2 desiredMoveTarget)
		{
			var xy = ChooseDestinationNear(a, desiredMoveTarget);
			if (xy == null)
				return false;
			world.IssueOrder(new Order("AttackMove", a, false) { TargetLocation = xy.Value });
			return true;
		}

		void DeployMcv(Actor self)
		{
			/* find our mcv and deploy it */
            var mcv = self.World.Actors
                .FirstOrDefault(a => a.Owner == p && a.Info == Rules.Info["mcv"]);

			if (mcv != null)
			{
				baseCenter = mcv.Location;
				world.IssueOrder(new Order("DeployTransform", mcv, false));
			}
			else
				BotDebug("AI: Can't find the MCV.");
		}

		//Build a random unit of the given type. Not going to be needed once there is actual AI...
		private void BuildRandom(string category)
		{
			// Pick a free queue
			var queue = world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == p &&
					   a.Trait.Info.Type == category &&
					   a.Trait.CurrentItem() == null)
				.Select(a => a.Trait)
				.FirstOrDefault();

			if (queue == null)
				return;

			var unit = ChooseRandomUnitToBuild(queue);
			Boolean found = false;
			if (unit != null)
			{
				foreach (var un in Info.UnitsToBuild)
				{
					if (un.Key == unit.Name)
					{
						found = true;
						break;
					}
				}

				if (found == true)
				{
					world.IssueOrder(Order.StartProduction(queue.self, unit.Name, 1));
				}
			}
		}

		class BaseBuilder
		{
			BuildState state = BuildState.WaitForFeedback;
			string category;
			HackyAI ai;
			int lastThinkTick;
			Func<ProductionQueue, ActorInfo> chooseItem;

			public BaseBuilder(HackyAI ai, string category, Func<ProductionQueue, ActorInfo> chooseItem)
			{
				this.ai = ai;
				this.category = category;
				this.chooseItem = chooseItem;
			}

			public void Tick()
			{
				// Pick a free queue
				var queue = ai.world.ActorsWithTrait<ProductionQueue>()
					.Where(a => a.Actor.Owner == ai.p && a.Trait.Info.Type == category)
					.Select(a => a.Trait)
					.FirstOrDefault();

				if (queue == null)
					return;

				var currentBuilding = queue.CurrentItem();
				switch (state)
				{
					case BuildState.ChooseItem:
						{
							var item = chooseItem(queue);
							if (item == null)
							{
								state = BuildState.WaitForFeedback;
								lastThinkTick = ai.ticks;
							}
							else
							{
								BotDebug("AI: Starting production of {0}".F(item.Name));
								state = BuildState.WaitForProduction;
								ai.world.IssueOrder(Order.StartProduction(queue.self, item.Name, 1));
							}
						}
						break;

					case BuildState.WaitForProduction:
						if (currentBuilding == null) return;	/* let it happen.. */

						else if (currentBuilding.Paused)
							ai.world.IssueOrder(Order.PauseProduction(queue.self, currentBuilding.Item, false));
						else if (currentBuilding.Done)
						{
							state = BuildState.WaitForFeedback;
							lastThinkTick = ai.ticks;

							/* place the building */
							var location = ai.ChooseBuildLocation(currentBuilding);
							if (location == null)
							{
								BotDebug("AI: Nowhere to place {0}".F(currentBuilding.Item));
								ai.world.IssueOrder(Order.CancelProduction(queue.self, currentBuilding.Item, 1));
							}
							else
							{
								ai.world.IssueOrder(new Order("PlaceBuilding", ai.p.PlayerActor, false)
									{
										TargetLocation = location.Value,
										TargetString = currentBuilding.Item
									});
							}
						}
						break;

					case BuildState.WaitForFeedback:
						if (ai.ticks - lastThinkTick > feedbackTime)
							state = BuildState.ChooseItem;
						break;
				}
			}
		}
	}
}
