namespace JG.Samples
{
    public class StatModifierPickup : Pickup
    {
        public StatModifierConfig StatModifierConfig;
        StatModifierFactory statModifierFactory = new StatModifierFactory();
        public override void ApplyPickupEffect(Entity entity)
        {
            entity.Stats.Mediator.AddModifier(statModifierFactory.Create(StatModifierConfig));
        }
    }
}