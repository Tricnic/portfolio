using UnityEngine;

namespace BSCore.Chat
{
    public class SystemChatRoom : BaseChatRoom
    {
        public SystemChatRoom(ChatClient chatClient, GameConfigData gameConfigData)
            : base (gameConfigData.SystemMessageColor, chatClient, gameConfigData)
        {
        }

        ~SystemChatRoom()
        {
        }

        public override string Id => "System";

        public void DisplayMessage(string message)
        {
            _chatClient.DisplaySystemMessage(message);
        }

        public void DisplayMessageFormat(string format, params object[] args)
        {
            _chatClient.DisplaySystemMessage(string.Format(format, args));
        }

        public void OnMessageReceived(SystemMessage message)
        {
            RaiseMessageReceived("", $"<color=#{_color}>{message.message}</color>");
        }
    }
}
