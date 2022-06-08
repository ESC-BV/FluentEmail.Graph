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
    "SaveSentItems": true
  }
}
```

## Release

Create new release with creation of new tag on main branch.

Start [publish](https://github.com/ESC-BV/FluentEmail.Graph/actions/workflows/publish.yml) manually, for the new tag. This will push the package to github and nuget.org

## Origin

Code originally written by [Matt Goldman](https://github.com/matt-goldman) and [merged](https://github.com/lukencode/FluentEmail/pull/218) into FluentEmail repo. But it was not published to NuGet until January 2021. Because we needed this implementation we created a separate repo, modified the code a bit and published it to NuGet.
