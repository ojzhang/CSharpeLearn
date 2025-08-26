using System;
using System.ComponentModel.DataAnnotations;

namespace TodoList.Core.Models
{
    /// <summary>
    /// 文件信息实体类
    /// 表示与待办事项关联的文件信息
    /// </summary>
    public class TodoItemFile
    {
        /// <summary>
        /// 关联的待办事项ID
        /// </summary>
        [Required, Key]
        public Guid TodoId { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        [MaxLength(500)]
        public string Path { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long Size { get; set; }
    }
}