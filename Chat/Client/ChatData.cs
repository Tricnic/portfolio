using System.Collections.Generic;
using System.Linq;

namespace BSCore.Chat
{
    [System.Serializable]
    public class PublicMessageRequest
    {
        /// <summary>
        /// The ID of the room to send the message to
        /// </summary>
        public string c;
        /// <summary>
        /// The message to send
        /// </summary>
        public string m;
    }

    [System.Serializable]
    public class PrivateMessageRequest
    {
        /// <summary>
        /// The ID of the player to send the message to
        /// </summary>
        public string i;
        /// <summary>
        /// The message to send
        /// </summary>
        public string m;
    }

    public class PublicMessageCrumb
    {
        public string i;
        public string m;
        public string c;
        public bool a;

        public string senderId => i;
        public string message => m;
        public string channelId => c;
        public bool isAdmin => a;

        public PublicMessage ToMessage(string senderNickname, string myId)
        {
            return new PublicMessage(senderId, message, senderNickname, channelId, isAdmin, myId);
        }

        public override string ToString()
        {
            return string.Format("{0} -> {1}:[{2}]", senderId, channelId, message);
        }
    }

    public class PrivateMessageCrumb
    {
        public string i;
        public string m;
        public string sn;

        public string senderId { get { return i; } }
        public string message { get { return m; } }
        public string senderNickname { get { return sn; } }

        public PrivateMessage ToMessage(string myId)
        {
            return new PrivateMessage(message, senderId, senderNickname, myId);
        }

        public override string ToString()
        {
            return string.Format("{0}({1}) -> me:[{2}]", senderNickname, senderId, message);
        }
    }

    public class PublicMessage : NodeClient.Message
    {
        public PublicMessage(string senderId, string message, string senderNickname, string channelId, bool isAdmin, string myId)
            : base(channelId, message, senderId, senderNickname, isAdmin)
        {
            isMe = senderId == myId;
        }

        public bool isMe { get; private set; }
    }

    public class PrivateMessage : NodeClient.Message
    {
        public PrivateMessage(string message, string senderNickname, string channelId, string myId)
            : base(myId, message, senderNickname, channelId, false)
        {
            isMe = senderId == myId;
        }

        public bool isMe { get; private set; }
    }

    public class SystemMessage : NodeClient.Message
    {
        public SystemMessage(string message) : base("system", message, "system", "system", true) { }
    }
}
