using Rocket.API.Collections;
using Rocket.Core.Plugins;

namespace MovementModifierCommands
{
    public class MovementModifierCommandsPlugin : RocketPlugin
    {
        public static MovementModifierCommandsPlugin Instance { get; private set; }

        protected override void Load()
        {
            Instance = this;
        }

        protected override void Unload()
        {
            Instance = null;
        }

        public override TranslationList DefaultTranslations { get; } = new TranslationList()
        {
            { "mm_help_help", "/mm help - Displays helpful command info." },
            { "mm_help_reload", "/mm reload - Force all players' modifiers to reload." },
            { "mm_help_delete", "/mm <item> delete - Delete the config for the item." },
            { "mm_help_modifier", "/mm <item> <modifier> <value> - Set an item's modifier value (~ for default)" },
            { "mm_help_modifiers", "Available modifiers: Speed, Jump, Gravity, StaminaCost, MustBeEquipped"},

            { "mm_deleted", "Deleted item modifier with id '{0}'."},

            { "mm_modifier_success", "Successfully set '{0}'s {1} modifier to {2}."},

            { "mm_reloaded", "Reloaded all players' modifiers." },

            { "mm_not_loaded", "Movement Modifier's config has not been loaded." },
            { "mm_not_found", "Item modifier with id '{0}' not found."},
            { "invalid_value", "Value specified is invalid for this modifier." },
            { "invalid_modifier", "Modifier specified is invalid." },
            { "invalid_parameters", "Invalid parameters." },
        };
    }
}
