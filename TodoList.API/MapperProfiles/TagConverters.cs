using AutoMapper;
using System.Collections.Generic;
using System.Linq;

namespace TodoList.API.MapperProfiles
{
    public class TagStringToEnumerableConverter : ITypeConverter<string, IEnumerable<string>>
    {
        public IEnumerable<string> Convert(string source, IEnumerable<string> destination, ResolutionContext context)
        {
            if (string.IsNullOrWhiteSpace(source)) return new List<string>();
            return source.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
        }
    }
}
