using Bunit;
using DevExpress.Blazor.Internal;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Sdk.Testing.Client;
using TestContext = Bunit.TestContext;

namespace DataCollectionWizard.Client.Tests.Extensions;

public static class TestContextExtensions
{
    public static BunitContext SetupSuiteServicesWithBlazorDx(this BunitContext ctx, Action<ClientServiceConfigurator>? setup = null)
    {
        ctx.SetupSuiteServices(setup);

        var env = Substitute.For<IEnvironmentInfo>();
        env.DeviceInfo.Returns(new DeviceInfo(false));

        ctx.Services.AddScoped(_ => Substitute.For<IEnvironmentInfoFactory>());
        ctx.Services.AddScoped(_ => Substitute.For<ISvgImagesLoader>());
        ctx.Services.AddScoped(_ => env);
        ctx.Services.AddDevExpressBlazor(options => options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5);
        ctx.Services.TryAddComponentRequiredServices();

        ctx.JSInterop.ConfigureJSInterop();
        return ctx;
    }

    /// <summary>
    /// Possible workaround for bUnit integration with DevExpress Blazor controls provided at
    /// https://supportcenter.devexpress.com/ticket/details/t1056787/devexpress-and-bunit-support
    /// </summary>
    public static BunitJSInterop ConfigureJSInterop(this BunitJSInterop interop)
    {
        interop.Mode = JSRuntimeMode.Loose;

        var rootModule = interop.SetupModule("./_content/DevExpress.Blazor/dx-blazor.js");
        rootModule.Mode = JSRuntimeMode.Strict;
        rootModule.Setup<DeviceInfo>("getDeviceInfo", _ => true).SetResult(new DeviceInfo(false));

        return interop;
    }
}
