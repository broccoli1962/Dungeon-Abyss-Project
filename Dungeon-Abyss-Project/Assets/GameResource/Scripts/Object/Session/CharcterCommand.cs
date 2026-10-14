using Unity.Netcode;
using UnityEngine;

namespace DdaIT.Object.Session
{
    public struct CharacterCommand : INetworkSerializable
    {
        public SlotId SlotId;
        public Vector3 Position;
        public Vector3 MoveDirection;
        public float SpeedRatio;
        public bool Sprint;
        public bool Aim;
        public bool AttackPressed;
        public bool InteractPressed;
        public float Yaw;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter{
            byte slot = SlotId.Value;

            serializer.SerializeValue(ref slot);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref MoveDirection);
            serializer.SerializeValue(ref SpeedRatio);
            serializer.SerializeValue(ref Sprint);
            serializer.SerializeValue(ref Aim);
            serializer.SerializeValue(ref AttackPressed);
            serializer.SerializeValue(ref InteractPressed);
            serializer.SerializeValue(ref Yaw);
        
            if(serializer.IsReader){
                SlotId = new SlotId(slot);
            }
        }
    }

    /// <summary>
    /// 애니메이션 상태 정보
    /// </summary>
    public struct CharacterAnimationParams{
        public float SpeedRatio;
        public bool IsAiming;
        public bool AttackPulse;
        public LifeState LifeState;
        public Vector3 MoveDirection;
    }
}
