using Autofac;
using Integration.Sysmond.Service.Services;
using Module = Autofac.Module;

namespace Integration.Sysmond.Service.DependencyResolvers;

/// <summary>Sysmond.Service assembly bağımlılıklarını Autofac ile kaydeder.</summary>
public class AutofacSysmondModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(SysmondSyncService).Assembly)
            .Where(t =>
                (t.Name.EndsWith("Service") || t.Name.EndsWith("Orchestrator") || t.Name.EndsWith("Provider"))
                && !t.IsGenericTypeDefinition
                && !t.IsAbstract)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }
}
