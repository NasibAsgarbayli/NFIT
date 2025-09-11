using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NFIT.Application.Shared
{
    public class BaseResponse<T>
    {

        public bool Success { get; set; }
        public string? Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public T? Data { get; set; }
      

        public BaseResponse(HttpStatusCode statusCode)
        {
            Success = ((int)statusCode >= 200 && (int)statusCode < 300);
            StatusCode = statusCode;

        }
        public BaseResponse(string message, HttpStatusCode statusCode)
        {
            Message = message;
            Success = ((int)statusCode >= 200 && (int)statusCode < 300);
            StatusCode = statusCode;
        }

        public BaseResponse(string message, bool isSuccess, HttpStatusCode statusCode)
        {
            Message = message;
            Success = isSuccess;
            StatusCode = statusCode;
        }
        public BaseResponse(string message, T? data, HttpStatusCode statusCode)
        {
            Success = ((int)statusCode >= 200 && (int)statusCode < 300);
            Data = data;
            Message = message;
            StatusCode = statusCode;
        }
    }
}
