# DDD library



## Result vs Exception

This library does provide support for both Result and Exception handling strategies in Domain-Driven Design (DDD). You can choose the approach that best fits your projects' needs.

Normally, you would use Result types to represent the outcome of business operations that can fail, allowing you to handle errors in a functional way without throwing exceptions. This is particularly useful in scenarios where you want to avoid the overhead of exceptions and prefer to work with explicit success/failure states.

However, applications like APIs usually propagate the error all the way to the top level anyway, where exceptions can be caught and translated into appropriate HTTP responses. In such cases, using exceptions might be more straightforward without tons of boilerplate.
