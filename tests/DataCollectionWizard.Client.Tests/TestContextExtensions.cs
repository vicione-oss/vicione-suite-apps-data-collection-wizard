using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Client.Tests;

public static class TestContextExtensions
{
    public static ClusterBuilder SetupVseClusterBuilder(this BunitContext ctx, string assemblyPath,
        Guid instanceId, Action<IDependencyResolver>? resolver = null)
    {
        var resolverService = Substitute.For<IDependencyResolver>();
        ctx.Services.AddSingleton(resolverService);
        resolver?.Invoke(resolverService);

        var functionBlockDesigns = TestFunctionBlockDesignLoader.LoadFunctionBlockDesigns(assemblyPath)
            .ToDictionary(d => d.Id, d => d);

        var builder = new ClusterBuilder(resolverService);

        resolverService.ResolveDataPortDesignDependency(Arg.Any<string>())
            .Returns(new ClusterDependency
            {
                Name = "Dataport",
                Version = "0.0.1"
            });

        foreach (var pair in functionBlockDesigns)
        {
            resolverService
                .ResolveFunctionBlockDesign(pair.Key)
                .Returns(pair.Value);

            resolverService
                .ResolveFunctionBlockDesignDependency(pair.Value.Id)
                .Returns(new ClusterDependency
                {
                    Name = "IoT.Core",
                    Version = "0.0.1"
                });

            builder.Editors.FunctionBlockDesign.AddFunctionBlockDesign(pair.Key);
        }

        // prerequisite
        var nodeGroup = builder.Editors.Cluster.AddNodeGroup("TestNodeGroup");
        var node = builder.Editors.NodeGroup.AddNode(nodeGroup, "TestNode");
        builder.Editors.Node.AddApplication(node, ClusterApplicationType.CoreOsStandalone, "TestApp", instanceId);

        return builder;
    }
}
