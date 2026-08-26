using MediatR;
using System;

namespace WebApi.Commands
{
    public class CreateTimeEntryCommand : IRequest<string>
    {
        public string EmployeeId { get; set; }
        public string ProjectId { get; set; }
        public DateTime Date { get; set; }
        public decimal Hours { get; set; }
        public string Comment { get; set; }
        public string CreatedBy { get; set; }
    }
}
