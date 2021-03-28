using Cysharp.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Connections.Events;
using OpenMod.Unturned.Players.Equipment.Events;
using OpenMod.Unturned.Players.Inventory.Events;
using System.Threading.Tasks;

namespace MovementModifier
{
    public class UnturnedEventListener : IEventListener<UnturnedPlayerConnectedEvent>,
        IEventListener<UnturnedPlayerItemEquippedEvent>,
        IEventListener<UnturnedPlayerItemUnequippedEvent>,
        IEventListener<UnturnedPlayerInventoryUpdatedEvent>
    {
        private readonly MovementModifierPlugin m_Plugin;

        public UnturnedEventListener(MovementModifierPlugin plugin)
        {
            m_Plugin = plugin;
        }

        public Task HandleEventAsync(object sender, UnturnedPlayerConnectedEvent @event)
        {
            async UniTask UpdatePlayer()
            {
                await UniTask.Delay(3000);

                await UniTask.SwitchToMainThread();

                m_Plugin.UpdatePlayer(@event.Player.Player);
            }

            UpdatePlayer().Forget();

            return Task.CompletedTask;
        }

        public Task HandleEventAsync(object sender, UnturnedPlayerItemEquippedEvent @event)
        {
            m_Plugin.UpdatePlayer(@event.Player.Player);

            return Task.CompletedTask;
        }

        public Task HandleEventAsync(object sender, UnturnedPlayerItemUnequippedEvent @event)
        {
            m_Plugin.UpdatePlayer(@event.Player.Player);

            return Task.CompletedTask;
        }

        public Task HandleEventAsync(object sender, UnturnedPlayerInventoryUpdatedEvent @event)
        {
            m_Plugin.UpdatePlayer(@event.Player.Player);

            return Task.CompletedTask;
        }
    }
}
