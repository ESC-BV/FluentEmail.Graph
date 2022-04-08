using System;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Graph;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SendMailSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddFluentEmail("user@user.com").AddGraphSender(Initial());
            services.AddScoped<IFluentEmail, Email>();
            var provider = services.BuildServiceProvider();

            //get mail service
            var eamil = provider.GetRequiredService<IFluentEmail>();
            var sender = provider.GetRequiredService<ISender>();
            eamil.Sender = sender;


            eamil.SetFrom("user@user.com", "testGuy")
                    .To("user@hotmail.com")
                    .Subject("Test contact Email ")
                    .Body(GetTestMailBody());
            eamil.Data.IsHtml = true;
            var response = eamil.Send();
            Console.ReadKey();

        }

        private static GraphSenderOptions Initial()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)

                .Build();
            var graphSenderOptions = config
                .GetSection("GraphSenderOptions")
                .Get<GraphSenderOptions>();

            return graphSenderOptions;
        }

        private static string GetTestMailBody()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title></title>
</head>
<body>
Test mail ok<br/>
<b>Bold text</b>
</body>
</html>";
        }
    }
}
