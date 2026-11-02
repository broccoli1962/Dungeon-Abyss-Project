using DdaIT.Object.Session;
using UnityEngine;

namespace DdaIT
{
    public interface IHitReceiver
    {
        bool TryHit(Component attacker, out HitTarget target);
    }
}
