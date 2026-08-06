using FluentValidation;
using MartinDrozdik.DDD.Enumerations;
using MartinDrozdik.DDD.Enumerations.Errors;
using MartinDrozdik.DDD.Enumerations.Validations;

namespace MartinDrozdik.DDD.Tests.Enumerations.Validations;

public class EnumerationRuleBuilderExtensionsTests
{
    private enum TestState
    {
        One,
        Two,
        Missing,
    }

    [Fact]
    public void Should_accept_a_struct_enum_member_with_a_counterpart()
    {
        // Arrange
        var model = new TestModel { State = TestState.One };

        // Act
        var result = new TestValidator().Validate(model);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_reject_a_struct_enum_member_without_a_counterpart()
    {
        // Arrange
        var model = new TestModel { State = TestState.Missing };

        // Act
        var result = new TestValidator().Validate(model);

        // Assert
        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(TestModel.State), failure.PropertyName);
        Assert.Equal(EnumerationErrorCodes.EnumerationNameNotFound.Key, failure.ErrorCode);
        Assert.Contains(nameof(TestEnum), failure.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_reject_an_undefined_struct_enum_value()
    {
        // Arrange
        var model = new TestModel { State = (TestState)99 };

        // Act
        var result = new TestValidator().Validate(model);

        // Assert
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Should_accept_a_null_optional_struct_enum_member()
    {
        // Arrange
        var model = new TestModel { State = TestState.One, OptionalState = null };

        // Act
        var result = new TestValidator().Validate(model);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_reject_an_optional_struct_enum_member_without_a_counterpart()
    {
        // Arrange
        var model = new TestModel { State = TestState.One, OptionalState = TestState.Missing };

        // Act
        var result = new TestValidator().Validate(model);

        // Assert
        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(TestModel.OptionalState), failure.PropertyName);
    }

    private class TestModel
    {
        public TestState State { get; init; }

        public TestState? OptionalState { get; init; }
    }

    private class TestValidator : AbstractValidator<TestModel>
    {
        public TestValidator()
        {
            RuleFor(x => x.State).MustMapToEnumeration(EnumerationMap.To<TestEnum>());
            RuleFor(x => x.OptionalState).MustMapToEnumeration(EnumerationMap.To<TestEnum>());
        }
    }

    private class TestEnum(EnumerationName name) : StaticEnumeration<TestEnum>(name)
    {
        public static readonly TestEnum One = new(nameof(One));
        public static readonly TestEnum Two = new(nameof(Two));
    }
}
