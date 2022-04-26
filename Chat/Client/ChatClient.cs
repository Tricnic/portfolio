using BestHTTP.SocketIO;
using System.Collections.Generic;
using UnityEngine;
using NodeClient;

namespace BSCore.Chat
{
    public class ChatClient
    {

        public ChatClient(SocketClient socketClient, GameConfigData gameConfigData)
        {
            _socketClient = socketClient;
            _gameConfigData = gameConfigData;
            SystemChatRoom = new SystemChatRoom(this, _gameConfigData);
            PrivateChatRoom = new PrivateChatRoom(this, _gameConfigData);
            _socketClient.ChannelJoined += OnChannelJoined;
            _socketClient.ChannelLeft += OnChannelLeft;
            Init();
        }

        public event System.Action Connected { add { _socketClient.Connected += value; } remove { _socketClient.Connected -= value; } }
        public event System.Action Disconnected { add { _socketClient.Disconnected += value; } remove { _socketClient.Disconnected -= value; } }
        public event System.Action Reconnected { add { _socketClient.Reconnected += value; } remove { _socketClient.Reconnected -= value; } }
        public event System.Action Authenticated { add { _socketClient.Authenticated += value; } remove { _socketClient.Authenticated -= value; } }
        private event System.Action<PublicChatRoom> _channelJoined;
        public event System.Action<PublicChatRoom> ChannelJoined { add { _channelJoined += value; } remove { _channelJoined -= value; } }
        private void RaiseChannelJoined(PublicChatRoom channel) { _channelJoined?.Invoke(channel); }
        public event System.Action<string> ChannelLeft { add { _socketClient.ChannelLeft += value; } remove { _socketClient.ChannelLeft -= value; } }

        public bool IsConnected => _socketClient.IsConnected;
        public bool IsAuthenticated => _socketClient.IsAuthenticated;
        public SystemChatRoom SystemChatRoom { get; private set; }
        public PrivateChatRoom PrivateChatRoom { get; private set; }
        public readonly Dictionary<string, PublicChatRoom> ChatRooms = new Dictionary<string, PublicChatRoom>();

        private readonly SocketClient _socketClient;
        private readonly GameConfigData _gameConfigData;

        #region Public API
        public void Disconnect()
        {
            Cleanup();
        }

        public PublicChatRoom JoinChannel(string channelId)
        {
            Debug.Log($"[ChatClient] Joining channel {channelId}");
            if (!ChatRooms.TryGetValue(channelId, out PublicChatRoom room))
            {
                if (_socketClient.TryGetChannelById(channelId, out ChannelCrumb channelCrumb))
                {
                    room = new PublicChatRoom(this, channelCrumb, _gameConfigData, _socketClient.ServiceId);
                }
                else
                {
                    room = new PublicChatRoom(this, channelId, _gameConfigData, _socketClient.ServiceId);
                    if (channelId != ChatController.GENERAL_CHANNEL)
                    {
                        DelayedAction.RunWhen(() => _socketClient.IsAuthenticated, () => _socketClient.JoinChannel(channelId));
                    }
                }
            }
            return room;
        }

        public void LeaveChannel(string channelId)
        {
            _socketClient.LeaveChannel(channelId);
        }

        public void SendPublicMessage(string channelId, string message)
        {
            //Debug.LogFormat("[ChatClient] Sending message to channel {0}: {1}", roomId, message);
            var request = new PublicMessageRequest
            {
                c = channelId,
                m = message
            };
            _socketClient.Emit(EventNames.PublicMessage, request);
        }

        public void SendPrivateMessage(string recipientId, string message)
        {
            //Debug.LogFormat("[ChatClient] Sending message to {0}: {1}", recipientId, message);
            var request = new PrivateMessageRequest
            {
                i = recipientId,
                m = message
            };
            _socketClient.Emit(EventNames.PrivateMessage, request);
        }

        public void DisplaySystemMessage(string text)
        {
            SystemChatRoom.OnMessageReceived(new SystemMessage(text));
        }

        public bool TryGetServiceIdByNicknameFromAnyChannel(string nickname, out string serviceId)
        {
            return _socketClient.TryGetServiceIdByNicknameFromAnyChannel(nickname, out serviceId);
        }

        public bool TryGetChannelById(string roomId, out PublicChatRoom room)
        {
            return ChatRooms.TryGetValue(roomId, out room);
        }
        #endregion

        #region Setup/Cleanup
        private void Init()
        {
#if DEBUG
            Debug.LogFormat("[ChatClient] Starting up...");
#endif
            // Chat messages
            _socketClient.On(EventNames.PublicMessage, OnPublicMessage);
            _socketClient.On(EventNames.PrivateMessage, OnPrivateMessage);
        }

        private void Cleanup()
        {
#if DEBUG
            Debug.LogFormat("[ChatClient] Shutting down...");
#endif
            // Chat messages
            _socketClient.Off(EventNames.PublicMessage, OnPublicMessage);
            _socketClient.Off(EventNames.PrivateMessage, OnPrivateMessage);
        }
        #endregion

        private void OnChannelJoined(ChannelCrumb channelCrumb)
        {
            if (ChatRooms.TryGetValue(channelCrumb.Id, out PublicChatRoom room))
            {
                room.AssignChannel(channelCrumb);
            }
            else
            {
                room = new PublicChatRoom(this, channelCrumb, _gameConfigData, _socketClient.ServiceId);
                ChatRooms.Add(channelCrumb.Id, room);
            }
            Debug.Log($"[ChatClient] Joined channel {channelCrumb.Id}");
            RaiseChannelJoined(room);
        }

        private void OnChannelLeft(string channelId)
        {
            if (ChatRooms.TryGetValue(channelId, out PublicChatRoom room))
            {
                room.RemoveChannel();
            }
        }

        private void OnPublicMessage(string payload, object[] args)
        {
            if(args == null || args.Length <= 0)
            {
                Debug.LogWarning("[ChatClient] Received public message with incomplete data");
                return;
            }
#if DEBUG
            Debug.LogFormat("[ChatClient] Received public message: {0}", string.Join(", ", args));
#endif
            PublicMessageCrumb messageCrumb = SocketClient.ConvertTo<PublicMessageCrumb>(args[0]);
            if (_socketClient.TryGetChannelById(messageCrumb.channelId, out ChannelCrumb channel))
            {
                string senderNickname = "";
                if (string.IsNullOrEmpty(messageCrumb.senderId))
                {
                    Debug.LogErrorFormat("[ChatClient] Received public message, but senderId was null: {0}", messageCrumb);
                }
                else if (channel.TryGetPlayerById(messageCrumb.senderId, out PlayerCrumb crumb))
                {
                    senderNickname = crumb.Nickname;
                }
                channel.RaiseMessageReceived(messageCrumb.ToMessage(senderNickname, _socketClient.ServiceId));
            }
            else
            {
                Debug.LogWarning("[ChatClient] Received public message for a channel not subscribed to.");
            }
        }

        private void OnPrivateMessage(string payload, object[] args)
        {
#if DEBUG
            Debug.LogFormat("[ChatClient] Received private message: {0}", payload);
#endif
            PrivateMessageCrumb messageCrumb = SocketClient.ConvertTo<PrivateMessageCrumb>(args[0]);
            PrivateMessage message = messageCrumb.ToMessage(_socketClient.ServiceId);
            PrivateChatRoom.OnMessageReceived(message);
        }
    }
}
