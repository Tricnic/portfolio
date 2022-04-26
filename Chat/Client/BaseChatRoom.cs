using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace BSCore.Chat
{
    public abstract class BaseChatRoom
    {
        private const int CACHED_MESSAGE_LIMIT = 50;

        public BaseChatRoom(ChatClient chatClient, GameConfigData gameConfigData)
        {
            _chatClient = chatClient;
            _gameConfigData = gameConfigData;
        }

        public BaseChatRoom(Color color, ChatClient chatClient, GameConfigData gameConfigData)
        {
            _color = ColorToHex(color);
            _chatClient = chatClient;
            _gameConfigData = gameConfigData;
        }

        public abstract string Id { get; }

        protected readonly string _color;
        protected readonly ChatClient _chatClient;
        protected readonly GameConfigData _gameConfigData;

        protected Queue<string> _cachedMessages = new Queue<string>();
        public List<string> CachedMessages { get { return new List<string>(_cachedMessages); } }

#pragma warning disable IDE1006 // Naming Styles
        private event System.Action<string> _messageReceived;
#pragma warning restore IDE1006 // Naming Styles
        public event System.Action<string> MessageReceived { add { _messageReceived += value; } remove { _messageReceived -= value; } }
        protected void RaiseMessageReceived(string displayName, string message)
        {
            message = $"{displayName}<color=#{_color}>{message}</color>";
            _cachedMessages.Enqueue(message);
            if(_cachedMessages.Count > CACHED_MESSAGE_LIMIT)
            {
                _cachedMessages.Dequeue();
            }
            _messageReceived?.Invoke(message);
        }

        protected string ColorToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }
    }
}
