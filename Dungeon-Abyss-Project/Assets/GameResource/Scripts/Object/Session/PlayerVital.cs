using System;
using Unity.Netcode;
using UnityEngine;

namespace DdaIT.Object.Session
{
    public struct PlayerVital : INetworkSerializable, IEquatable<PlayerVital>
    {
        public float Health, MaxHealth;
        public float Stamina, MaxStamina;
        public float Hunger, MaxHunger;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Health);
            serializer.SerializeValue(ref MaxHealth);
            serializer.SerializeValue(ref Stamina);
            serializer.SerializeValue(ref MaxStamina);
            serializer.SerializeValue(ref Hunger);
            serializer.SerializeValue(ref MaxHunger);
        }

        public bool Equals(PlayerVital other)
        {
            return Mathf.Approximately(Health, other.Health)
                && Mathf.Approximately(MaxHealth, other.MaxHealth)
                && Mathf.Approximately(Stamina, other.Stamina) 
                && Mathf.Approximately(MaxStamina, other.MaxStamina) 
                && Mathf.Approximately(Hunger, other.Hunger) 
                && Mathf.Approximately(MaxHunger, other.MaxHunger);
        }
    }
}
