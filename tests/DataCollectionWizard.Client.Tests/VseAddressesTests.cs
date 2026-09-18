namespace DataCollectionWizard.Client.Tests;

public class VseAddressesTests
{
    public class GetVsePortFromUri
    {
        [Theory]
        [InlineData(1234, 1234)]
        [InlineData(null, 3321)]
        public void ReturnsCorrectPort(int? port, int expected)
        {
            var uri = port.HasValue ? new UriBuilder("http", "test.com", port.Value).Uri : new Uri("http://www.test.com/");

            Assert.Equal(expected, VseAddresses.GetVsePortFromUri(uri));
        }
    }
}
