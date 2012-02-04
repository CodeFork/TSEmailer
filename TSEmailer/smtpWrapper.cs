using System;
using System.Net;
using System.Data;
using System.Net.Mail;
using System.Collections.Generic;
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

        public void SendEmail(MailAddress from, List<MailAddress> to, List<MailAddress> cc, List<MailAddress> bcc, string subj, string body)
        {
            MailMessage msg = new MailMessage();
            foreach(MailAddress add in to)
            {
                msg.To.Add(add);
            }
            foreach (MailAddress add in cc)
            {
                msg.CC.Add(add);
            }
            foreach (MailAddress add in bcc)
            {
                msg.Bcc.Add(add);
            }
            msg.From = from;
            msg.Sender = from;
            msg.Subject = subj;
            msg.Body = body;
            try
            {
                tseClient.Send(msg);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void SendEmail(List<SqlValue> from, DataTable bcc, string subj, string body)
        {
            MailMessage msg = new MailMessage();
            //Load player names and email addresses into a usable list
            foreach (DataRow row in bcc.Rows)
            {
                msg.Bcc.Add(new MailAddress(row.ItemArray[1].ToString(), row.ItemArray[0].ToString()));
            }
            msg.From = new MailAddress(from[1].ToString(),from[0].ToString());
            msg.Sender = new MailAddress(from[1].ToString(), from[0].ToString());
            msg.Subject = subj;
            msg.Body = body;
            try
            {
                tseClient.Send(msg);
            }
            catch (Exception)
            {
                throw;
            }
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

