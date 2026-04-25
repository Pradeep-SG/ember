using System.Collections.Generic;

namespace Kindrith.ShadowBattle
{
    // Stub interface for analytics events. WP-08 wires AnalyticsBus + NDJSON sink behind this.
    public interface IBattleEventEmitter
    {
        void Emit(string name, IDictionary<string, object> parameters);
    }
}
