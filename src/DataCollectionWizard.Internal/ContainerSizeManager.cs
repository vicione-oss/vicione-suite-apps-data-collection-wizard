using ViciOne.Cluster.Model;

namespace DataCollectionWizard.Internal;

internal sealed class ContainerSizeManager
{
    private Container? _currentContainer;
    private int _elementCount = -1;
    private bool _firstContainerCreated;

    public string? ChildContainerPrefix { get; set; }
    public int ContainerSize { get; set; } = 10;
    public int ItemHeight { get; set; } = 200;

    public event Func<string, Container>? CreateNewContainer;
    public event Action? CreatingFirstContainer;

    public void CreateFirstContainer()
    {
        if (!_firstContainerCreated)
        {
            _firstContainerCreated = true;
            CreatingFirstContainer?.Invoke();
        }
    }

    private string GetContainerName(int elementCount)
    {
        var lowerBound = elementCount / ContainerSize * ContainerSize;
        return $"{ChildContainerPrefix} {lowerBound + 1} - {lowerBound + ContainerSize}";
    }

    public Container GetCurrentContainer()
        => _currentContainer ?? throw new InvalidOperationException("container is not yet initialized");

    public int GetCurrentYPosition()
        => _elementCount % ContainerSize * ItemHeight;

    public void PreAddElement()
    {
        CreateFirstContainer();

        _elementCount++;

        if (_elementCount % ContainerSize == 0)
            _currentContainer = CreateNewContainer?.Invoke(GetContainerName(_elementCount));
    }
}
