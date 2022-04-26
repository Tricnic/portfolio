using UnityEngine;

namespace BSCore.Chat
{
    public class PrivateChatRoom : BaseChatRoom
    {
        public PrivateChatRoom(ChatClient chatClient, GameConfigData gameConfigData)
            : base(gameConfigData.PrivateMessageColor, chatClient, gameConfigData)
        {
        }

        ~PrivateChatRoom()
        {
        }

        public override string Id => "Private";

        public void SendMessage(string recipientId, string message)
        {
            _chatClient.SendPrivateMessage(recipientId, message);
        }

        public void OnMessageReceived(PrivateMessage message)
        {
            string displayName = string.Format("{0} {1}", 
                message.isMe ? "To" : message.senderNickname, 
                message.isMe ? message.senderNickname : "whispered");

            RaiseMessageReceived($"<font=\"American Captain SDF\">{displayName}</font>: ", message.message);
        }
    }
}
