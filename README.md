# Set of Domain-Driven Design (DDD) libraries for .NET

This repository contains a set of libraries that provide Nugets for Domain-Driven Design (DDD) in .NET.

## MartinDrozdik.DDD

**A pragmatic .NET library for Domain-Driven Design that doesn't force you into abstract nonsense (that much).**

Contains basic interfaces and building blocks for DDD, such as:

- ValueObject, Entity, AggregateRoot, Enumerations...
- Validation and error handling utilities
- Type-safe ID patterns
- Mediator for commands and queries (with handlers) – integrated via DI
  - And pipelines!
- Other goodies that make DDD easier without forcing you into a specific architecture or framework

Check out [README.md](./src/MartinDrozdik.DDD/README.md) for this library and possibly the [demo app](../MartinDrozdik.DDD.Demo) for recommended usage.

[![DDD Logo](./src/MartinDrozdik.DDD/ddd-icon.png)](./src/MartinDrozdik.DDD/README.md)

## MartinDrozdik.DDD.Web

*TODO*

## MartinDrozdik.DDD.Demo

A demo application that shows recommended patterns for using the MartinDrozdik.DDD library. It's not gospel, but it works. Check out: