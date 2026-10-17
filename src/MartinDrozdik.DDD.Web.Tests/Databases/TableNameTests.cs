using MartinDrozdik.DDD.Blobs.Models;
using MartinDrozdik.DDD.Templates;
using MartinDrozdik.DDD.Testing;
using MartinDrozdik.DDD.Web.Blobs;
using MartinDrozdik.DDD.Web.Databases;
using MartinDrozdik.DDD.Web.Outbox;
using MartinDrozdik.DDD.Web.Outbox.Models;
using Microsoft.EntityFrameworkCore;

namespace MartinDrozdik.DDD.Web.Tests.Databases;

public class TableNameTests
{
    [Fact]
    public void A_name_alone_is_in_the_default_schema_of_the_provider()
    {
        // Arrange
        // Act
        var table = new TableName("OutboxMessage");

        // Assert
        Assert.Equal("OutboxMessage", table.Name);
        Assert.Null(table.Schema);
        Assert.Equal("OutboxMessage", table.ToString());
    }

    [Fact]
    public void A_name_with_a_schema_is_qualified_by_it()
    {
        // Arrange
        // Act
        var table = new TableName("Outbox", "invoicing");

        // Assert
        Assert.Equal("Outbox", table.Name);
        Assert.Equal("invoicing", table.Schema);
        Assert.Equal("invoicing.Outbox", table.ToString());
    }

    [Fact]
    public void A_string_converts_to_a_name_in_the_default_schema()
    {
        // Arrange
        // Act
        TableName table = "InvoiceOutbox";

        // Assert
        Assert.Equal(new TableName("InvoiceOutbox"), table);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void A_blank_name_throws(string name)
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<ArgumentException>(() => new TableName(name));
        Assert.Throws<ArgumentException>(() => new TableName(name, "invoicing"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void A_blank_schema_throws(string schema)
    {
        // Arrange
        // Act
        // Assert
        Assert.Throws<ArgumentException>(() => new TableName("Outbox", schema));
    }

    [Fact]
    public void Equality_by_value_works_correctly()
    {
        // Arrange
        var table1 = new TableName("Outbox", "invoicing");
        var table2 = new TableName("Outbox", "invoicing");
        var differentSchema = new TableName("Outbox", "shipping");
        var noSchema = new TableName("Outbox");

        // Act
        // Assert
        EqualityAssert.TestEqualityComparer(comparer: table1, table1, table2, differentSchema);
        EqualityAssert.TestEqualityOperators<ValueObject>(table1, table2, differentSchema);
        Assert.NotEqual(table1, noSchema);
    }

    [Fact]
    public void The_outbox_is_mapped_to_the_given_table_and_schema()
    {
        // Arrange
        var modelBuilder = new ModelBuilder();

        // Act
        modelBuilder.AddOutbox(new TableName("Messages", "invoicing"));

        // Assert
        var entityType = modelBuilder.Model.FindEntityType(typeof(OutboxMessage));
        Assert.NotNull(entityType);
        Assert.Equal("Messages", entityType.GetTableName());
        Assert.Equal("invoicing", entityType.GetSchema());
        Assert.Contains(entityType.GetIndexes(), index => index.GetDatabaseName() == "IX_Messages_ProcessedAt_FailedAt_AvailableAt");
    }

    [Fact]
    public void The_blob_catalogue_is_mapped_to_the_default_table_or_the_given_one()
    {
        // Arrange
        var defaultBuilder = new ModelBuilder();
        var customBuilder = new ModelBuilder();

        // Act
        defaultBuilder.AddBlobs();
        customBuilder.AddBlobs(new TableName("Files", "storage"));

        // Assert
        var defaultType = defaultBuilder.Model.FindEntityType(typeof(Blob));
        Assert.NotNull(defaultType);
        Assert.Equal(Web.Blobs.ModelBuilderExtensions.DefaultTableName, defaultType.GetTableName());
        Assert.Null(defaultType.GetSchema());

        var customType = customBuilder.Model.FindEntityType(typeof(Blob));
        Assert.NotNull(customType);
        Assert.Equal("Files", customType.GetTableName());
        Assert.Equal("storage", customType.GetSchema());
    }
}
