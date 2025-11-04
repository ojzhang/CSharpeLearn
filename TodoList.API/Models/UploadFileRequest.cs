using Microsoft.AspNetCore.Http;

namespace TodoList.API.Models
{
    /// <summary>
    /// DTO 用于接收 multipart/form-data 的文件上传请求
    /// </summary>
    public class UploadFileRequest
    {
        public IFormFile File { get; set; }
    }
}
