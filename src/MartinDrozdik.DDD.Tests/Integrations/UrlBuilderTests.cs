using MartinDrozdik.DDD.Integrations;

namespace MartinDrozdik.DDD.Tests.Integrations;

public class UrlBuilderTests
{
    [Fact]
    public void Empty_builder_produces_root_string()
    {
        // Arrange
        var builder = new UrlBuilder();

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal("/", url);
    }

    [Fact]
    public void Empty_relative_builder_produces_empty_string()
    {
        // Arrange
        var builder = new UrlBuilder().AsRelative();

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal(string.Empty, url);
    }

    [Theory]
    [InlineData(new string[] { }, "/")]
    [InlineData(new[] { "api", "users" }, "/api/users")]
    [InlineData(new[] { "api", "users", "details" }, "/api/users/details")]
    [InlineData(new[] { "with spaces", "séğmęnt" }, "/with%20spaces/s%C3%A9%C4%9Fm%C4%99nt")]
    [InlineData(new[] { "single" }, "/single")]
    [InlineData(new[] { "\t", "\n", " ", "  " }, "/%09/%0A/%20/%20%20")]
    public void Build_produces_correct_absolute_path(string[] segments, string expectedUrl)
    {
        // Arrange
        var builder = new UrlBuilder(segments);

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal(expectedUrl, url);
    }

    [Theory]
    [InlineData(new string[] { }, "")]
    [InlineData(new[] { "api", "users" }, "api/users")]
    [InlineData(new[] { "api", "users", "details" }, "api/users/details")]
    [InlineData(new[] { "with spaces", "séğmęnt" }, "with%20spaces/s%C3%A9%C4%9Fm%C4%99nt")]
    [InlineData(new[] { "single" }, "single")]
    [InlineData(new[] { "\t", "\n", " ", "  " }, "%09/%0A/%20/%20%20")]
    public void Build_produces_correct_relative_path(string[] segments, string expectedUrl)
    {
        // Arrange
        var builder = new UrlBuilder(segments).AsRelative();

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal(expectedUrl, url);
    }

    [Fact]
    public void WithPath_accumulates_across_multiple_calls()
    {
        // Arrange
        var builder = new UrlBuilder("api")
            .WithPath("users")
            .WithPath("{id}", "details");

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal("/api/users/%7Bid%7D/details", url);
    }

    [Theory]
    [InlineData(" ", "/#%20")]
    [InlineData("  ", "/#%20%20")]
    [InlineData("\t", "/#%09")]
    [InlineData("\n", "/#%0A")]
    [InlineData("section1", "/#section1")]
    [InlineData("my fragment", "/#my%20fragment")]
    [InlineData("héllo", "/#h%C3%A9llo")]
    public void Build_produces_correct_fragment(string fragment, string expectedSuffix)
    {
        // Arrange
        var builder = new UrlBuilder().WithFragment(fragment);

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal(expectedSuffix, url);
    }

    [Theory]
    [InlineData("example.com", null, null, "example.com")]
    [InlineData("example.com", 8080, null, "example.com:8080")]
    [InlineData("example.com", null, "https", "https://example.com")]
    [InlineData("example.com", 443, "https", "https://example.com:443")]
    [InlineData("example.com", 80, "http", "http://example.com:80")]
    public void Build_produces_correct_domain_port_and_scheme(string domain, int? port, string? scheme, string expectedPrefix)
    {
        // Arrange
        var builder = new UrlBuilder().WithDomain(domain, port);

        if (scheme is not null)
        {
            builder = builder.WithScheme(scheme);
        }

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal(expectedPrefix, url);
    }

    [Theory]
    [InlineData("key1", "value1", "key2", "value2", "/?key1=value1&key2=value2")]
    [InlineData("key1", "", "key2", "", "/?key1=&key2=")]
    [InlineData("key1", "  ", "key2", " ", "/?key1=%20%20&key2=%20")]
    [InlineData("key1", "\n", "key2", "\t", "/?key1=%0A&key2=%09")]
    [InlineData("search", "hello world", "page", "1", "/?search=hello%20world&page=1")]
    [InlineData("filter", "a&b", "sort", "asc", "/?filter=a%26b&sort=asc")]
    public void Build_produces_correct_query_string(string key1, string val1, string key2, string val2, string expectedUrl)
    {
        // Arrange
        var builder = new UrlBuilder()
            .WithQueryParameter(key1, val1)
            .WithQueryParameter(key2, val2);

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal(expectedUrl, url);
    }

    [Fact]
    public void Empty_query_keys_are_not_allowed()
    {
        // Arrange
        var builder = new UrlBuilder()
            .WithQueryParameter(string.Empty, "hello");

        // Act
        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    public void Build_produces_full_url_with_all_parts()
    {
        // Arrange
        var builder = new UrlBuilder("api", "users")
            .WithScheme("https")
            .WithDomain("example.com", 8080)
            .WithQueryParameter("page", "1")
            .WithFragment("results");

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal("https://example.com:8080/api/users?page=1#results", url);
    }

    [Fact]
    public void Build_produces_absolute_url()
    {
        // Arrange
        var builder = new UrlBuilder("api", "users")
            .AsAbsolute()
            .WithQueryParameter("page", "1")
            .WithFragment("results");

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal("/api/users?page=1#results", url);
    }

    [Fact]
    public void Build_produces_relative_url_with_path()
    {
        // Arrange
        var builder = new UrlBuilder("api", "users")
            .AsRelative()
            .WithQueryParameter("page", "1")
            .WithFragment("results");

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal("api/users?page=1#results", url);
    }

    [Fact]
    public void Build_produces_relative_url_without_path()
    {
        // Arrange
        var builder = new UrlBuilder()
            .AsRelative()
            .WithQueryParameter("page", "1")
            .WithFragment("results");

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal("?page=1#results", url);
    }

    [Theory]
    [InlineData("https")]
    [InlineData("http")]
    [InlineData("ftp")]
    public void Build_throws_when_scheme_is_set_without_domain(string scheme)
    {
        // Arrange
        var builder = new UrlBuilder("api").WithScheme(scheme);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    public void Build_throws_when_relative_url_has_domain()
    {
        // Arrange
        var builder = new UrlBuilder("api")
            .AsRelative()
            .WithDomain("example.com", null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    public void Build_throws_when_relative_url_has_port()
    {
        // Arrange
        var builder = new UrlBuilder("api")
            .AsRelative()
            .WithDomain("example.com", 8080);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Fact]
    public void Build_throws_when_relative_url_has_scheme()
    {
        // Arrange
        var builder = new UrlBuilder("api")
            .AsRelative()
            .WithScheme("https")
            .WithDomain("example.com", null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(builder.Build);
    }

    [Theory]
    [InlineData("id", "42", new[] { "api", "users", "{id}" }, "/api/users/42")]
    [InlineData("id", "hello world", new[] { "api", "{id}", "details" }, "/api/hello%20world/details")]
    [InlineData("resource", "orders", new[] { "api", "{resource}" }, "/api/orders")]
    public void Build_substitutes_parameters_in_path(string key, string value, string[] segments, string expectedUrl)
    {
        // Arrange
        var builder = new UrlBuilder(segments).WithParameter(key, value);

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal(expectedUrl, url);
    }

    [Theory]
    [InlineData("filter", "active", "/?status=active")]
    [InlineData("filter", "hello world", "/?status=hello%20world")]
    public void Build_substitutes_parameters_in_query_values(string key, string value, string expectedUrl)
    {
        // Arrange
        var builder = new UrlBuilder()
            .WithQueryParameter("status", $"{{{key}}}")
            .WithParameter(key, value);

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal(expectedUrl, url);
    }

    [Theory]
    [InlineData("section", "intro", "/#intro")]
    [InlineData("section", "my section", "/#my%20section")]
    public void Build_substitutes_parameters_in_fragment(string key, string value, string expectedUrl)
    {
        // Arrange
        var builder = new UrlBuilder()
            .WithFragment($"{{{key}}}")
            .WithParameter(key, value);

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal(expectedUrl, url);
    }

    [Theory]
    [InlineData("host", "example.com", "https://example.com")]
    [InlineData("host", "other.org", "https://other.org")]
    public void Build_substitutes_parameters_in_domain(string key, string value, string expectedUrl)
    {
        // Arrange
        var builder = new UrlBuilder()
            .WithScheme("https")
            .WithDomain($"{{{key}}}", null)
            .WithParameter(key, value);

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal(expectedUrl, url);
    }

    [Fact]
    public void Build_substitutes_multiple_parameters_across_all_sections()
    {
        // Arrange
        var builder = new UrlBuilder("api", "{resource}", "{id}")
            .WithScheme("https")
            .WithDomain("abc{host}", null)
            .WithQueryParameter("format", "abc{fmt}")
            .WithFragment("abc{section}")
            .WithParameter("host", "example.com")
            .WithParameter("resource", "users")
            .WithParameter("id", "99")
            .WithParameter("fmt", "json")
            .WithParameter("section", "top");

        // Act
        var url = builder.Build();

        // Assert
        Assert.Equal("https://abcexample.com/api/users/99?format=abcjson#abctop", url);
    }

    [Fact]
    public void Build_is_idempotent()
    {
        // Arrange
        var builder = new UrlBuilder("api", "users")
            .WithScheme("https")
            .WithDomain("example.com", null)
            .WithQueryParameter("page", "1")
            .WithFragment("results")
            .WithParameter("unused", "value");

        // Act
        var url1 = builder.Build();
        var url2 = builder.Build();
        var url3 = builder.Build();

        // Assert
        Assert.Equal(url1, url2);
        Assert.Equal(url2, url3);
    }

    [Fact]
    public void With_methods_return_new_instances_and_do_not_mutate_original()
    {
        // Arrange
        var original = new UrlBuilder("api", "users").AsRelative();

        // Act — each With* call returns a new builder; original must remain unchanged
        var withPath = original.WithPath("extra");
        var withQuery = original.WithQueryParameter("page", "1");
        var withParam = original.WithParameter("key", "value");
        var withFragment = original.WithFragment("section");
        var withDomain = original.WithDomain("example.com", null);
        var withScheme = original.AsAbsolute().WithScheme("https").WithDomain("example.com", null);
        var asAbsolute = original.AsAbsolute();

        // Assert — original is untouched
        Assert.Equal("api/users", original.Build());

        // Assert — each derived builder reflects only its own change
        Assert.Equal("api/users/extra", withPath.Build());
        Assert.Equal("api/users?page=1", withQuery.Build());
        Assert.Equal("api/users", withParam.Build()); // parameter unused in path
        Assert.Equal("api/users#section", withFragment.Build());
        Assert.Throws<InvalidOperationException>(withDomain.Build); // relative + domain → invalid
        Assert.Equal("https://example.com/api/users", withScheme.Build());
        Assert.Equal("/api/users", asAbsolute.Build());
    }

    [Fact]
    public void Build_result_is_not_affected_by_subsequent_builder_mutations()
    {
        // Arrange
        var original = new UrlBuilder("api", "users").AsRelative();

        // Act
        var mutated = original
            .AsAbsolute()
            .WithScheme("https")
            .WithDomain("example.com", null)
            .WithQueryParameter("page", "1");

        // Assert
        Assert.Equal("api/users", original.Build());
        Assert.Equal("https://example.com/api/users?page=1", mutated.Build());
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/api")]
    [InlineData("http://example.com:8080")]
    [InlineData("https://example.com:80/api")]
    [InlineData("https://example.com:80/api/users")]
    [InlineData("https://example.com:8080/api/users?page=1")]
    [InlineData("https://example.com:8080/api/users?page=1#section")]
    [InlineData("https://example.com/api%20space?q=hello%20world#frag%20ment")]
    [InlineData("example.com")]
    [InlineData("example.com/api")]
    [InlineData("example.com:8080")]
    [InlineData("example.com:80/api")]
    [InlineData("example.com:80/api/users")]
    [InlineData("example.com:8080/api/users?page=1")]
    [InlineData("example.com:8080/api/users?page=1#section")]
    [InlineData("example.com/api%20space?q=hello%20world#frag%20ment")]
    [InlineData("/")]
    [InlineData("/api")]
    [InlineData("/api/users")]
    [InlineData("/api/users?page=1")]
    [InlineData("/api/users?page=1#section")]
    [InlineData("api")]
    [InlineData("api/users")]
    [InlineData("api/users?page=1")]
    [InlineData("api/users?page=1#section")]
    [InlineData("?page=1#section")]
    [InlineData("#section")]
    [InlineData("/api?a=1&b=2")]
    [InlineData("/api?a=1&a=2")]
    public void FromUrl_roundtrip_urls(string url)
    {
        // Act
        var result = UrlBuilder.FromUrl(url).Build();

        // Assert
        Assert.Equal(url, result);
    }

    [Theory]
    [InlineData("/api?key=", "/api?key=")]
    [InlineData("/api?key= ", "/api?key=%20")]
    [InlineData("/api?flag", "/api?flag=")]
    [InlineData("//", "/")]
    [InlineData("/?#", "/")]
    [InlineData("/api/", "/api")]
    [InlineData("api/", "api")]
    [InlineData("/api#", "/api")]
    [InlineData("/api# ", "/api#%20")]
    [InlineData("/api/#", "/api")]
    [InlineData("/api#hello%20world", "/api#hello%20world")]
    [InlineData("/api//users///details", "/api/users/details")]
    [InlineData("/api/{id}/details", "/api/%7Bid%7D/details")]
    [InlineData("/api?filter={value}", "/api?filter=%7Bvalue%7D")]
    [InlineData("/api#{section}", "/api#%7Bsection%7D")]
    [InlineData("example.com:8080/api/users/?page=1#section", "example.com:8080/api/users?page=1#section")]
    [InlineData("http://example.com:8080/api/users/?page=1#section", "http://example.com:8080/api/users?page=1#section")]
    public void FromUrl_roundtrip_urls_with_cleaning(string input, string expected)
    {
        var result = UrlBuilder.FromUrl(input).Build();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("://example.com")]
    [InlineData("http://example.com:abc")]
    [InlineData("http://example.com:-1")]
    [InlineData("http://example.com:999999")]
    public void FromUrl_invalid_input_throws(string input)
    {
        Assert.Throws<ArgumentException>(() => UrlBuilder.FromUrl(input));
    }
}
