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
        private const int ThreeMb = 3145728;
        private readonly GraphServiceClient graphClient;

        public GraphSender(GraphSenderOptions options)
        {
            ClientSecretCredential spn = new(options.TenantId, options.ClientId, options.Secret);
            this.graphClient = new(spn);
        }

        public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
        {
            return this.SendAsync(email, token).GetAwaiter().GetResult();
        }

        public async Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null)
        {
            try
            {
                var message = await this.CreateMessage(email);

                await this.SendMessage(email, message);

                return new SendResponse
                {
                    MessageId = message.Id,
                };
            }
            catch (Exception ex)
            {
                return new SendResponse
                {
                    ErrorMessages = new List<string> { ex.Message },
                };
            }
        }

        private async Task<Message> CreateMessage(IFluentEmail email)
        {
            var templateBody = new ItemBody
            {
                Content = email.Data.Body,
                ContentType = email.Data.IsHtml ? BodyType.Html : BodyType.Text,
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

            Message draft = await this.graphClient
                .Users[email.Data.FromAddress.EmailAddress]
                .MailFolders
                .Drafts
                .Messages
                .Request()
                .AddAsync(template);

            if (email.Data.Attachments != null && email.Data.Attachments.Count > 0)
            {
                foreach (var a in email.Data.Attachments)
                {
                    if (a.Data.Length < ThreeMb)
                    {
                        await this.UploadAttachmentUnder3Mb(email, draft, a);
                    }
                    else
                    {
                        await this.UploadAttachement3MbOrOver(email, draft, a);
                    }
                }
            }

            switch (email.Data.Priority)
            {
                case Priority.High:
                    draft.Importance = Importance.High;
                    break;

                case Priority.Normal:
                    draft.Importance = Importance.Normal;
                    break;

                case Priority.Low:
                    draft.Importance = Importance.Low;
                    break;

                default:
                    draft.Importance = Importance.Normal;
                    break;
            }

            return draft;
        }

        private static IList<Recipient> CreateRecipientList(IEnumerable<Address> addressList)
        {
            if (addressList == null)
            {
                return new List<Recipient>();
            }

            return addressList
                .Select(ConvertToRecipient)
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
                EmailAddress = new EmailAddress
                {
                    Address = address.EmailAddress,
                    Name = address.Name,
                },
            };
        }

        private async Task UploadAttachmentUnder3Mb(IFluentEmail email, Message draft, Core.Models.Attachment a)
        {
            var attachment = new FileAttachment
            {
                Name = a.Filename,
                ContentType = a.ContentType,
                ContentBytes = GetAttachmentBytes(a.Data),
            };

            await this.graphClient
                .Users[email.Data.FromAddress.EmailAddress]
                .Messages[draft.Id]
                .Attachments
                .Request()
                .AddAsync(attachment);
        }

        private static byte[] GetAttachmentBytes(Stream stream)
        {
            using var m = new MemoryStream();
            stream.CopyTo(m);
            return m.ToArray();
        }

        private async Task UploadAttachement3MbOrOver(IFluentEmail email, Message draft, Core.Models.Attachment a)
        {
            var attachmentItem = new AttachmentItem
            {
                AttachmentType = AttachmentType.File,
                Name = a.Filename,
                Size = a.Data.Length,
            };

            var uploadSession = await this.graphClient
                .Users[email.Data.FromAddress.EmailAddress]
                .Messages[draft.Id]
                .Attachments
                .CreateUploadSession(attachmentItem)
                .Request()
                .PostAsync();

            int maxSliceSize = 320 * 1024;
            var fileUploadTask = new LargeFileUploadTask<FileAttachment>(uploadSession, a.Data, maxSliceSize);

            await fileUploadTask.UploadAsync();
        }

        private async Task SendMessage(IFluentEmail email, Message message)
        {
            await this.graphClient
                .Users[email.Data.FromAddress.EmailAddress]
                .Messages[message.Id]
                .Send()
                .Request()
                .PostAsync();
        }
    }
}
