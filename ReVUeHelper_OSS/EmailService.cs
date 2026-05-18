using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ReVUeHelper_OSS
{
    public class EmailService
    {
        private readonly string _addresses;
        private readonly string _emailFrom;            

        public EmailService()
        {
            _emailFrom = "efrunza@amherst.com";            
        }

        public void SendEmail(string message)
        {
            try
            {
                // Configure the SMTP client
                SmtpClient smtpClient = new SmtpClient("dfsmtpout.root.amherst.com");
                //smtpClient.UseDefaultCredentials = false;
                //smtpClient.Credentials = new NetworkCredential("your_email_username", "your_email_password");
                //smtpClient.EnableSsl = false; // Set to true if your mail server requires SSL
                smtpClient.Port = 25; // Change the port if needed

                // Create the email message
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(_emailFrom);
                mailMessage.To.Add("efrunza@amherst.com");
                mailMessage.To.Add("cphan@amherst.com");
                mailMessage.Subject = "WTO Opportunity Status Out Of Sync";
                mailMessage.Body = message;

                // Send the email
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
    }
}


