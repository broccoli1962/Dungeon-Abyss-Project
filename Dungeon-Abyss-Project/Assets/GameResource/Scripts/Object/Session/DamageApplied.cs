namespace DdaIT.Object.Session
{
    public readonly struct DamageApplied
    {
        public readonly int AttackerSlot;
        public readonly int TargetKind;
        public readonly int TargetId;
        public readonly float Amount;
        public readonly float HpAfter;
        public readonly LifeState LifeStateAfter;

        public readonly bool DownOrDead;

        public DamageApplied(int attackerSlot,
            int targetKind,
            int targetId,
            float amount,
            float hpAfter,
            LifeState lifeStateAfter)
        {
            AttackerSlot = attackerSlot;
            TargetKind = targetKind;
            TargetId = targetId;
            Amount = amount;
            HpAfter = hpAfter;
            LifeStateAfter = lifeStateAfter;
            DownOrDead = lifeStateAfter == LifeState.Down ||
                        lifeStateAfter == LifeState.Dead;
        }
    }

    public readonly struct DoorOpened
    {
        public readonly int DoorId;
        public readonly SlotId By;

        public DoorOpened(int doorId, SlotId by)
        {
            DoorId = doorId;
            By = by;
        }
    }

    public readonly struct ItemPicked
    {
        public readonly int ItemId;
        public readonly SlotId By;

        public ItemPicked(int itemId, SlotId by)
        {
            ItemId = itemId;
            By = by;
        }
    }
}