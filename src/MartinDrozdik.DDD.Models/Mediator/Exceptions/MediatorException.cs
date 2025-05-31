using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MartinDrozdik.DDD.Models.Mediator.Exceptions;

internal class MediatorException : Exception
{
    /// <inheritdoc />
    public MediatorException()
    {
    }

    /// <inheritdoc />
    public MediatorException(string? message)
        : base(message)
    {
    }

    /// <inheritdoc />
    public MediatorException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
