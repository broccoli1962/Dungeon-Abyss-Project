using DdaIT.Object.Session;
using R3;
using UnityEngine;

namespace DdaIT.Object.UI
{
    public class PlayerHudPresenter : UIPresenter<PlayerHudPanel>
    {
        private RunState _state;
        private RunEvents _events;
        private SlotId _localSlot;
        private CompositeDisposable _disposables = new CompositeDisposable();

        public void Bind(RunState state, RunEvents events, SlotId localSlot)
        {
            _state = state;
            _events = events;
            _localSlot = localSlot;
            
            Refresh();
            _events.DamageApplied.Subscribe(OnDamage).AddTo(_disposables);
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

        private void OnDamage(DamageApplied e){
            if(e.TargetKind != 0 || e.TargetId != _localSlot.Value){
                return;
            }
            Refresh();
        }

        private void Refresh(){
            ref var player = ref _state.Players[_localSlot.Value];
            View.HealthBar.SetRatio(player.Combat.Hp / player.Combat.MaxHp);
        }
    }
}
