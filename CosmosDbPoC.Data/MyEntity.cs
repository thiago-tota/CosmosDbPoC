using System.Text.Json.Serialization;

namespace CosmosDbPoC.Data
{
    public class MyEntity : IEntity
    {
        [JsonIgnore]
        public static string ContainerId => "MyEntityCollection";

        public string Id { get; set; } = string.Empty;
        public string PartitionKey { get; set; } = string.Empty;
        public int Locator { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public decimal Salary { get; set; }

        // TODO: PartitionKey must be defined properly. Here is IsActive due it's a PoC.
        public bool IsActive
        {
            get => Convert.ToBoolean(PartitionKey);
            set => PartitionKey = value.ToString();
        }

        public byte[] Image { get; set; } = Array.Empty<byte>();
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public TimeSpan Duration { get; set; }
        public char CharValue { get; set; }
        public short ShortValue { get; set; }
        public long LongValue { get; set; }
        public float FloatValue { get; set; }

        [JsonIgnore]
        public string JsonObject { get; set; } = string.Empty;

        public static IEnumerable<MyEntity> GenerateRandomDto(int quantity)
        {
            var random = new Random();
            var dtos = new List<MyEntity>();

            for (var i = 0; i < quantity; i++)
            {
                var dto = new MyEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    Locator = random.Next(1000000, 9999999),
                    Name = Path.GetRandomFileName(),
                    DateOfBirth = DateTime.Now.AddDays(-random.Next(365 * 100)),
                    Salary = (decimal)random.NextDouble() * 10000,
                    IsActive = random.Next(2) == 0,
                    Image = new byte[100],
                    Latitude = random.NextDouble() * 180 - 90,
                    Longitude = random.NextDouble() * 360 - 180,
                    Duration = TimeSpan.FromSeconds(random.NextDouble() * 3600),
                    CharValue = (char)random.Next(65, 91),
                    ShortValue = (short)random.Next(short.MinValue, short.MaxValue),
                    LongValue = random.NextInt64(long.MinValue, long.MaxValue),
                    FloatValue = (float)random.NextDouble() * 10000,
                };

                random.NextBytes(dto.Image);
                dto.JsonObject = dto.ToJson(JsonExtension.GetApplicationDefaultOptions());

                dtos.Add(dto);
            }

            return dtos;
        }
    }
}
