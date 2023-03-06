namespace FluentEmail.Graph
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Identity;
    using FluentEmail.Core;
    using FluentEmail.Core.Interfaces;
    using FluentEmail.Core.Models;
    using Microsoft.Graph;

    /// <summary>
    /// Implementation of <c>ISender</c> for the Microsoft Graph API.
    /// See <see cref="FluentEmailServicesBuilderExtensions"/>.
    /// </summary>
    public class GraphSender : ISender
    {
        private const int ThreeMbLimit = 3145728;

        private readonly GraphServiceClient graphClient;

        public GraphSender(GraphSenderOptions options)
        {
            ClientSecretCredential spn = new (
                options.TenantId,
                options.ClientId,
                options.Secret);
            this.graphClient = new (spn);
        }

        public GraphSender(GraphServiceClient graphClient)
        {
            this.graphClient = graphClient;
        }

        public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
        {
            return this.SendAsync(email, token)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null)
        {
            try
            {
                if (email.Data.Attachments?.Any() == true)
                {
                    var draftMessage = await this.SendWithAttachments(email);

                    return new SendResponse { MessageId = draftMessage.Id, };
                }

                var message = await this.SendWithoutAttachments(email);

                return new SendResponse { MessageId = message.Id, };
            }
            catch (Exception ex)
            {
                return new SendResponse { ErrorMessages = new List<string> { ex.Message }, };
            }
        }

        private async Task<Message> SendWithoutAttachments(IFluentEmail email)
        {
            // https://docs.microsoft.com/en-us/graph/api/user-sendmail?view=graph-rest-1.0&tabs=http
            var message = MessageCreation.CreateGraphMessageFromFluentEmail(email);
            await this.graphClient.Users[email.Data.FromAddress.EmailAddress]
                .SendMail(message)
                .Request()
                .PostAsync();

            return message;
        }

        private async Task<Message> SendWithAttachments(IFluentEmail email)
        {
            // https://docs.microsoft.com/en-us/graph/api/user-post-messages?view=graph-rest-1.0&tabs=csharp
            var draftMessage = await this.CreateDraftMessage(email);
            await this.graphClient.Users[email.Data.FromAddress.EmailAddress]
                .Messages[draftMessage.Id]
                .Send()
                .Request()
                .PostAsync();

            return draftMessage;
        }

        private static byte[] GetAttachmentBytes(Stream stream)
        {
            using var m = new MemoryStream();
            stream.CopyTo(m);

            return m.ToArray();
        }

        private async Task<Message> CreateDraftMessage(IFluentEmail email)
        {
            var template = MessageCreation.CreateGraphMessageFromFluentEmail(email);

            var request = this.graphClient.Users[email.Data.FromAddress.EmailAddress]
                .Messages.Request();

            var draftMessage = await request.AddAsync(template);
            foreach (var attachment in email.Data.Attachments)
            {
                if (attachment.Data.Length < ThreeMbLimit)
                {
                    await this.UploadAttachmentUnder3Mb(
                        email,
                        draftMessage,
                        attachment);
                }
                else
                {
                    await this.UploadAttachment3MbOrOver(
                        email,
                        draftMessage,
                        attachment);
                }
            }

            return draftMessage;
        }

        private async Task UploadAttachmentUnder3Mb(IFluentEmail email, Message draft, Core.Models.Attachment a)
        {
            var attachment = new FileAttachment
            {
                Name = a.Filename,
                ContentType = a.ContentType,
                ContentBytes = GetAttachmentBytes(a.Data),
                ContentId = a.ContentId,
                IsInline = a.IsInline,

                // can never be bigger than 3MB, so it is safe to cast to int
                Size = (int)a.Data.Length,
            };

            await this.graphClient.Users[email.Data.FromAddress.EmailAddress]
                .Messages[draft.Id]
                .Attachments.Request()
                .AddAsync(attachment);
        }

        private async Task UploadAttachment3MbOrOver(
            IFluentEmail email,
            Message draft,
            Core.Models.Attachment attachment)
        {
            var attachmentItem = new AttachmentItem
            {
                AttachmentType = AttachmentType.File,
                Name = attachment.Filename,
                Size = attachment.Data.Length,
                ContentType = attachment.ContentType,
                ContentId = attachment.ContentId,
                IsInline = attachment.IsInline,
            };

            var uploadSession = await this.graphClient.Users[email.Data.FromAddress.EmailAddress]
                .Messages[draft.Id]
                .Attachments.CreateUploadSession(attachmentItem)
                .Request()
                .PostAsync();

            var fileUploadTask = new LargeFileUploadTask<FileAttachment>(uploadSession, attachment.Data);

            await fileUploadTask.UploadAsync();
        }
    }
}
