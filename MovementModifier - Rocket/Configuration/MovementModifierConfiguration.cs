using Rocket.API;
using System.Xml.Serialization;

namespace MovementModifier.Configuration
{
    public class MovementModifierConfiguration : IRocketPluginConfiguration
    {
        public Multipliers? GlobalMultipliers;

        [XmlArrayItem("Item")]
        public ItemModifier[]? ItemModifiers;

        public void LoadDefaults()
        {
            GlobalMultipliers = new Multipliers()
            {
                Speed = 1,
                Jump = 1,
                Gravity = 1,
                StaminaCost = 1,
            };

            ItemModifiers = new[]
            {
                new ItemModifier() { Id = "Viper", Speed = 2, Jump = 2, Gravity = 2 },
                new ItemModifier() { Id = "Tracksuit Top", Gravity = 0.5f },
                new ItemModifier() { Id = "Ace", StaminaCost = 0, MustBeEquipped = false },
            };
        }
    }
}