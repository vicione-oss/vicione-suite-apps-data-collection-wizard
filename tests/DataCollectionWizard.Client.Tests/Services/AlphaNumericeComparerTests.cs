using DataCollectionWizard.Client.Services;

namespace DataCollectionWizard.Client.Tests.Services;

public class AlphaNumericeComparerTests
{
    [Theory]
    [InlineData("Test1", "Test2")]
    [InlineData("Test2", "Test11")]
    [InlineData("a", "B")]
    [InlineData("A132", "B99")]
    [InlineData("A1", "A9")]
    [InlineData("A>1", "A1")]
    [InlineData(".Test", "Test")]
    [InlineData("AL1302", "IOLM 3 moneo cloud")]
    [InlineData("IOLM 1 mit Moneo cloud", "IOLM 3 moneo cloud")]
    [InlineData("IOLM 1 mit Moneo cloud", "VSE100 - VSE100-1 1 00179311")]
    [InlineData("IOLM 1 mit Moneo cloud.AL1352", "VSE100 - VSE100-1 1 00179311")]
    [InlineData("IOLM 3 mit Moneo cloud.AL1352", "VSE100 - VSE100-1 1 00179311")]
    [InlineData("1", "A")]
    [InlineData("A7", "A007")]
    [InlineData("A007", "A8")]
    [InlineData("A.B", "AB")]
    [InlineData("A-1", "A.1")]
    [InlineData("Port", "Ports")]
    [InlineData("Port1", "Ports")]
    public void DoesSortCorrectly(string in1, string in2)
    {
        var result = AlphanumericComparer.Default.Compare(in1, in2);

        Assert.True(result < 0);
    }

    [Theory]
    [InlineData("Port 12", "Port 12")]
    [InlineData("port", "PORT")]
    public void TreatsAsEqual(string in1, string in2)
        => Assert.Equal(0, AlphanumericComparer.Default.Compare(in1, in2));

    // Skipping a separator does not make the strings the same length: when everything else ties, the longer one,
    // separators included, still sorts last.
    [Theory]
    [InlineData("A.1", "A1", 1)]
    [InlineData("A-2", "A1", 1)]
    [InlineData("A_B", "AC", -1)]
    public void IgnoresSeparatorsWhenAsked(string in1, string in2, int expectedSign)
    {
        var result = new AlphanumericComparer(ignoreSeparators: true).Compare(in1, in2);

        Assert.Equal(expectedSign, Math.Sign(result));
    }
}
