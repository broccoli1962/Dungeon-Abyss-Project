using System.Collections.Generic;
using DdaIT.Object.Session;
using R3;
using UnityEngine;

namespace DdaIT.Object.Character.Player
{
    public class PlayerCombatController : CombatController
    {
        private readonly List<HitTarget> _pendingHits = new();

        /// <summary>서버에서 조회한 무기 충돌을 세션 타격 대상으로 변환한다.</summary>
        protected override void Awake()
        {
            base.Awake();
            WeaponHit.Subscribe(OnWeaponHit).AddTo(this);
        }

        private void OnWeaponHit(Collider other)
        {
            if (other == null || other.transform.IsChildOf(transform))
                return;

            if (other.TryGetComponent<IHitReceiver>(out var receiver) && receiver.TryHit(this, out var target))
                _pendingHits.Add(target);
        }

        /// <summary>누적된 타격을 시뮬레이션 버퍼에 옮기고 비운다.</summary>
        public void DrainHit(List<HitTarget> hits)
        {
            hits.AddRange(_pendingHits);
            ClearHits();
        }

        /// <summary>상태 전환 전에 수집한 타격을 버린다.</summary>
        public void ClearHits() => _pendingHits.Clear();
    }
}
