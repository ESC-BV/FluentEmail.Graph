Set up the config:

```
cd sample
cd SendMailTestApp

dotnet user-secrets init

dotnet user-secrets set "GraphSenderOptions:TenantId" "your value"
dotnet user-secrets set "GraphSenderOptions:ClientId" "your value"
dotnet user-secrets set "GraphSenderOptions:Secret" "your value"

dotnet user-secrets set "MailOptions:FromAddress" "your@value"

```