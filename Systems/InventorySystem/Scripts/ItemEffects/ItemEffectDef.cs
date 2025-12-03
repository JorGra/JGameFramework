using System;
using Newtonsoft.Json;
using JG.GameContent;

namespace JG.Inventory
{
    /// <summary>
    /// Base class for data-defined item effects. Concrete subclasses carry typed parameters
    /// and construct the runtime <see cref="IItemEffect"/>.
    /// </summary>
    [JsonConverter(typeof(DiscriminatorConverter<ItemEffectDef>))]
    [Serializable]
    public abstract class ItemEffectDef
    {
        /// <summary>
        /// Public discriminator value (defaults to the type name). This is the ONLY id used for (de)serialization.
        /// </summary>
        public virtual string TypeId => GetType().Name;

        /// <summary>
        /// Build the runtime effect instance from this definition.
        /// </summary>
        public abstract IItemEffect BuildEffect();
    }
}
