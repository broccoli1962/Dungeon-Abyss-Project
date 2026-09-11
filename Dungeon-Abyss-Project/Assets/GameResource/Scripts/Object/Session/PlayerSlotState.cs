using UnityEngine;

namespace DdaIT.Object.Session
{
    public struct CombatRuntime
    {
        public float Hp, MaxHp;
        //공격력 기본값
        public float BaseAttack;
        //파생 캐시
        public float Attack;
        public float MoveSpeedMul;

        //효과
        public ActiveEffects[] Effects;
    }

    public struct ActiveEffects
    {
        public int EffectId;
        public float Remaining;
        public byte Stacks;
    }

    public struct PlayerSlotState
    {
        public SlotId Slot;
        public bool IsJoined;
        public LifeState LifeState;

        public CombatRuntime Combat;

        //자원
        public float Hunger, MaxHunger;
        public float Stamina, MaxStamina;

        public Vector3 Position;
        public float Yaw;
        public bool Aiming;
        public bool HasKey;
        public CharacterAnimationParams AnimationParams;
    }

    public struct MonsterState
    {
        public int MonsterId;
        public bool IsAlive;
        public int TargetSlot; // -1 == None

        public CombatRuntime Combat;

        public float Yaw;
        public Vector3 Position;
    }

    public struct DoorState
    {
        public int DoorId;
        public bool Locked;
        public bool IsOpen;
    }

    /// <summary>
    /// 런 원본
    /// </summary>
    public sealed class RunState
    {
        public uint RunSeed;
        public int FoorIndex;
        public RunPhase phase;
        public PlayerSlotState[] Players;
        public MonsterState[] Monsters;
        public DoorState[] Doors;

        public const int MaxPlayer = 4;

        public RunState()
        {
            Players = new PlayerSlotState[MaxPlayer];
            for (int i = 0; i < MaxPlayer; i++)
                Players[i].Slot = new SlotId((byte)i);
        }
    }
}