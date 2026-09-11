using System;

namespace DdaIT.Object.Session
{
    //캐릭터 슬롯 ID
    public readonly struct SlotId : IEquatable<SlotId>
    {
        public readonly byte Value;
        public SlotId(byte value) => Value = value;
        public bool Equals(SlotId other) => Value == other.Value;
    }

    //캐릭터 생존 상태
    public enum LifeState : byte
    {
        Absent = 0,
        Alive = 1,
        Down = 2,
        Dead = 3,
    }

    //게임 진행 상태
    public enum RunPhase : byte{
        None = 0,
        // 로비
        Lobby = 1,
        // 게임 진행 중
        Playing = 2,
        // 층 클리어
        FloorClear = 3,
        // 파티 전멸
        PartyDown = 4,
        // 런 중 게임 중단
        Aborted = 5,
    }

    //월드에 작동하는 상호작용 종류
    public enum InteractKind : byte
    {
        Door = 0,
        Revive = 1,
        Pickup = 2,
    }
}
