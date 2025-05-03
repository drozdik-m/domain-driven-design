# Copilot instructions

## General rules

- Use file-scoped namespaces

## Tests

This project uses for testing:

- Newest **XUnit**
- `Assert.That` syntax
- Test cases with name serving as test description where each word is separated by "_", like: `Equals_sucesfully_returns_true_for_equal_parameters`
- Test cases with 3 parts, where each part is commented as:
    - Arrange
    - Act
    - Assert
- Test classes end with postfix `*Tests*`
