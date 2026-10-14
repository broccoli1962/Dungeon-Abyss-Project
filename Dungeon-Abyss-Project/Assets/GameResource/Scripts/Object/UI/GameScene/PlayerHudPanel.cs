using UnityEngine;
using DdaIT.Object.Character;

namespace DdaIT.Object.UI
{
    public class PlayerHudPanel : UIPanel<PlayerHudPresenter>
    {
        [SerializeField] private SingleGaugeBar _healthBar;
        [SerializeField] private SingleGaugeBar _staminaBar;
        [SerializeField] private SingleGaugeBar _hungerBar;

        public SingleGaugeBar HealthBar { get => _healthBar; }
        public SingleGaugeBar StaminaBar { get => _staminaBar; }
        public SingleGaugeBar HungerBar { get => _hungerBar; }

        public override UILayer Layer => UILayer.HUD;

        public void Bind(PlayerNetwork playerNet){
            Presenter.Bind(playerNet);
        }
    }
}
