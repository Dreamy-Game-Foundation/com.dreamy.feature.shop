using System;
using System.Collections.Generic;
using Dreamy.Economy;

namespace Dreamy.Shop.UI
{
    public sealed class ShopResourceDestinationMap
    {
        private readonly Dictionary<ResourceId, string> destinations;
        public ShopResourceDestinationMap(IReadOnlyDictionary<ResourceId, string> destinations)
        {
            if (destinations == null) throw new ArgumentNullException(nameof(destinations));
            this.destinations = new Dictionary<ResourceId, string>();
            foreach (var pair in destinations)
            {
                if (!pair.Key.IsValid || string.IsNullOrWhiteSpace(pair.Value)) throw new ArgumentException("Invalid destination.");
                this.destinations.Add(pair.Key, pair.Value);
            }
        }
        public bool TryGet(ResourceId resource, out string destination) => destinations.TryGetValue(resource, out destination);
    }
}
