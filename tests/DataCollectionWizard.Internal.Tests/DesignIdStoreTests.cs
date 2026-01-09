using System.Data;
using DataCollectionWizard.Internal.Services.DesignIds;

namespace DataCollectionWizard.Internal.Tests;

public class DesignIdStoreTests
{
    public class Check_duplicates
    {
        [Fact]
        public void Does_not_have_duplicate_design_ids()
        {
            var iDesignIdStoreInterface = typeof(IDesignIdStore);
            var designIdStores = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => iDesignIdStoreInterface.IsAssignableFrom(p));

            var designIdCache = new Dictionary<Guid, string>();

            foreach (var designIdStoreType in designIdStores)
            {
                if (designIdStoreType.IsInterface)
                {
                    continue;
                }

                var designIds = designIdStoreType.GetProperties().Where(p => p.PropertyType == typeof(Guid)).ToArray();
                var instance = Activator.CreateInstance(designIdStoreType);

                foreach (var designId in designIds)
                {
                    var designIdString = $"{designIdStoreType.Name} {designId.Name}";
                    var value = (Guid)designId.GetValue(instance)!;

                    if (designIdCache.TryGetValue(value, out var existingDesignIdString))
                    {
                        throw new DuplicateNameException($"{existingDesignIdString} already uses design id {value} ({designIdString})");
                    }

                    designIdCache.Add(value, designIdString);
                }
            }
        }
    }
}
