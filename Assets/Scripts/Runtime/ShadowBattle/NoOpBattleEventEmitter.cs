using System.Collections.Generic;

namespace Kindrith.ShadowBattle
{
    public sealed class NoOpBattleEventEmitter : IBattleEventEmitter
    {
        public void Emit(string name, IDictionary<string, object> parameters) { }
    }
}
