namespace DataCollectionWizard.Public;

// Moved here from ViciOne.DeviceTree.Contracts.Constants.VseAddresses, which was removed from the shared
// package as it was no longer used by any production driver code.
public static class VseAddresses
{
    public static int VseDefaultPort { get; } = 3321;

    public static Uri GetVseAddressWithPort(string vseAddress)
    {
        UriBuilder builder = new(vseAddress);
        builder.Port = GetVsePortFromUri(builder.Uri);
        return builder.Uri;
    }

    public static int GetVsePortFromUri(Uri deviceUri)
        => deviceUri.IsDefaultPort
            ? VseDefaultPort
            : deviceUri.Port;
}
