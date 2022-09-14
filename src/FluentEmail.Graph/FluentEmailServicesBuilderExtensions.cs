namespace FluentEmail.Graph
{
    using FluentEmail.Core.Interfaces;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    /// <summary>
    /// Contains extension methods to register the <see cref="GraphSender"/> with the <c>FluentEmailServicesBuilder</c> from <c>FluentEmail.Core</c>.
    /// </summary>
    public static class FluentEmailServicesBuilderExtensions
    {
        public static FluentEmailServicesBuilder AddGraphSender(
            this FluentEmailServicesBuilder builder,
            GraphSenderOptions options)
        {
            builder.Services.TryAdd(ServiceDescriptor.Scoped<ISender>(_ => new GraphSender(options)));
            return builder;
        }

        public static FluentEmailServicesBuilder AddGraphSender(
            this FluentEmailServicesBuilder builder,
            string graphEmailClientId,
            string graphEmailTenantId,
            string graphEmailSecret)
        {
            var options = new GraphSenderOptions
            {
                ClientId = graphEmailClientId,
                TenantId = graphEmailTenantId,
                Secret = graphEmailSecret,
            };
            return builder.AddGraphSender(options);
        }
    }
}
