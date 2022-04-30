using Hkmp.Api.Client;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

namespace Satchel.HkmpPipe{
    public class Client : ClientAddon {
        internal static Client Instance;
        internal IClientApi clientApi;
        internal event EventHandler<RecievedEventArgs> OnRecieve;

        public Client() {
            Instance = this;
        }
        internal void send(ushort fromPlayer,ushort toPlayer,string _mod,string _eventName,string _eventData,bool _rebroadcast = false, bool _broadcastToAll = false ,bool _reliable = false,bool sameScene = false){
            if(!clientApi.NetClient.IsConnected){
                return;
            }
            Modding.Logger.LogDebug("client send" + _eventName);
            var netSender = clientApi.NetClient.GetNetworkSender<Packets>(Instance);

            // SendCollectionData using the given packet ID
            netSender.SendCollectionData(Packets.GenericPacket, new GenericPacket {
               _isReliable = _reliable,
               _dropReliableDataIfNewerExists = false,
               mod = _mod,
               rebroadcast = _rebroadcast,
               broadcastToAll = _broadcastToAll,
               fromPlayer = fromPlayer,
               toPlayer = toPlayer,
               eventName = _eventName,
               eventData = _eventData,
               sameScene = sameScene
            });
        }
 
        public override void Initialize(IClientApi _clientApi){
            this.clientApi = _clientApi;
            var netReceiver = clientApi.NetClient.GetNetworkReceiver<Packets>(Instance, InstantiatePacket);

            netReceiver.RegisterPacketHandler<GenericPacket>(
                Packets.GenericPacket,
                packetData => {
                    Modding.Logger.LogDebug("Client recieve |  " + packetData.eventName + $"[{packetData.eventData}]");
                    //broadcast event to all client addons
                    OnRecieve?.Invoke(this,new RecievedEventArgs{
                        packet = packetData
                    });
                }
            );
        }

        protected override string Name => "Satchel";
        protected override string Version => AssemblyUtils.ver;
        public override bool NeedsNetwork => true;


        private static IPacketData InstantiatePacket(Packets packetId) {
            switch (packetId) {
                case Packets.GenericPacket:
                    return new PacketDataCollection<GenericPacket>();
            }
            return null;
        }
    }
}