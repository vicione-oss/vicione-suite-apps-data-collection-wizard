namespace DataCollectionWizard.Internal.Commands;

// Username/Password are carried so the device-tree fetch engine can reach an authenticating master (the fetch
// runs before the credentials are ever written onto the resulting node, so they must travel with the request).
public record DeviceEngineInfo(Uri Address, string Type, string? Username = null, string? Password = null);
