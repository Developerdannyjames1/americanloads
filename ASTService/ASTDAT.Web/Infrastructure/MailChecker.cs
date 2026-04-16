using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using OpenPop.Pop3;
using ASTDAT.Tools;

namespace ASTDAT.Web.Infrastructure
{
    class MailChecker : OpenPop.Pop3.Pop3Client
    {
        public bool UseSSL { get; set; }
        public string MailServer { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }

        public MailChecker() : base()
        {
        }

        string checkFrom;
        string checkSubject;
        int EmailLifetime = 0;

        public void Connect()
        {
            MailServer = ConfigurationManager.AppSettings["POP3Server"];
            Port = ConfigurationManager.AppSettings["POP3Port"] != null ? Int32.Parse(ConfigurationManager.AppSettings["POP3Port"]) : 995;
            UseSSL = ConfigurationManager.AppSettings["POP3UseSSL"] != null ? Boolean.Parse(ConfigurationManager.AppSettings["POP3UseSSL"]) : true;
            UserName = ConfigurationManager.AppSettings["MailUserName"];
            Password = ConfigurationManager.AppSettings["MailPassword"];
            checkFrom = ConfigurationManager.AppSettings["CheckFrom"];
            checkSubject = ConfigurationManager.AppSettings["CheckSubject"];
            EmailLifetime = ConfigurationManager.AppSettings["EmailLifetime"] != null ? Int32.Parse(ConfigurationManager.AppSettings["EmailLifetime"]) : 0;

            base.Connect(MailServer, Port, UseSSL);
            if (Connected)
            {
                Authenticate(UserName, Password);
            }
        }

        public List<EMessage> GetMessages()
        {
            if (!Connected)
            {
                Connect();
            }

            List<EMessage> list = new List<EMessage>();
            var mesCount = GetMessageCount();
            Logger.Write($"GetMessages.mesCount:{mesCount}");
            if (mesCount > 0)
            {
                var mInfoes = GetMessageInfos();

                for (int i = mesCount; i > 0; i--)
                {
                    try
                    {
                        if (i % 20 == 0)
                        {
                            Logger.Write($"GetMessages.mesCount:{i}/{mesCount}");
                        }
                        var mHeaders = GetMessageHeaders(i);
                        if (mHeaders.MessageId == null)
                        {
                            continue;
                        }
                        if ((string.IsNullOrWhiteSpace(checkFrom) || mHeaders.From.Address.Trim().ToUpper() == checkFrom.ToUpper()) &&
                            (string.IsNullOrWhiteSpace(checkSubject) || mHeaders.Subject.Trim().ToUpper() == checkSubject.ToUpper()) &&
                            (EmailLifetime == 0 || mHeaders.DateSent >= DateTime.Today.AddDays(-EmailLifetime)))
                        {
                            if (CheckForExist(mHeaders.MessageId.Trim()))
                            {
                                break;
                                //continue;
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
                            if (htmlPart != null)
                            {
                                message.Html = htmlPart.BodyEncoding.GetString(htmlPart.Body);
                            }
                            else
                                message.Html = Encoding.UTF8.GetString(GetMessageAsBytes(i));

                            list.Insert(0, message);
                            //!!!! DEBUG
                            //if (list.Count == 20) break;
                            //if (message.Html.ToLower().IndexOf("power only") > 0) break;
                            //!!!! DEBUG
                        }
                    }
                    catch(Exception exc)
                    {
                        Logger.Write("GetMessages", exc);
                    }
                }
            }

            Logger.Write($"GetMessages.Done {list.Count}");
            Disconnect();
            return list;
        }

        bool CheckForExist(string messageID)
        {
            var conString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
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

    public enum Customers
    {
        Unknown,
        Insight
    }

    public enum EventLogType
    {
        Start,
        //MailError,
        ParseError,
        UploadError,
        SQLError,
        SystemError,
        FinishConvert,
        FinishUpload
    }

    public class FinalModel
    {
        public string Load { get; set; }
        public int isConverted { get; set; }
        public int isUploaded { get; set; }
        public string FileName { get; set; }
        public string ConvertError { get; set; }
    }
}