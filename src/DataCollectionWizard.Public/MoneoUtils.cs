
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DataCollectionWizard.Public.Extensions;
using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Public;

public static class MoneoUtils
{
    public static string GenerateDataSourceId(string nodeId)
        => CreateSHA512Hex(nodeId);

    private static string CleanMacAddress(string macAddress)
        => macAddress
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal)
            .Replace(":", "", StringComparison.Ordinal);

    public static Guid ConstructDeviceId(string macAddress, string fallBackIdentifier)
    {
        var cleanMacAddress = CleanMacAddress(macAddress);

        if (cleanMacAddress.Length != 12)
        {
            var fallBackHash = CreateSHA512Hex(fallBackIdentifier);
            var l = fallBackHash.Length;

            var secondBlock = fallBackHash.Substring(l - 24, 4);
            var thirdBlock = fallBackHash.Substring(l - 20, 4);
            var fourthBlock = fallBackHash.Substring(l - 16, 4);
            var fifthBlock = fallBackHash.Substring(l - 12, 12);

            return Guid.Parse($"00000001-{secondBlock}-{thirdBlock}-{fourthBlock}-{fifthBlock}");
        }

        return Guid.Parse($"00000000-0000-0000-0000-{cleanMacAddress}");
    }

    private static string CreateSHA512Hex(string nodeId)
    {
        var bytes = Encoding.UTF8.GetBytes(nodeId);
        var hex = string.Empty;
        var hashValue = SHA512.HashData(bytes);

        foreach (var x in hashValue)
            hex += string.Format(CultureInfo.InvariantCulture, "{0:x2}", x);

        return hex;
    }

    public static string GetFallbackIdentifier(IDeviceTreeMasterNode deviceTreeMaster)
        => GetFallbackIdentifier(deviceTreeMaster.GetMacAddress(), null, deviceTreeMaster.SerialNumber);

    public static string GetFallbackIdentifier(string macAddress, string? manufacturerId, string? serialNumber)
    {
        if (string.IsNullOrEmpty(serialNumber) || string.IsNullOrEmpty(manufacturerId))
        {
            return macAddress;
        }

        return manufacturerId + serialNumber;
    }
}
