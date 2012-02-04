using System;
using System.Net;
using System.Net.Mail;

namespace smtpWrapper
{
    public class TSEsmtp
    {
        public static SmtpClient tseClient;

        public TSEsmtp(string host, int port)
        {
            tseClient = new SmtpClient(host, port);
        }

        public TSEsmtp(string host, int port, string user, string pass, bool tseTLS)
        {
            tseClient = new SmtpClient(host, port);
            tseClient.Credentials = new NetworkCredential(user, pass);
            tseClient.EnableSsl = tseTLS;
        }

        public string SendEmail(string to, string from, string subject, string body)
        {
            MailMessage msg = new MailMessage(from, to, subject, body);
            try
            {
                tseClient.Send(msg);
                return "Message Sent: " + subject;
            }
            catch (Exception ex)
            {
                return ex.Message;
                //throw;
            }
        }
    }
}

