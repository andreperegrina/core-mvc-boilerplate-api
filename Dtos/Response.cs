using System;

namespace WebApi.Dtos
{
    public class Response
    {
        public string Status { get; set; }
        public object Data { get; set; }

        public Response(Status status, object data)
        {
            Status = status.ToString();
            Data = data;
        }
    }
}