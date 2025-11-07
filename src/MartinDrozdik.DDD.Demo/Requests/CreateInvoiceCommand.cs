using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Models.Mediator.Commands;
using static MartinDrozdik.DDD.Demo.Requests.CreateInvoiceCommand;

namespace MartinDrozdik.DDD.Demo.Requests;

public record CreateInvoiceCommand(Model model) : ICommand<PersonId>
{
    public class Model
    {
        public required string FullName { get; init; }

        public required DateTimeOffset DateOfBirth { get; init; }
    }
}
