using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Autofac;
using Autofac.Core;
using NUnit.Framework.Internal;
using Recyclarr.TestLibrary.Autofac;
using Spectre.Console;

namespace Recyclarr.Cli.IntegrationTests;

[TestFixture]
public class CompositionRootTest
{
    // Warning CA1812 : CompositionRootTest.ConcreteTypeEnumerator is an internal class that is apparently never
    // instantiated.
    [SuppressMessage("Performance", "CA1812", Justification = "Created via reflection by TestCaseSource attribute")]
    private sealed class ConcreteTypeEnumerator : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            var builder = new ContainerBuilder();
            CompositionRoot.Setup(builder);

            // These are things that Spectre.Console normally registers for us, so they won't explicitly be
            // in the CompositionRoot. Register mocks/stubs here.
            builder.RegisterMockFor<IAnsiConsole>();

            var container = builder.Build();
            return container.ComponentRegistry.Registrations
                .SelectMany(x => x.Services)
                .OfType<TypedService>()
                .Select(x => x.ServiceType)
                .Distinct()
                .Where(x => x.FullName == null || !x.FullName.StartsWith("Autofac."))
                .Select(x => new TestCaseParameters(new object[] {container, x}) {TestName = x.FullName})
                .GetEnumerator();
        }
    }

    [TestCaseSource(typeof(ConcreteTypeEnumerator))]
    public void Service_should_be_instantiable(ILifetimeScope scope, Type service)
    {
        scope.Resolve(service).Should().NotBeNull();
    }
}
