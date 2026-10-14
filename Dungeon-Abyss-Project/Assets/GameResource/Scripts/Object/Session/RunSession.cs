using UnityEngine;
using Unity.Netcode;

namespace DdaIT.Object.Session
{
    public sealed class RunSession : NetworkBehaviour
    {
        public RunState State { get; private set;}
        public RunEvents Events { get; private set;}
        public RunSimulation Simulation { get; private set;}

        public override void OnNetworkSpawn(){
            if(!IsServer) return;

            State = new RunState();
            Events = new RunEvents();
            Simulation = new RunSimulation(State, Events, new NetworkSimAuthority());
            Simulation.StartRun(12345u);
        }
    }

    public sealed class NetworkSimAuthority : ISimAuthority
    {
        public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        public bool IsOwner(SlotId slot) => IsHost;
    }
}
