# DDD library

README IN PROGRESS

# MartinDrozdik.DDD

[![Build, test and publish nuget package](https://github.com/drozdik-m/domain-driven-design/actions/workflows/dotnet-ci-cd.yml/badge.svg)](https://github.com/drozdik-m/domain-driven-design/actions/workflows/dotnet-ci-cd.yml)
[![NuGet](https://img.shields.io/nuget/v/MartinDrozdik.DDD.Models.svg)](https://www.nuget.org/packages/MartinDrozdik.DDD.Models/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MartinDrozdik.DDD.Models.svg)](https://www.nuget.org/packages/MartinDrozdik.DDD.Models/)

## Result vs Exception

This library does provide support for both Result and Exception handling strategies in Domain-Driven Design (DDD). You can choose the approach that best fits your projects' needs.

Normally, you would use Result types to represent the outcome of business operations that can fail, allowing you to handle errors in a functional way without throwing exceptions. This is particularly useful in scenarios where you want to avoid the overhead of exceptions and prefer to work with explicit success/failure states.

However, applications like APIs usually propagate the error all the way to the top level anyway, where exceptions can be caught and translated into appropriate HTTP responses. In such cases, using exceptions might be more straightforward without tons of boilerplate.
