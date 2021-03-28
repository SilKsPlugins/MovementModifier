using Rocket.API;
using System.Collections.Generic;
using System.Linq;
using MovementModifier;
using MovementModifier.Configuration;
using Rocket.Unturned.Chat;
using SDG.Unturned;

namespace MovementModifierCommands.Commands
{
    public class CommandMM : IRocketCommand
    {
        public string Name => "mm";

        public string Help => "Manage the Movement Modifier configuration.";

        public string Syntax => "";

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public List<string> Aliases => new List<string>();
        
        public List<string> Permissions => new List<string>()
        {
            "mm"
        };

        private void Print(IRocketPlayer player, string key, params object[] parameters) => UnturnedChat.Say(player,
            MovementModifierCommandsPlugin.Instance.Translate(key, parameters));

        private ItemAsset GetAsset(string id) => MovementModifierPlugin.GetAsset(id);

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = MovementModifierPlugin.Instance;
            
            if (plugin == null)
            {
                Print(caller, "mm_not_loaded");
                return;
            }

            if (command.Length == 0 || (command.Length == 1 && command[0].ToLower() == "help"))
            {
                Print(caller, "mm_help_help");
                Print(caller, "mm_help_reload");
                Print(caller, "mm_help_delete");
                Print(caller, "mm_help_modifier");
                Print(caller, "mm_help_modifiers");
                return;
            }

            if (command.Length == 1 && command[0].ToLower() == "reload")
            {
                foreach (var player in Provider.clients.Where(x => x?.player != null))
                {
                    plugin.UpdatePlayerMultipliers(player.player);
                }

                Print(caller, "mm_reloaded");
                return;
            }


            if (command.Length == 2 && command[1].ToLower() == "delete")
            {
                ItemAsset asset = GetAsset(command[0]);

                if (asset == null)
                {
                    Print(caller, "mm_not_found", command[0]);
                    return;
                }

                ItemModifier modifier = plugin.ActiveModifiers.FirstOrDefault(x => x.Asset.id == asset.id);

                if (modifier == null)
                {
                    Print(caller, "mm_not_found", command[0]);
                    return;
                }

                plugin.ActiveModifiers.Remove(modifier);

                Print(caller, "mm_deleted", command[0]);

                return;
            }

            if (command.Length == 3)
            {
                ItemAsset asset = GetAsset(command[0]);

                if (asset == null)
                {
                    Print(caller, "mm_not_found", command[0]);
                    return;
                }

                ItemModifier modifier = plugin.ActiveModifiers.FirstOrDefault(x => x.Asset.id == asset.id) ?? new ItemModifier
                {
                    Asset = asset
                };

                bool defaultValue = command[2] == "~";

                // ReSharper disable StringLiteralTypo

                switch (command[1].ToLower())
                {
                    case "speed":
                        float speed = 1;
                        if (!defaultValue && !float.TryParse(command[2], out speed))
                        {
                            Print(caller, "invalid_value");
                            return;
                        }
                        modifier.Speed = speed;
                        break;

                    case "jump":
                        float jump = 1;
                        if (!defaultValue && !float.TryParse(command[2], out jump))
                        {
                            Print(caller, "invalid_value");
                            return;
                        }
                        modifier.Jump = jump;
                        break;

                    case "gravity":
                        float gravity = 1;
                        if (!defaultValue && !float.TryParse(command[2], out gravity))
                        {
                            Print(caller, "invalid_value");
                            return;
                        }
                        modifier.Gravity = gravity;
                        break;

                    case "staminacost":
                        float staminaCost = 1;
                        if (!defaultValue && !float.TryParse(command[2], out staminaCost))
                        {
                            Print(caller, "invalid_value");
                            return;
                        }
                        modifier.StaminaCost = staminaCost;
                        break;

                    case "mustbeequipped":
                        bool mustBeEquipped = false;
                        if (!defaultValue && !bool.TryParse(command[2], out mustBeEquipped))
                        {
                            Print(caller, "invalid_value");
                            return;
                        }
                        modifier.MustBeEquipped = defaultValue ? null : (bool?)mustBeEquipped;
                        break;
                    default:
                        Print(caller, "invalid_modifier");
                        return;
                }

                // ReSharper restore StringLiteralTypo

                if (!plugin.ActiveModifiers.Contains(modifier))
                {
                    plugin.ActiveModifiers.Add(modifier);
                }

                Print(caller, "mm_modifier_success", asset.itemName, command[1], command[2]);
                return;
            }

            Print(caller, "invalid_parameters");
        }
    }
}
