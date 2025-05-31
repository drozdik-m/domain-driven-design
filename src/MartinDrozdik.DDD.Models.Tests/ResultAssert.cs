using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CSharpFunctionalExtensions;
using Xunit.Sdk;

namespace MartinDrozdik.DDD.Models.Tests;

internal static class ResultAssert
{
    public static void IsSuccess<E>(this UnitResult<E> result)
    {
        Assert.True(result.IsSuccess, $"Expected {nameof(UnitResult<E>)} to be successful, but it was not.");
    }

    public static void IsSuccess<T, E>(this Result<T, E> result)
    {
        Assert.True(result.IsSuccess, $"Expected {nameof(Result<T, E>)} result to be successful, but it was not.");
    }
}
