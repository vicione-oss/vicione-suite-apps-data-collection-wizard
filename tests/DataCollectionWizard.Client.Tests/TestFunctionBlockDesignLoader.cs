using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Generation;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Engine.DefaultPoolings;
using ViciOne.Engine.DefaultValidators;

namespace DataCollectionWizard.Client.Tests;

public static class TestFunctionBlockDesignLoader
{
    private static List<FunctionBlockDesign>? s_functionBlockDesigns;

    private static List<FunctionBlockDesign> LoadFunctionBlockDesigns(Component component)
    {
        AssemblyLoadContext? loadContext = default;
        try
        {
            loadContext = UnloadableAssemblyLoadContext.Create(component.AssemblyLocation);

            var poolingFactory = new PoolingFactory(loadContext.Assemblies.Append(typeof(SumPooling).Assembly));

            var functionBlockDesignInfos = RuntimeFunctionBlockAssemblyAnalyzer
                .GetFunctionBlockDesigns(loadContext.Assemblies)
                .ToList();

            return [.. FunctionBlockDesignGenerator.GenerateFunctionBlockDesigns(functionBlockDesignInfos, poolingFactory)];
        }
        catch (Exception)
        {
            return [];
        }
        finally
        {
            loadContext?.Unload();
        }
    }

    public static List<FunctionBlockDesign> LoadFunctionBlockDesigns(string assemblyPath)
    {
        // the designs are static information and can be shared between tests
        if (s_functionBlockDesigns is not null)
            return s_functionBlockDesigns;

        var components = UnloadableAssemblyLoadContext.GetComponents(assemblyPath);
        s_functionBlockDesigns = [.. components.SelectMany(LoadFunctionBlockDesigns)];

        return s_functionBlockDesigns;
    }

    internal sealed class UnloadableAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly Stack<nint> _unmanagedAssemblies = new();

        private UnloadableAssemblyLoadContext(string component, IList<Assembly> shared) : base(isCollectible: true)
        {
            Unloading += _ =>
            {
                while (_unmanagedAssemblies.Count > 0)
                    NativeLibrary.Free(_unmanagedAssemblies.Pop());
            };

            _resolver = new(component);
            shared.Add(Assembly.GetExecutingAssembly());
        }

        internal static UnloadableAssemblyLoadContext Create(string assemblyLocation)
        {
            var shared = new List<Assembly>
            {
                typeof(FunctionBlockDesignAttribute).Assembly, // Runtime
                typeof(IConnectorDesign).Assembly, // Contracts
                typeof(StringLengthValidator).Assembly,  // ViciOne.Engine.DefaultValidators
            };

            var context = new UnloadableAssemblyLoadContext(assemblyLocation, shared);
            context.Load(assemblyLocation);
            return context;
        }

        internal static IEnumerable<Component> GetComponents(string directory)
        {
            var loadableComponents = Directory.GetFiles(directory, "*.deps.json", SearchOption.AllDirectories);
            return loadableComponents.Select(Component);

            Component Component(string loadableComponent)
            {
                var assembly = loadableComponent.Replace(".deps.json", ".dll", StringComparison.Ordinal);
                var pathParts = loadableComponent
                    .Replace(directory, "", StringComparison.Ordinal)
                    .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var version = string.Empty;
                var packageName = string.Empty;
                if (pathParts.Length >= 3)
                {
                    packageName = pathParts[0];
                    version = pathParts[1];
                }

                if (!Path.IsPathRooted(assembly))
                    assembly = Path.GetFullPath(assembly);

                return new Component(assembly, packageName, version);
            }
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // already loaded in default context? then let it be resolved there (e.g. netstandard)
            if (Default.Assemblies.Any(a => a.GetName().Name == assemblyName.Name))
                return null;

            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            return path is not null ? LoadFromAssemblyPath(path) : null;
        }

        private void Load(string filename)
        {
            using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            LoadFromStream(stream);
        }

        protected override nint LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (path is not null)
            {
                var pointer = LoadUnmanagedDllFromPath(path);
                _unmanagedAssemblies.Push(pointer);
                return pointer;
            }

            return nint.Zero;
        }
    }

    internal sealed record Component(string AssemblyLocation, string PackageName, string Version);
}
