using System.Linq.Expressions;
using CosmosDbPoC.Data;
using CosmosDbPoC.Data.Repository;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CosmosDbPoC.Tests
{
    public class CosmosRepositoryTests
    {
        private readonly CosmosRepository<MyEntity> _repository;

        public CosmosRepositoryTests()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var host = configuration.GetSection("CosmosSettings").GetSection("Host").Value;
            var primaryKey = configuration.GetSection("CosmosSettings").GetSection("PrimaryKey").Value;
            var dbName = configuration.GetSection("CosmosSettings").GetSection("DbName").Value;

            _repository = new CosmosRepository<MyEntity>(host!, primaryKey!, dbName!, MyEntity.ContainerId);
        }

        [Fact]
        public async Task CreateMany()
        {
            var records = MyEntity.GenerateRandomDto(1000);
            var response = await _repository.CreateAsync(records);
            response.All(f => f.Result).Should().BeTrue();
        }

        [Fact]
        public async Task CreateOne()
        {
            var dto = MyEntity.GenerateRandomDto(1).Single();
            var response = await _repository.CreateAsync(dto);
            response.Should().BeTrue();
        }

        [Fact]
        public async Task GetAll()
        {
            var response = await _repository.GetAllAsync();
            response.Should().HaveCountGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task GetById()
        {
            var record = await CreateNewRecord();
            await Task.Delay(200);

            var response = await _repository.GetByIdAsync(record.Id);
            response.Should().NotBeNull();
        }

        [Theory]
        [InlineData("Locator")]
        [InlineData("Name")]
        public async Task GetByField(string field)
        {
            var record = await CreateNewRecord();
            await Task.Delay(200);

            var value = record.GetType().GetProperty(field)?.GetValue(record);
            var param = Expression.Parameter(typeof(MyEntity), "name");
            var prop = Expression.Property(param, field);
            var constant = Expression.Constant(value);
            var equal = Expression.Equal(prop, constant);
            var lambda = Expression.Lambda<Func<MyEntity, bool>>(equal, param);

            var response = await _repository.GetByFieldAsync(lambda);

            response.Should().NotBeNull();
            response.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task Update()
        {
            var record = await CreateNewRecord();
            await Task.Delay(200);

            record.Name = "Changed Name";
            var response = await _repository.UpdateAsync(record.Id, record);
            response.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteById()
        {
            var record = await CreateNewRecord();
            await Task.Delay(200);

            var response = await _repository.DeleteAsync(record.Id);
            response.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteByEntity()
        {
            var record = await CreateNewRecord();
            await Task.Delay(200);

            var response = await _repository.DeleteAsync(record);
            response.Should().BeTrue();
        }

        private async Task<MyEntity> CreateNewRecord()
        {
            var record = MyEntity.GenerateRandomDto(1).Single();
            await _repository.CreateAsync(record);

            return record;
        }
    }
}