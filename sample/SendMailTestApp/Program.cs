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
        services.AddScoped<IFluentEmailFactory, FluentEmailFactory>();
        services.AddFluentEmail(fromAddress)
            .AddGraphSender(graphSenderOptions);
        var serviceProvider = services.BuildServiceProvider();

        // get mail service and out Graph sender
        var emailFactory = serviceProvider.GetRequiredService<IFluentEmailFactory>();
        var sender = serviceProvider.GetRequiredService<ISender>();
        if (sender is not GraphSender)
        {
            Console.WriteLine("GraphSender not resolved.");

            return;
        }

        Console.WriteLine("Mail will be sent from: " + fromAddress);
        Console.WriteLine("Enter destination e-mail address:");
        var destinationAddress = Console.ReadLine();

        Console.WriteLine("Add attachments? Type 'Y' for yes.");
        var addAttachment = (Console.ReadLine() ?? string.Empty).Equals("Y", StringComparison.OrdinalIgnoreCase);

        var email = emailFactory.Create();
        email.SetFrom(fromAddress)
            .To(destinationAddress)
            .Subject("Test Email Graph API")
            .Body("This is the <b>body</b> of the mail.");
        email.Data.IsHtml = true;

        if (addAttachment)
        {
            Console.WriteLine(
                "Adding attachments - this will use another Graph API endpoint that needs the Mail.ReadWrite permission.");
            email.AttachFromFilename("TestAttachmentSmall.txt");
            email.AttachFromFilename("TestAttachmentLarge.txt");
        }
        else
        {
            Console.WriteLine(
                "Not adding attachment - this will use default Graph API endpoint that needs the Mail.Send permission.");
        }

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
                Console.WriteLine("ERROR");
                Console.WriteLine(errorMessage);
            }
        }
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
