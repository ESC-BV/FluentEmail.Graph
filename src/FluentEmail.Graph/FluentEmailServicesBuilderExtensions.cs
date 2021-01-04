namespace FluentEmail.Graph
{
    using FluentEmail.Core.Interfaces;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

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
            string graphEmailAppId,
            string graphEmailTenantId,
            string graphEmailSecret,
            bool saveSentItems = false)
        {
            var options = new GraphSenderOptions
            {
                AppId = graphEmailAppId,
                TenantId = graphEmailTenantId,
                Secret = graphEmailSecret,
                SaveSentItems = saveSentItems,
            };
            return builder.AddGraphSender(options);
        }
    }
}
