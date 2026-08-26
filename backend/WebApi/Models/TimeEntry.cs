using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace WebApi.Models
{
    public class TimeEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string EmployeeId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ProjectId { get; set; }

        public DateTime Date { get; set; }

        public decimal Hours { get; set; }

        public decimal ExpectedCost { get; set; }

        public string Comment { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public int Version { get; set; } = 1;
    }
}
