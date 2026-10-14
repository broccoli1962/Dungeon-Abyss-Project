using DdaIT.Object.Session;
using R3;
using DdaIT.Object.Character;
using Unity.VisualScripting;
using UnityEngine;

namespace DdaIT.Object.UI
{
    public class PlayerHudPresenter : UIPresenter<PlayerHudPanel>
    {
        private readonly CompositeDisposable _disposables = new();

        public void Bind(PlayerNetwork player)
        {
            _disposables.Clear();

            Refresh(player.Vitals);
            player.VitalChanged.Subscribe(Refresh).AddTo(_disposables);
        }

        public override void OnOpen()
        {
            base.OnOpen();
        }

        public override void OnClose()
        {
            _disposables.Clear();
            base.OnClose();
        }

        private void Refresh(PlayerVital vitals){
            SetGuage(View.HealthBar, vitals.Health, vitals.MaxHealth);
            SetGuage(View.StaminaBar, vitals.Stamina, vitals.MaxStamina);
            SetGuage(View.HungerBar, vitals.Hunger, vitals.MaxHunger);
        }

        private static void SetGuage(SingleGaugeBar bar, float value, float maxValue){
            if(bar == null) return;

            bar.SetRatio(maxValue > 0f ? value / maxValue : 0f);
            bar.SetText(Mathf.CeilToInt(value).ToString(), Mathf.CeilToInt(maxValue).ToString());
        }
    }
}
