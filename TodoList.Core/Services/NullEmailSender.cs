using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace TodoList.Core.Services
{
    /// <summary>
    /// 一个空操作的邮件发送器，用于在没有配置实际邮件服务时作为后备方案
    /// </summary>
    public class NullEmailSender : IEmailSender
    {
        /// <summary>
        /// 空操作的邮件发送方法，不实际发送邮件
        /// </summary>
        /// <param name="email">收件人邮箱地址</param>
        /// <param name="subject">邮件主题</param>
        /// <param name="htmlMessage">邮件内容</param>
        /// <returns>完成的任务</returns>
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // 不执行任何操作，仅返回完成的任务
            // 这样即使没有配置邮件服务，应用程序也能正常运行
            return Task.CompletedTask;
        }
    }
}