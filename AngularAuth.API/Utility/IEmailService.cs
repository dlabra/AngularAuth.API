using AngularAuth.API.Models;

namespace AngularAuth.API.Utility
{
    public interface IEmailService
    {
        void SendEmail(EmailModel emailModel);
    }
}
