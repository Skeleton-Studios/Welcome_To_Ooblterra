using System;
using Unity.Netcode;


namespace Welcome_To_Ooblterra;
internal class WTONetworkHandler : NetworkBehaviour {
    public static WTONetworkHandler Instance { get; private set; }

    public static event Action<string> LevelEvent;

    public override void OnNetworkSpawn() {
        LevelEvent = null;
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer) { 
            Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
        }
        Instance = this;
        base.OnNetworkSpawn();
    }

    [ClientRpc]
    public void EventClientRpc(string eventName) {
        LevelEvent?.Invoke(eventName); // If the event has subscribers (does not equal null), invoke the event
    }
}
