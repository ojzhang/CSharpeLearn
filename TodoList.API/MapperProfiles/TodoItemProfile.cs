using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TodoList.API.Models;
using TodoList.Core.Models;

namespace TodoList.API.MapperProfiles
{
    public class TodoItemProfile : Profile
    {
        public TodoItemProfile()
        {
            // Convert comma-separated tags string into IEnumerable<string> using a dedicated converter
            CreateMap<string, IEnumerable<string>>().ConvertUsing<TagStringToEnumerableConverter>();

            CreateMap<TodoItemDto, TodoItem>();
        }
    }
}
