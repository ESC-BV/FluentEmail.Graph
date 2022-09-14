// See https://aka.ms/new-console-template for more information

using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Graph;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(
        (hostContext, services) =>
        {
            var graphSenderOptions = hostContext.Configuration.GetRequiredSection("GraphSenderOptions")
                .Get<GraphSenderOptions>();
            var fromAddress = hostContext.Configuration.GetValue<string>("MailOptions:FromAddress");

            services.AddScoped<IFluentEmail, Email>();
            services.AddFluentEmail(fromAddress)
                .AddGraphSender(graphSenderOptions);
        });

using var host = builder.Build();

host.
// get mail service and out Graph sender
var email = host.Services.GetService<IFluentEmail>();
var sender = host.Services.GetService<ISender>();
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

await host.RunAsync();
