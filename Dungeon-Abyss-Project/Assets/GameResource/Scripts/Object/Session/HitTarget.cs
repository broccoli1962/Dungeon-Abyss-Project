namespace DdaIT.Object.Session
{
    public enum HitTargetKind : byte
    {
        Player = 0,
        Monster = 1,
        World = 2,
    }
    public readonly struct HitTarget
    {
        public readonly HitTargetKind kind;
        public readonly int Id;

        public HitTarget(HitTargetKind kind, int id)
        {
            this.kind = kind;
            Id = id;
        }
    }

    public readonly struct PendingHit{
        public readonly HitTarget Target;
        public readonly uint CancellationVersion;

        public PendingHit(HitTarget target, uint cancellationVersion){
            Target = target;
            CancellationVersion = cancellationVersion;
        }
    }
}
