# FluentEmail.Graph

Sender for [FluentEmail](https://github.com/lukencode/FluentEmail) that
uses [Microsoft Graph API](https://docs.microsoft.com/en-us/graph/api/resources/mail-api-overview?view=graph-rest-1.0).

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
    "TenantId": "your tenant id",
    "ClientId": "your app id",
    "Secret": "your secret here"
  }
}
```

## v2

The original version only used
the [user: sendMail](https://docs.microsoft.com/en-us/graph/api/user-sendmail?view=graph-rest-1.0&tabs=http) Graph API
endpoint. This could not handle attachments over 3MB.

Starting with v2, if you have included any attachments, the implementation will switch to the following:

- A [draft message](https://docs.microsoft.com/en-us/graph/api/user-post-messages?view=graph-rest-1.0&tabs=http) is
  created
- Attachments are added
  using [attachment: createUploadSession](https://docs.microsoft.com/en-us/graph/api/attachment-createuploadsession?view=graph-rest-1.0&tabs=http)
- The mail is sent
  using [message: send](https://docs.microsoft.com/en-us/graph/api/message-send?view=graph-rest-1.0&tabs=http).

⚠️BREAKING: The `Mail.ReadWrite` permission is required when adding attachments.

[Microsoft Docs on using the Graph API to send large attachments](https://docs.microsoft.com/en-us/graph/outlook-large-attachments?tabs=csharp)

⚠️REMOVED: Unfortunately, the Microsoft Graph API `Send` method does not have a `SaveSentItems` argument like
the `SendMail` method
that was previously used. The `SaveSentItems` option has been removed and there is no way to disable this anymore. (
See [Link 1](https://docs.microsoft.com/en-us/answers/questions/337574/graph-sdk-i-want-to-send-the-saved-draft-mail-but.html)
, [Link 2](https://docs.microsoft.com/en-us/graph/api/message-send?view=graph-rest-1.0&tabs=http),
and [Link 3](https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/743)).

Uploading attachments to a draft message was contributed by [@huntmj01](https://github.com/huntmj01).

## Graph API Permissions

The `Mail.Send` permission must be granted.

Adding attachments? Then the `Mail.ReadWrite` permissions is also required.

## Release

Create new release with creation of new tag on main branch.

Start [publish](https://github.com/ESC-BV/FluentEmail.Graph/actions/workflows/publish.yml) manually, for the new tag.
This will push the package to github and nuget.org

## Origin

Code originally written by [Matt Goldman](https://github.com/matt-goldman)
and [merged](https://github.com/lukencode/FluentEmail/pull/218) into FluentEmail repo. But it was not published to NuGet
until January 2021. Because we needed this implementation we created a separate repo, modified the code a bit and
published it to NuGet.
