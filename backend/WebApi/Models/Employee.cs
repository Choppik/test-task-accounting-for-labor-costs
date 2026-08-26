using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace WebApi.Models
{
    public class Employee
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public List<SalaryHistory> SalaryHistory { get; set; } = new List<SalaryHistory>();
    }
}
