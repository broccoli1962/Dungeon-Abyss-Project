using UnityEngine;
using Unity.Netcode;
using System;

namespace DdaIT.Object.Session
{
    public struct PlayerPose : INetworkSerializable, IEquatable<PlayerPose>
    {
        public Vector3 Position;
        public float Yaw;
        public float SpeedRatio;
        public bool IsAiming;
        public Vector3 MoveDirection;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Yaw);
            serializer.SerializeValue(ref MoveDirection);
            serializer.SerializeValue(ref SpeedRatio);
            serializer.SerializeValue(ref IsAiming);
        }
        public bool Equals(PlayerPose other)
        {
            return Position == other.Position
                && MoveDirection == other.MoveDirection
                && Mathf.Approximately(Yaw, other.Yaw)
                && Mathf.Approximately(SpeedRatio, other.SpeedRatio)
                && IsAiming == other.IsAiming;
        }
    }
}
