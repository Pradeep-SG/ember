using System.Collections.Generic;
using Kindrith.Analytics;
using Kindrith.ShadowBattle;

namespace Kindrith.UI
{
    // Bridges Kindrith.ShadowBattle.IBattleEventEmitter (the slim interface the battle subsystem
    // exposes) onto Kindrith.Analytics.AnalyticsBus (which adds the common envelope). Lives in
    // Kindrith.UI because the UI layer is the assembly that references both subsystems.
    public sealed class AnalyticsBusAdapter : IBattleEventEmitter
    {
        readonly AnalyticsBus _bus;

        public AnalyticsBusAdapter(AnalyticsBus bus)
        {
            _bus = bus ?? throw new System.ArgumentNullException(nameof(bus));
        }

        public void Emit(string name, IDictionary<string, object> parameters)
        {
            _bus.Emit(name, parameters);
        }
    }
}
