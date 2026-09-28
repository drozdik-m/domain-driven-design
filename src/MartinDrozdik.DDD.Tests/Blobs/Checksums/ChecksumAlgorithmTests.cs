using System.Text;
using MartinDrozdik.DDD.Blobs.Checksums;
using MartinDrozdik.DDD.Testing;

namespace MartinDrozdik.DDD.Tests.Blobs.Checksums;

public class ChecksumAlgorithmTests
{
    [Fact]
    public void FromName_finds_the_name_written_to_storage()
    {
        // Arrange
        // Act
        var result = ChecksumAlgorithm.FromName("sha256");

        // Assert
        // Changing a name would break every checksum already stored under it
        result.IsSuccess();
        Assert.Same(ChecksumAlgorithm.Sha256, result.Value);
    }

    [Fact]
    public void GetAll_returns_every_algorithm()
    {
        // Arrange
        // Act
        var algorithms = ChecksumAlgorithm.GetAll();

        // Assert
        Assert.Equal([ChecksumAlgorithm.Sha256], algorithms);
    }

    [Fact]
    public void CreateHash_produces_a_hash_of_the_declared_length()
    {
        // Arrange
        foreach (var algorithm in ChecksumAlgorithm.GetAll())
        {
            using var hash = algorithm.CreateHash();
            hash.AppendData(Encoding.UTF8.GetBytes("hello"));

            // Act
            var hex = Convert.ToHexStringLower(hash.GetCurrentHash());

            // Assert
            Assert.Equal(algorithm.HashLength, hex.Length);
            BlobChecksum.Create(algorithm, hex).IsSuccess();
        }
    }

    [Fact]
    public void Every_algorithm_fits_the_columns_it_is_stored_in()
    {
        // Arrange
        // Act
        var algorithms = ChecksumAlgorithm.GetAll();

        // Assert
        Assert.All(algorithms, algorithm =>
        {
            Assert.InRange(algorithm.Name.Key.Length, 1, ChecksumAlgorithm.NameMaxLength);
            Assert.InRange(algorithm.HashLength, 1, BlobChecksum.MaxLength);
        });
    }
}
