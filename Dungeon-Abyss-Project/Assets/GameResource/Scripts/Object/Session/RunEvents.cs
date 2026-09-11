using R3;

namespace DdaIT.Object.Session
{
    public sealed class RunEvents{
        public readonly Subject<DamageApplied> DamageApplied = new();
        public readonly Subject<DoorOpened> DoorOpened = new();
        public readonly Subject<ItemPicked> ItemPicked = new();
        public readonly Subject<RunPhase> PhaseChanged = new();

        public RunEvents(){
            DamageApplied = new Subject<DamageApplied>();
            DoorOpened = new Subject<DoorOpened>();
            ItemPicked = new Subject<ItemPicked>();
            PhaseChanged = new Subject<RunPhase>();
        }
    }
}