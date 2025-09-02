using NodaTime;
using NodaTime.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TodoList.Core;

namespace TodoList.Core.Models
{
    /// <summary>
    /// 待办事项实体类
    /// 表示一个待办事项，包含标题、内容、完成状态、添加时间、截止时间等信息
    /// </summary>
    public class TodoItem
    {
        /// <summary>
        /// 待办事项唯一标识符
        /// </summary>
        [Required, Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 用户ID，关联到ApplicationUser实体
        /// </summary>
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; }

        /// <summary>
        /// 待办事项标题
        /// </summary>
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public string Title { get; set; }

        /// <summary>
        /// 待办事项内容
        /// </summary>
        [MaxLength(200)]
        [MinLength(15)]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; }

        /// <summary>
        /// 待办事项完成状态
        /// </summary>
        public bool Done { get; set; }

        /// <summary>
        /// 待办事项添加时间
        /// 使用NodaTime的Instant类型处理时间
        /// 此属性不直接映射到数据库，通过AddedDateTime属性进行序列化
        /// </summary>
        [NotMapped]
        public Instant Added { get; set; }

        /// <summary>
        /// 待办事项截止时间
        /// 使用NodaTime的Instant类型处理时间
        /// 此属性不直接映射到数据库，通过DuetoDateTime属性进行序列化
        /// </summary>
        [NotMapped]
        public Instant DueTo { get; set; }

        /// <summary>
        /// 关联的文件信息
        /// </summary>
        public TodoItemFile File { get; set; }

        /// <summary>
        /// 用于EF序列化目的的添加时间属性
        /// 该属性已标记为过时，仅用于Entity Framework序列化，不应用于业务逻辑
        /// 映射到数据库中的"Added"列，类型为可空DateTime
        /// </summary>
        [Obsolete("Property only used for EF-serialization purposes")]
        [DataType(DataType.DateTime)]
        [Column("Added")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DateTime? AddedDateTime
        {
            get => Added == NodaTime.Instant.MinValue ? (DateTime?)null : Added.ToDateTimeUtc();
            set => Added = value == null ? NodaTime.Instant.MinValue : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).ToInstant();
        }

        /// <summary>
        /// 用于EF序列化目的的截止时间属性
        /// 该属性已标记为过时，仅用于Entity Framework序列化，不应用于业务逻辑
        /// 映射到数据库中的"DueTo"列，类型为可空DateTime
        /// </summary>
        [Obsolete("Property only used for EF-serialization purposes")]
        [DataType(DataType.DateTime)]
        [Column("DueTo")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DateTime? DuetoDateTime
        {
            get => DueTo == NodaTime.Instant.MinValue ? (DateTime?)null : DueTo.ToDateTimeUtc();
            set => DueTo = value == null ? NodaTime.Instant.MinValue : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc).ToInstant();
        }

        /// <summary>
        /// 待办事项关联的标签集合
        /// </summary>
        [Column("Tags")]
        [MaxLength(Constants.MAX_TAGS)]
        public IEnumerable<string> Tags { get; set; }
    }
}