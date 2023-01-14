using SeamlessClientPlugin.Messages;
using VRage.GameServices;

namespace SeamlessClientPlugin.SeamlessTransfer
{
    public class ServerPing
    {
        private static WorldRequest Request => _transfer.WorldRequest;
        private static Transfer _transfer;

        public static void StartServerPing(Transfer clientTransfer)
        {
            // We need to first ping the server to make sure its running and so we can get a connection
            _transfer = clientTransfer;

            if (_transfer.TargetServerId == 0)
            {
                SeamlessClient.TryShow("This is not a valid server!");
                return;
            }

            var server = new MyGameServerItem
            {
                ConnectionString = _transfer.IpAddress,
                SteamID = _transfer.TargetServerId,
                Name = _transfer.ServerName
            };

            SeamlessClient.TryShow($"Beginning Redirect to server: {_transfer.TargetServerId}");

            var world = Request.DeserializeWorldData();

            var switcher = new SwitchServers(server, world);
            switcher.BeginSwitch();
        }
    }
}