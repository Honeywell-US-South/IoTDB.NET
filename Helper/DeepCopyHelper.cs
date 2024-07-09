namespace IoTDBdotNET.Helper
{
    public static class DeepCopyHelper
    {
        public static T DeepCopy<T>(T obj)
        {
            // Check if the object is serializable
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", nameof(obj));
            }

            // Deep copy using JSON serialization
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // Optional: ignore case sensitivity for property names
                WriteIndented = false // Optional: pretty-print the JSON
            };

            // Serialize the object to JSON
            var json = JsonSerializer.Serialize(obj, options);

            // Deserialize JSON to create a new instance
            return JsonSerializer.Deserialize<T>(json, options) ?? default!;
        }
    }
}
