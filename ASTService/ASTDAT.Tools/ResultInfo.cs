using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Tools
{
    public class ResultInfo
    {
        public bool IsSuccess { get; set; }
        public bool IsTS { get; set; }
        public bool IsDAT { get; set; }
        public string Message { get; set; }
        public int Code { get; set; }

        public string Source
        {
            get
            {
                if (IsDAT && IsTS)
                {
                    return "DAT+TS";
                }
                if (IsDAT)
                {
                    return "DAT";
                }
                if (IsTS)
                {
                    return "TS";
                }
                return "Unknown";
            }
        }

        public ResultInfo()
        {
        }

        public ResultInfo(bool isSuccess, bool isTS, bool isDAT, string message, int code = 0)
        {
            IsSuccess = isSuccess;
            IsTS = isTS;
            IsDAT = isDAT;
            Message = message;
            Code = code;
        }

        public static ResultInfo Error(string message, int code = 0)
        {
            return new ResultInfo(false, false, false, message, code);
        }

        public static ResultInfo Success(string message, int code = 0)
        {
            return new ResultInfo(true, false, false, message, code);
        }

        public static ResultInfo DATError(string message, int code = 0)
        {
            return new ResultInfo(false, false, true, message, code);
        }

        public static ResultInfo DATSuccess(string message, int code = 0)
        {
            return new ResultInfo(true, false, true, message, code);
        }

        public static ResultInfo TSError(string message, int code = 0)
        {
            return new ResultInfo(false, true, false, message, code);
        }

        public static ResultInfo TSSuccess(string message, int code = 0)
        {
            return new ResultInfo(true, true, false, message, code);
        }
    }
}
