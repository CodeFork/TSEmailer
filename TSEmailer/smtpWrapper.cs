using System;
using System.Net;
using System.Data;
using System.Net.Mail;
using System.Collections.Generic;
using TShockAPI;
using TShockAPI.DB;

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

        public void Dispose()
        {
            tseClient.Dispose();
        }

        public void SendEmail(MailAddress from, MailAddressCollection to, MailAddressCollection cc, MailAddressCollection bcc, string subj, string body)
        {
            MailMessage msg = new MailMessage();
            foreach(MailAddress item in to)
            {
                msg.To.Add(item);
            }
            foreach (MailAddress item in cc)
            {
                msg.CC.Add(item);
            }
            foreach (MailAddress item in bcc)
            {
                msg.Bcc.Add(item);
            }
            msg.From = from;
            msg.Sender = from;
            Log.Info("Preparing message FROM " + from.DisplayName + " at address " + from.Address);
            msg.Subject = subj;
            msg.Body = body;
            Log.Info("Sending email message...");
            try
            {
                tseClient.Send(msg);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string SendEmail(string from, string to, string subject, string body)
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

