using UnityEngine;
using Zenject;

namespace BSCore.Chat
{
    public enum MessageColorIds { Group, Public }
    public class ChatInstaller : Installer<ChatInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<ChatClient>().AsSingle().NonLazy();
            Container.Bind<ChatCommandController>().AsSingle();
        }
    }
}
