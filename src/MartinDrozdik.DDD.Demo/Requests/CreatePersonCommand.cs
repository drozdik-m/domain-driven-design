using MartinDrozdik.DDD.Demo.Models.Entities;
using MartinDrozdik.DDD.Models.Mediator.Commands;

namespace MartinDrozdik.DDD.Demo.Requests;

public record CreatePersonCommand(string FullName, DateTimeOffset DateOfBirth) : ICommand<PersonId>;

public class CreatePersonModel
{
    public string FullName { get; set; }

    public DateTimeOffset DateOfBirth { get; set; }
}
