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
    public void DoesSortCorrectly(string in1, string in2)
    {
        var result = AlphanumericComparer.Default.Compare(in1, in2);

        Assert.True(result < 0);
    }
}
