using Serilog;

namespace Midjourney.Base
{
    /// <summary>
    /// 邮件发送通知
    /// https://help.aliyun.com/document_detail/29451.html?spm=a2c4g.11186623.6.607.383c2649OgIrok
    /// </summary>
    public class EmailJob : SingletonBase<EmailJob>
    {
        public async void EmailSend(SmtpConfig config, string subject, string body, string to = null)
        {
            var mailTo = config.To;
            if (!string.IsNullOrWhiteSpace(to))
            {
                mailTo = to;
            }

            if (string.IsNullOrWhiteSpace(config?.FromPassword) || string.IsNullOrWhiteSpace(mailTo))
            {
                return;
            }

            try
            {
                // SMTP服务器信息
                string smtpServer = config.Host; // "smtp.mxhichina.com"; // 请替换为你的SMTP服务器地址
                int port = config.Port; // SMTP端口，一般为587或465，具体依据你的SMTP服务器而定
                bool enableSsl = config.EnableSsl; // 根据你的SMTP服务器要求设置

                // 邮件账户信息
                string userName = config.FromEmail; // 你的邮箱地址
                string password = config.FromPassword; // 你的邮箱密码或应用专用密码

                string fromName = config.FromName; // 发件人昵称
                string fromEmail = config.FromEmail; // 发件人邮箱地址

                // 调用邮件发送方法
                EmailSender.SendMimeKitEmail(smtpServer, userName, password, fromName, fromEmail, mailTo, subject, body, port, enableSsl);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "发送邮件失败");

                try
                {
                    // 尝试第二次
                    await EmailSender.Instance.SendEmailAsync(config, mailTo, subject, body);
                }
                catch (Exception exx)
                {
                    Log.Error(exx, "第二次发送邮件失败");
                }
            }
        }
    }
}