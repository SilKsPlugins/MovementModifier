using SDG.Unturned;

namespace MovementModifier.Configuration
{
    public class Modifier
    {
        public float Speed { get; set; }

        public float Jump { get; set; }

        public float Gravity { get; set; }

        public float StaminaCost { get; set; }

        public Modifier() : this(1, 1, 1, 1)
        {
        }

        public Modifier(float speed, float jump, float gravity, float staminaCost)
        {
            Speed = speed;
            Jump = jump;
            Gravity = gravity;
            StaminaCost = staminaCost;
        }

        public virtual bool Applies(Player player) => true;

        public virtual bool ModifiesAnything() => Speed != 1 || Jump != 1 || Gravity != 1 || StaminaCost != 1;

        public Modifier Clone() => (Modifier)this.MemberwiseClone();

        public static Modifier operator *(Modifier a, Modifier b) =>
            new Modifier(
                a.Speed * b.Speed,
                a.Jump * b.Jump,
                a.Gravity * b.Gravity,
                a.StaminaCost * b.StaminaCost);
    }
}
