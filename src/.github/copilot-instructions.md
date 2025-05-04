# Copilot instructions

## General rules

- Use file-scoped namespaces
- Use `<see cref="X">` in all xml comments if applicable
- Use `nameof` for all strings that are method names, property names, etc.

## Tests

This project uses for testing:

- Newest **XUnit**
- Test cases with name as test description, like: `Equals_sucesfully_returns_true_for_equal_parameters`
    - **Each word** is separated by "_"
    - The description is like a normal sentence
- Test cases with 3 parts, where each part is commented as:
    - Arrange
    - Act
    - Assert
- Test classes end with postfix `*Tests*`
