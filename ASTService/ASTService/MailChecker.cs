using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using OpenPop.Pop3;

namespace ASTService
{
    class MailChecker : OpenPop.Pop3.Pop3Client
    {
        #region Properties
        public bool UseSSL
        {
            get { return useSSL; }
            set { useSSL = value; }
        }
        bool useSSL = false;
        public string MailServer
        {
            get { return mailServer; }
            set { mailServer = value; }
        }
        string mailServer { get; set; }
        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }
        string userName { get; set; }
        public string Password
        {
            get { return password; }
            set { password = value; }
        }
        string password { get; set; }
        public int Port
        {
            get { return port; }
            set { port = value; }
        }
        int port { get; set; }
        Configuration Settings;
        #endregion

        public MailChecker(Configuration config) : base()
        {
            Settings = config;
        }

        string checkFrom;
        string checkSubject;
        int EmailLifetime = 0;

        public void Connect()
        {
            MailServer = Settings.AppSettings.Settings["POP3Server"]?.Value;
            Port = Settings.AppSettings.Settings["POP3Port"] != null ? Int32.Parse(Settings.AppSettings.Settings["POP3Port"].Value) : 995;
            UseSSL = Settings.AppSettings.Settings["POP3UseSSL"] != null ? Boolean.Parse(Settings.AppSettings.Settings["POP3UseSSL"]?.Value) : true;
            UserName = Settings.AppSettings.Settings["MailUserName"]?.Value;
            Password = Settings.AppSettings.Settings["MailPassword"]?.Value;
            checkFrom = Settings.AppSettings.Settings["CheckFrom"]?.Value;
            checkSubject = Settings.AppSettings.Settings["CheckSubject"]?.Value;
            EmailLifetime = Settings.AppSettings.Settings["EmailLifetime"] != null ? Int32.Parse(Settings.AppSettings.Settings["EmailLifetime"].Value) : 0;

            base.Connect(MailServer, Port, UseSSL);
            if (Connected)
            {
                Authenticate(UserName, Password);
            }
            

        }

        public static int DEBUG_Email_Limit = 0;

        public List<EMessage> GetMessages()
        {

            if (!Connected)
                Connect();

            List<EMessage> list = new List<EMessage>();
            var mesCount = GetMessageCount();
            if (mesCount > 0)
            {
                var mInfoes = GetMessageInfos();
                
                for (int i = mesCount; i > 0; i--)
                {
                    var mHeaders = GetMessageHeaders(i);
                    if (mHeaders.MessageId == null)
                    {
                        continue;
                    }
                    if ( (string.IsNullOrWhiteSpace(checkFrom) || mHeaders.From.Address.Trim().ToUpper() == checkFrom.ToUpper()) &&
                        (string.IsNullOrWhiteSpace(checkSubject) || mHeaders.Subject.Trim().ToUpper() == checkSubject.ToUpper()) &&
                        (EmailLifetime == 0 || mHeaders.DateSent>= DateTime.Today.AddDays(-EmailLifetime)))
                    {
                        if (CheckForExist(mHeaders.MessageId.Trim()))
                        {
                            break;
                        }
                        if (EmailLifetime > 0 && mHeaders.DateSent < DateTime.Today.AddDays(-EmailLifetime))
                        {
                            break;
                        }
                        if (list.Any(p => p.Message.Headers.MessageId == mHeaders.MessageId))
                        {
                            continue;
                        }
                        var mes = GetMessage(i);
                        var message = new EMessage()
                        {
                            Headers = mHeaders,
                            Subject = mHeaders.Subject,
                            From = new EFrom() { Name = mHeaders.From.DisplayName, Address = mHeaders.From.Address },
                            Date = mHeaders.DateSent,
                            Customer = Customers.Insight,
                            //Html = Encoding.UTF8.GetString(GetMessageAsBytes(i)),
                            Message = mes
                        };
                        var htmlPart = mes.MessagePart.MessageParts.FirstOrDefault(p => p.ContentType.MediaType.Contains("text/html"));
                        if (htmlPart !=null)
                        {
                            message.Html = htmlPart.BodyEncoding.GetString(htmlPart.Body);
                        }
                        else
                            message.Html = Encoding.UTF8.GetString(GetMessageAsBytes(i));

                        list.Insert(0, message);
                        if (DEBUG_Email_Limit != 0 && list.Count == DEBUG_Email_Limit)
                        {
                            break;
                        }
                    }
                }
            }
            Disconnect();
            return list;
        }
        bool CheckForExist(string messageID)
        {
            var conString = Settings.ConnectionStrings.ConnectionStrings["Default"].ConnectionString;
            var conn = new SqlConnection(conString);

            conn.Open();
            if (conn.State == ConnectionState.Open)
            {
                using (SqlCommand comm = new SqlCommand(string.Format("select [MessageID] from [UploadLog] where [MessageID]='{0}'",messageID), conn))
                {
                    var id = comm.ExecuteScalar();
                    conn.Close();
                    return (id != null);
                }
            }
            return true;
        }
    }

    public class EMessage
    {
        public OpenPop.Mime.Header.MessageHeader Headers { get; set; }
        public string Subject { get; set; }
        public EFrom From { get; set; }
        public DateTime? Date { get; set; }
        public string Text { get; set; }
        public string Html { get; set; }
        public Customers Customer { get; set; }
        public OpenPop.Mime.Message Message { get; set; }
        public int Id { get; set; }
        public bool Converted { get; set; }
        public bool Uploaded { get; set; }
    }

    public class EFrom
    {
        public string Address { get; set; }
        public string Name { get; set; }
    }
}