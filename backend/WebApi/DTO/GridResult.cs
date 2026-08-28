using System.Collections.Generic;

namespace WebApi.DTO
{
    public class GridResult<T>
    {
        public List<T> Rows { get; set; }
        public long TotalRowCount { get; set; }
    }
}
