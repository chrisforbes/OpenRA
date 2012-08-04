using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{

    class HealCrateActionInfo : CrateActionInfo
	{
		public readonly int Amount = -100;

		public override object Create(ActorInitializer init) { return new HealCrateAction(init.self, this); }
	}

	class HealCrateAction : CrateAction
	{
        public HealCrateAction(Actor self, HealCrateActionInfo info)
			: base(self,info) {}

        public override void Activate(Actor collector)
        {
            collector.World.AddFrameEndTask(w =>
            {
                var health = collector.TraitOrDefault<Health>();
                if (health != null)
                    self.InflictDamage(collector, (info as HealCrateActionInfo).Amount, null);
            });

            base.Activate(collector);
        }
    }
}
