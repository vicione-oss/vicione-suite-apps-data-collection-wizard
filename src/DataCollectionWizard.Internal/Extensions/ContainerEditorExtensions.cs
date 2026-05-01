using System.Drawing;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Internal.Extensions;

public static class ContainerEditorExtensions
{
    private const string NotGiven = "not given";

    public static ChildContainer AddSubContainer(this ContainerEditor containerEditor, Dataflow dataflow, string name, Container? parent, int x, int verticalSeparation)
    {
        parent ??= dataflow.Root;
        var y = GetLowestChildY(parent, x);

        if (string.IsNullOrWhiteSpace(name))
            name = NotGiven;

        return containerEditor.AddContainer(parent, name, null, new Point(x, y + verticalSeparation));
    }

    public static FunctionBlock AddSubFunctionBlock(this ContainerEditor containerEditor, Dataflow dataflow, Guid designId, string name, Container? parent, int x, int verticalSeparation)
    {
        parent ??= dataflow.Root;
        var y = GetLowestChildY(parent, x);

        if (string.IsNullOrWhiteSpace(name))
            name = NotGiven;

        return AddFunctionBlock(containerEditor, dataflow, designId, name, parent, new Point(x, y + verticalSeparation));
    }

    public static FunctionBlock AddFunctionBlock(this ContainerEditor containerEditor, Dataflow dataflow, Guid designId, string name, Container? container = null, Point? location = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = NotGiven;
        }

        return containerEditor.AddFunctionBlock(container is null ? dataflow.Root : container, designId, name, null, location);
    }

    private static int GetLowestChildY(Container parent, int x)
    {
        var allChildrensLocations = parent.Containers.Select(c => new Point(c.X ?? 0, c.Y ?? 0));
        allChildrensLocations = allChildrensLocations.Union(parent.FunctionBlocks.Select(f => new Point(f.X ?? 0, f.Y ?? 0)));

        return allChildrensLocations.Where(l => l.X == x && l.Y >= 0).OrderBy(l => l.Y).LastOrDefault().Y;
    }
}
