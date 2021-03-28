namespace MovementModifier
{
    public class Multipliers
    {
        public float Speed = 1;

        public float Jump = 1;

        public float Gravity = 1;

        public float StaminaCost = 1;

        public Multipliers Clone() => (Multipliers)this.MemberwiseClone();
    }
}
