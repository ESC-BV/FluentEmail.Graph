// See https://aka.ms/new-console-template for more information

using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Graph;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SendMailTestApp;

using System.Reflection;

class Program
{
    static async Task Main(string[] args)
    {
        var configurationRoot = GetConfigurationRoot(args);
        var graphSenderOptions = configurationRoot.GetRequiredSection("GraphSenderOptions")
            .Get<GraphSenderOptions>();
        var fromAddress = configurationRoot.GetValue<string>("MailOptions:FromAddress");

        var services = new ServiceCollection();
        services.AddScoped<IFluentEmail, Email>();
        services.AddFluentEmail(fromAddress)
            .AddGraphSender(graphSenderOptions);
        var serviceProvider = services.BuildServiceProvider();

        // get mail service and out Graph sender
        var email = serviceProvider.GetRequiredService<IFluentEmail>();
        var sender = serviceProvider.GetRequiredService<ISender>();
        if (sender is not GraphSender)
        {
            Console.WriteLine("GraphSender not resolved.");

            return;
        }

        Console.WriteLine("Enter destination e-mail address to send test mail to:");
        var destinationAddress = Console.ReadLine();
        email.To(destinationAddress)
            .Subject("Test Email Graph API")
            .Body("This is the <b>body</b> of the mail.");
        email.Data.IsHtml = true;

        var response = await sender.SendAsync(email);
        if (response.Successful)
        {
            Console.WriteLine("Mail sent.");
        }
        else
        {
            Console.WriteLine("Mail was NOT sent.");
            foreach (var errorMessage in response.ErrorMessages)
            {
                Console.WriteLine("- " + errorMessage);
            }
        }

        Console.ReadKey();
    }

    private static IConfigurationRoot GetConfigurationRoot(string[] args)
    {
        var config = new ConfigurationBuilder();
        config.AddJsonFile("appsettings.json", optional: true);

        var appAssembly = Assembly.Load(new AssemblyName("SendMailTestApp"));
        config.AddUserSecrets(appAssembly, optional: false);

        config.AddEnvironmentVariables();

        if (args is { Length: > 0 })
        {
            config.AddCommandLine(args);
        }

        var configurationRoot = config.Build();

        return configurationRoot;
    }
}
