using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Internal.Services.CloudDataflowGenerators;

public sealed class CloudInput
{
    public ConnectorInput? InputConnector { get; set; }
    public DataPortTreeNode? InputTreeNode { get; set; }

    public void Connect(ConnectorOutput output, ClusterBuilder builder, bool visible = false)
    {
        if (InputTreeNode is not null)
        {
            builder.Editors.DataPortTreeNode.AssignConnector(InputTreeNode, output);
        }

        if (InputConnector is not null)
        {
            if (!builder.Editors.Connector.CanCreateLink(output, InputConnector, visible))
            {
                return;
            }

            builder.Editors.Connector.AddLink(output, InputConnector, visible);
        }
    }

    public bool IsConfigured()
        => InputTreeNode is not null || InputConnector is not null;
}
