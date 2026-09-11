using UnityEngine;
using DdaIT.Object.Session;

namespace DdaIT.Object.UI
{
    public class PlayerHudPanel : UIPanel<PlayerHudPresenter>
    {
        [SerializeField] private CommonGaugeBar _healthBar;
        [SerializeField] private CommonGaugeBar _staminaBar;
        [SerializeField] private CommonGaugeBar _hungerBar;

        public CommonGaugeBar HealthBar { get => _healthBar; }
        public CommonGaugeBar StaminaBar { get => _staminaBar; }
        public CommonGaugeBar HungerBar { get => _hungerBar; }

        public override UILayer Layer => UILayer.HUD;

        public void Bind(RunState state, RunEvents events, SlotId localSlot){
            Presenter.Bind(state, events, localSlot);
        }
    }
}
