using Backend;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class SqliteTestFixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<PlannerContext> _options;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PlannerContext> _logger;

        public SqliteTestFixture()
        {
            // Keep the connection open for the lifetime of the fixture
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<PlannerContext>()
                .UseSqlite(_connection)
                .Options;

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "SomeSetting", "TestValue" }
                })
                .Build();

            _logger = NullLogger<PlannerContext>.Instance;

            // Create schema once
            using var context = new PlannerContext(_options, _configuration, _logger);
            context.Database.EnsureCreated();
            context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
        }

        // Factory method for fresh contexts
        public PlannerContext CreateContext() => new(_options, _configuration, _logger);

        public IConfiguration Configuration { get { return _configuration; } }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
        }
    }

    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<SqliteTestFixture>
    {
        // This class is just a marker for the collection.
    }