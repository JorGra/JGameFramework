namespace JG.Samples
{
    public class StatModifierPickup : Pickup
    {
        public StatModifierConfig StatModifierConfig;

        public override void ApplyPickupEffect(Entity entity)
        {
            entity.Stats.Mediator.AddModifier(UnityServiceLocator.ServiceLocator.For(this).Get<IStatModifierFactory>().Create(StatModifierConfig));
        }
    }
}