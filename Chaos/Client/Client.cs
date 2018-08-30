// ****************************************************************************
// This file belongs to the Chaos-Server project.
// 
// This project is free and open-source, provided that any alterations or
// modifications to any portions of this project adhere to the
// Affero General Public License (Version 3).
// 
// A copy of the AGPLv3 can be found in the project directory.
// You may also find a copy at <https://www.gnu.org/licenses/agpl-3.0.html>
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Chaos
{
    internal sealed class Client
    {
        private readonly object Sync = new object();
        private byte[] ClientBuffer;
        private List<byte> FullClientBuffer;
        private ClientPackets.Handler[] PacketHandlers { get; }

        internal Queue<ServerPacket> SendQueue;
        internal bool Connected;
        internal byte Signature;
        internal byte StepCount;
        internal IPAddress IpAddress { get; }
        internal ServerType ServerType { get; set; }
        internal Server Server { get; }
        internal Socket ClientSocket { get; }
        internal Crypto Crypto { get; set; }
        internal User User { get; set; }
        internal string CreateCharName { get; set; }
        internal string CreateCharPw { get; set; }
        internal int Id { get; }
        internal DateTime LastClickObj { get; set; }
        internal DateTime LastRefresh { get; set; }
        internal Dialog CurrentDialog { get; set; }
        internal object ActiveObject { get; set; }
        internal bool IsLoopback = false;

        /// <summary>
        /// Base constructor for a new client with reference to the server, and the user's socket.
        /// </summary>
        /// <param name="server">The game server.</param>
        /// <param name="socket">The client's socket.</param>
        internal Client(Server server, Socket socket)
        {
            Connected = false;
            ClientBuffer = new byte[4096];
            FullClientBuffer = new List<byte>();
            SendQueue = new Queue<ServerPacket>();

            Server = server;
            ServerType = ServerType.Lobby;
            ClientSocket = socket;
            Crypto = new Crypto();
            PacketHandlers = ClientPackets.Handlers;
            Signature = 0;
            StepCount = 1;
            Id = Interlocked.Increment(ref Server.NextClientId);
            IpAddress = (socket.RemoteEndPoint as IPEndPoint).Address;

            LastClickObj = DateTime.MinValue;
            LastRefresh = DateTime.MinValue;
            IsLoopback = IpAddress.Equals(IPAddress.Loopback);
        }

        /// <summary>
        /// Adds the client to the client list, then begins receiving data.
        /// </summary>
        internal void Connect()
        {
            if (Server.TryAddClient(this))
            {
                Connected = true;

                //when we receive data, copy the readable data to the client buffer and call endreceive
                ClientSocket.BeginReceive(ClientBuffer, 0, ClientBuffer.Length, SocketFlags.None, new AsyncCallback(ClientEndReceive), ClientSocket);

                if (ServerType != ServerType.World)
                {
                    Enqueue(ServerPackets.AcceptConnection());
                    Enqueue(ServerPackets.ChangeCounter());
                }

                Server.WriteLog($@"Connection accepted", this);
            }
            else
            {
                Server.WriteLog($@"Connection failure", this);
            }
        }

        /// <summary>
        /// Disconnects the client from the server.
        /// </summary>
        /// <param name="wait">False if you want to immediately kill the client. True if you want the client to time out.</param>
        internal void Disconnect(bool wait = false)
        {
            lock (Sync)
            {
                Connected = false;
                if (wait)
                    return;

                if (Server.TryRemoveClient(this))
                {
                    if (User != null)
                        try { Game.World.RemoveClient(this); }
                        catch { }
                    ClientSocket.Disconnect(false);
                }
            }
        }

        /// <summary>
        /// Asynchronously receives data from the client, and processes the information.
        /// </summary>
        /// <param name="ar">Result of the async operation.</param>
        private void ClientEndReceive(IAsyncResult ar)
        {
            try
            {
                //get the length of the packet
                int length = ClientSocket.EndReceive(ar);
                //if we receive a packet with no length, disconnect the client, something went wrong
                if (length == 0)
                    Disconnect();
                else
                {
                    //otherwise copy the client buffer into a new byte array sized to fit the length of the packet
                    byte[] data = new byte[length];
                    Buffer.BlockCopy(ClientBuffer, 0, data, 0, length);
                    //copy that array into the full client buffer, so we can deal with the information in a properly sized list
                    FullClientBuffer.AddRange(data);
                    while (FullClientBuffer.Count > 3)
                    {
                        //check to see if it's a valid packet, this gives us the number of bytes that arent trailing random shit
                        int count = (FullClientBuffer[1] << 8) + FullClientBuffer[2] + 3;
                        if (count <= FullClientBuffer.Count)
                        {
                            //create a clientpacket out of the readable data
                            ClientPacket clientPacket = new ClientPacket(FullClientBuffer.GetRange(0, count).ToArray());
                            //remove the data from the fullclientbuffer
                            FullClientBuffer.RemoveRange(0, count);
                            //send it off to be processed by the server

                            if (clientPacket != null)
                            {
                                if (clientPacket.IsEncrypted)
                                    clientPacket.Decrypt(Crypto);

                                if (clientPacket.IsDialog)
                                    clientPacket.DecryptDialog();

                                try
                                {
                                    PacketHandlers[clientPacket.OpCode](this, clientPacket);
                                }
                                catch (Exception e)
                                {
                                    Server.WriteLog(e.ToString(), this);
                                }
                            }
                        }
                        else
                            break;
                    }
                }
                if (Connected) //begin checking for received info again
                    ClientSocket.BeginReceive(ClientBuffer, 0, ClientBuffer.Length, SocketFlags.None, new AsyncCallback(ClientEndReceive), ClientSocket);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Asynchronously finalizes the sending of a packet.
        /// </summary>
        /// <param name="ar">Result of the async operation.</param>
        private void EndSend(IAsyncResult ar) => ((Socket)ar.AsyncState).EndSend(ar);

        /// <summary>
        /// Queues a packet up to be sent to the client.
        /// </summary>
        /// <param name="packets">Packet(s) to be sent.</param>
        internal void Enqueue(params ServerPacket[] packets)
        {
            lock (SendQueue)
            {
                for (int i = 0; i < packets.Length; i++)
                    if (packets[i] != null)
                        SendQueue.Enqueue(packets[i]);
            }
        }

        /// <summary>
        /// Begins an asynchronous operation to send a packet to the client.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        internal void Send(ServerPacket packet)
        {
            byte[] data = packet.ToArray();
            try { ClientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(EndSend), ClientSocket); }
            catch
            {
                //do things to save the connection?
            }
        }

        /// <summary>
        /// Redirects the client to another server, or in this case... the same server.
        /// </summary>
        /// <param name="redirect">Redirect information.</param>
        internal void Redirect(Redirect redirect)
        {
            Server.Redirects.Add(redirect);
            Enqueue(ServerPackets.Redirect(redirect));
        }

        /// <summary>
        /// Sends a message to the client when theyre at the login screen.
        /// </summary>
        internal void SendLoginMessage(LoginMessageType messageType, string message = "") => Enqueue(ServerPackets.LoginMessage(messageType, message));

        /// <summary>
        /// Refreshes the clients view of their current hp, mp and other attributes.
        /// </summary>
        internal void SendAttributes(StatUpdateType updateType) => Enqueue(ServerPackets.Attributes(User.IsAdmin, updateType, User.Attributes));

        /// <summary>
        /// Sends a message to the client when they're already logged in.
        /// </summary>
        internal void SendServerMessage(ServerMessageType messageType, string message) => Enqueue(ServerPackets.ServerMessage(messageType, message));

        /// <summary>
        /// Sends a public message and it's origins to the client.
        /// </summary>
        /// <param name="sourceId">ID of the source of the message.</param>
        internal void SendPublicMessage(PublicMessageType messageType, int sourceId, string message) => Enqueue(ServerPackets.PublicChat(messageType, sourceId, message));

        /// <summary>
        /// Sends an animation to the client and adds it to the animation history.
        /// </summary>
        /// <param name="animation">The animation to send.</param>
        internal void SendAnimation(Animation animation)
        {
            Enqueue(ServerPackets.Animation(animation));
        }

        /// <summary>
        /// Sends an effect to the client's spellbar.
        /// </summary>
        /// <param name="eff">The effect to send.</param>
        /// <param name="remove">Whether or not to remove the effect from the spell bar.</param>
        internal void SendEffect(Effect eff, bool remove = false)
        {
            if (remove)
            {
                User.EffectsBar.TryRemove(eff);
                Enqueue(ServerPackets.EffectsBar(eff.Sprite, EffectsBarColor.None));
            }
            else
                Enqueue(ServerPackets.EffectsBar(eff.Sprite, eff.Color()));
        }
        /// <summary>
        /// Sends a persuit menu to the client. Sets necessary client variables.
        /// </summary>
        /// <param name="merchant">Merchant with a merchantmenu.</param>
        internal void SendMenu(Merchant merchant)
        {
            ActiveObject = merchant;
            if (merchant.Menu.Type == MenuType.Dialog)
            {
                if (merchant.NextDialogId == 0)
                    CurrentDialog = Game.Dialogs.ItemOrMerchantMenuDialog(PursuitIds.None, 0, merchant.Menu.Text, merchant.Menu.Dialogs);
                else
                    CurrentDialog = Game.Dialogs[merchant.NextDialogId];

                Enqueue(ServerPackets.DisplayMenu(this, merchant, CurrentDialog));
            }
            else
                Enqueue(ServerPackets.DisplayMenu(this, merchant));
        }

        /// <summary>
        /// Sends a dialog to the client. Sets necessary client variables.
        /// </summary>
        /// <param name="invoker">Item or Merchant that's the source of the dialog.</param>
        /// <param name="dialog">The dialog to be sent.</param>
        internal void SendDialog(object invoker, Dialog dialog)
        {
            if (dialog.Type != DialogType.CloseDialog)
            {
                ActiveObject = invoker;
                CurrentDialog = dialog;
            }
            else
            {
                ActiveObject = null;
                CurrentDialog = null;
            }

            Enqueue(ServerPackets.DisplayDialog(invoker, dialog));
        }

        ~Client()
        {
            Disconnect();
        }
    }
}
