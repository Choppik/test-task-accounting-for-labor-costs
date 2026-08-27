using MediatR;
using System;

namespace WebApi.Commands
{
    public class UpdateTimeEntryCommand : IRequest<bool>
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }

        public string ProjectId { get; set; }

        public DateTime Date { get; set; }

        public decimal Hours { get; set; }

        public string Comment { get; set; }

        public int Version { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime ModifiedAt { get; set; }
    }
}
