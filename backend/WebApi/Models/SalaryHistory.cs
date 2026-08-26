using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApi.Models
{
    public class SalaryHistory
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HourlyRate { get; set; }

        public DateTime EffectiveFrom { get; set; }
    }
}
