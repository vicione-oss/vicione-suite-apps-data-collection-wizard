namespace DataCollectionWizard.Client;

// VSE devices are reached on port 3321; an address given without an explicit port is normalized to it.
internal static class VseAddresses
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
