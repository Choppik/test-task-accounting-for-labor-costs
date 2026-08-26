using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace WebApi.Models
{
    public class ClosedPeriod
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public bool IsClosed { get; set; }

        public string ClosedBy { get; set; }

        public DateTime ClosedAt { get; set; }
    }
}
