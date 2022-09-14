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
    using JetBrains.Annotations;
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
                var message = await this.CreateMessage(email);

                await this.SendMessage(email, message);

                return new SendResponse { MessageId = message.Id, };
            }
            catch (Exception ex)
            {
                return new SendResponse { ErrorMessages = new List<string> { ex.Message }, };
            }
        }

        private static IList<Recipient> CreateRecipientList(IEnumerable<Address> addressList)
        {
            if (addressList == null)
            {
                return new List<Recipient>();
            }

            return addressList.Select(ConvertToRecipient)
                .ToList();
        }

        private static Recipient ConvertToRecipient([NotNull] Address address)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            return new Recipient
            {
                EmailAddress = new EmailAddress { Address = address.EmailAddress, Name = address.Name, },
            };
        }

        private static void SetPriority(IFluentEmail email, Message draftMessage)
        {
            switch (email.Data.Priority)
            {
                case Priority.High:
                    draftMessage.Importance = Importance.High;

                    break;

                case Priority.Normal:
                    draftMessage.Importance = Importance.Normal;

                    break;

                case Priority.Low:
                    draftMessage.Importance = Importance.Low;

                    break;

                default:
                    draftMessage.Importance = Importance.Normal;

                    break;
            }
        }

        private static byte[] GetAttachmentBytes(Stream stream)
        {
            using var m = new MemoryStream();
            stream.CopyTo(m);

            return m.ToArray();
        }

        private async Task<Message> CreateMessage(IFluentEmail email)
        {
            var templateBody = new ItemBody
            {
                Content = email.Data.Body, ContentType = email.Data.IsHtml ? BodyType.Html : BodyType.Text,
            };

            var template = new Message
            {
                Subject = email.Data.Subject,
                Body = templateBody,
                From = ConvertToRecipient(email.Data.FromAddress),
                ReplyTo = CreateRecipientList(email.Data.ReplyToAddresses),
                ToRecipients = CreateRecipientList(email.Data.ToAddresses),
                CcRecipients = CreateRecipientList(email.Data.CcAddresses),
                BccRecipients = CreateRecipientList(email.Data.BccAddresses),
            };

            var draftMessage = await this.graphClient.Users[email.Data.FromAddress.EmailAddress]
                .MailFolders.Drafts.Messages.Request()
                .AddAsync(template);

            if (email.Data.Attachments is { Count: > 0 })
            {
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
            }

            SetPriority(email, draftMessage);

            return draftMessage;
        }

        private async Task UploadAttachmentUnder3Mb(IFluentEmail email, Message draft, Core.Models.Attachment a)
        {
            var attachment = new FileAttachment
            {
                Name = a.Filename, ContentType = a.ContentType, ContentBytes = GetAttachmentBytes(a.Data),
            };

            await this.graphClient.Users[email.Data.FromAddress.EmailAddress]
                .Messages[draft.Id]
                .Attachments.Request()
                .AddAsync(attachment);
        }

        private async Task UploadAttachment3MbOrOver(IFluentEmail email, Message draft, Core.Models.Attachment attachment)
        {
            var attachmentItem = new AttachmentItem
            {
                AttachmentType = AttachmentType.File, Name = attachment.Filename, Size = attachment.Data.Length,
            };

            var uploadSession = await this.graphClient.Users[email.Data.FromAddress.EmailAddress]
                .Messages[draft.Id]
                .Attachments.CreateUploadSession(attachmentItem)
                .Request()
                .PostAsync();

            var fileUploadTask = new LargeFileUploadTask<FileAttachment>(
                uploadSession,
                attachment.Data);

            await fileUploadTask.UploadAsync();
        }

        private async Task SendMessage(IFluentEmail email, Message message)
        {
            await this.graphClient.Users[email.Data.FromAddress.EmailAddress]
                .Messages[message.Id]
                .Send()
                .Request()
                .PostAsync();
        }
    }
}
