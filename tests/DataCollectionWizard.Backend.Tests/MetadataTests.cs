using Sdk.Testing;

namespace DataCollectionWizard.Backend.Tests;

public class MetadataTests
{
    [Fact]
    public void Should_be_serializeable()
        => MetadataValidator.ValidateMetadata();
}
