# Fork of NatchEurope/FluentEmail.Graph

Fork of [NatchEurope/FluentEmail.Graph](https://github.com/NatchEurope/FluentEmail.Graph) that modifies the `GraphSender` to use an upload session for sending emails with attachments that are 3MB or larger. I was receiving a Microsoft Graph API error when trying to use FluentEmail.Graph to send emails with attachments over 3MB.

[Microsoft Docs on using the Graph API to send large attachments](https://docs.microsoft.com/en-us/graph/outlook-large-attachments?tabs=csharp)

Unfortunately, the Microsoft Graph API `Send` method does not have a `SaveSentItems` argument like the `SendMail` method does, so I had to remove the option to disable saving sent items. See [Link 1](https://docs.microsoft.com/en-us/answers/questions/337574/graph-sdk-i-want-to-send-the-saved-draft-mail-but.html), [Link 2](https://docs.microsoft.com/en-us/graph/api/message-send?view=graph-rest-1.0&tabs=http), and [Link 3](https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/743).

# FluentEmail.Graph

Sender for [FluentEmail](https://github.com/lukencode/FluentEmail) that uses [Microsoft Graph API](https://docs.microsoft.com/en-us/graph/api/resources/mail-api-overview?view=graph-rest-1.0).

![Nuget](https://img.shields.io/nuget/v/FluentEmail.Graph)

![CI](https://github.com/ESC-BV/FluentEmail.Graph/workflows/CI/badge.svg)
![CodeQL](https://github.com/ESC-BV/FluentEmail.Graph/workflows/CodeQL/badge.svg)
![Publish](https://github.com/ESC-BV/FluentEmail.Graph/workflows/Publish/badge.svg)

## Usage

Call one of the `AddGraphSender` extension methods.

```csharp
var graphSenderOptions = this.Configuration
    .GetSection("GraphSenderOptions")
    .Get<GraphSenderOptions>();
services.AddFluentEmail("alias@test.com")
    .AddRazorRenderer()
    .AddGraphSender(graphSenderOptions);
```

Example config in `appsettings.json`

```json
{
  "GraphSenderOptions": {
    "AppId": "your app id",
    "TenantId": "your tenant id",
    "Secret": "your secret here",
  }
}
```

## Release

Create new release with creation of new tag on main branch.

Start [publish](https://github.com/ESC-BV/FluentEmail.Graph/actions/workflows/publish.yml) manually, for the new tag. This will push the package to github and nuget.org

## Origin

Code originally written by [Matt Goldman](https://github.com/matt-goldman) and [merged](https://github.com/lukencode/FluentEmail/pull/218) into FluentEmail repo. But it was not published to NuGet until January 2021. Because we needed this implementation we created a separate repo, modified the code a bit and published it to NuGet.
