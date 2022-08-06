using System.Text.Json;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.Serialization.Implementations
{
    /// <summary>
    /// A <see cref="IDataSerializer"/> that uses <see cref="System.Text.Json"/>.
    /// </summary>
    public class JsonDataSerializer: IDataSerializer
    {
        /// <inheritdoc />
        public RedisValue Serialize<T>(T data) => JsonSerializer.Serialize(data);

        /// <inheritdoc />
        public T Deserialize<T>(RedisValue data)
        {
            var json = (string?)data;
            if (json == null)
            {
                throw new ArgumentNullException(nameof(data));
            }


            var result = JsonSerializer.Deserialize<T>(json);
            if (result == null)
            {
                throw new ApplicationException("Failed to deserialize data.")
                {
                    Data = { { "rawData", data } }
                };
            }

            return result;
        }
    }
}
