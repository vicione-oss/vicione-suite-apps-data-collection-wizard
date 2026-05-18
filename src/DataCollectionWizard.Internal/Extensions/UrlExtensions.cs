namespace DataCollectionWizard.Internal.Extensions;

public static class UrlExtensions
{
    public const ushort VseDefaultPort = 3321;

    public static string GetVseAddress(this Uri uri)
        => $"{uri.DnsSafeHost}:{GetVsePort(uri)}";

    private static int GetVsePort(this Uri uri)
        => uri.IsDefaultPort
            ? VseDefaultPort
            : uri.Port;

    public static Uri SetVsePort(this Uri uri)
    {
        if (uri.IsDefaultPort)
        {
            UriBuilder uriBuilder = new(uri)
            {
                Port = 3321
            };
            uri = uriBuilder.Uri;
        }

        return uri;
    }
}
