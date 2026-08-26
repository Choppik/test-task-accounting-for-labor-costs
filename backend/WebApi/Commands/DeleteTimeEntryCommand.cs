using MediatR;

namespace WebApi.Commands
{
    public class DeleteTimeEntryCommand : IRequest<bool>
    {
        public string Id { get; set; }
    }
}
