using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IoTDBdotNET
{
    public class IotValue
    {

        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public string?[] Values { get; set; } = new string?[16];
        public DateTime?[] Timestamps { get; set; } = new DateTime?[16];
        public string Unit { get; set; } = "n/a";
        public bool AllowManualOperator { get; set; } = true;
        public bool TimeSeries { get; set; } = false;
        public bool BlockChain { get; set; } = false;
        public System.Type? StrictDataType { get; set; } = null;
        public bool IsPassword { get; set; } = false;

        public IotValue()
        {
            InitValues();
        }

        public IotValue(string name, string description, object? value, bool isPassword, bool allowManualOperator, bool timeSeries, bool blockChain)
        {
            InitValues();
            Name = name;
            Description = description;
            AllowManualOperator = allowManualOperator;
            TimeSeries = timeSeries;
            BlockChain = blockChain;

            if (isPassword && value?.GetType() != typeof(string))
            {

                throw new InvalidConstraintException("Password value type is not of type string.");
            } else if (isPassword && value != null)
            {
                SetPassword(16, value.ToString());
            }
            else
            {
                SetValue(16, value);
            }
        }

        private void InitValues()
        {
            for (int i = 0; i < Values.Length; i++)
            {
                Values[i] = null;
                //HasValues[i] = false;
            }
        }
        internal bool SetRawValue(int index, string? value)
        {
            if (index < 0 && index >= Values.Length) return false;

            if (AllowManualOperator && index == 7)
            {
                Values[index] = null;
                Timestamps[index] = null;
                //HasValues[index] = false;
                return false; //manual operator 
            }

            Values[index] = value;
            Timestamps[index] = DateTime.UtcNow;
            //HasValues[index] = value != null;

            return true;
        }


        public string? Value
        {
            get
            {
                for (int i = 0; i < Values.Length; i++)
                {
                    if (Values[i] != null) return Values[i];
                }

                return null;
            }
            
        }

        public int Priority
        {
            get
            {
                for (int i = 0; i < Values.Length; i++)
                {
                    if (Values[i] != null) return i + 1;
                }
                return 0;
            }
            
        }

        public DateTime Timestamp
        {
            get
            {
                for (int i = 0; i < Timestamps.Length; i++)
                {
                    if (Timestamps[i] != null) return Timestamps[i]??DateTime.MinValue;
                }
                return DateTime.MinValue;
            }
        }

        #region Check
        /// <summary>
        /// Check if value is Guid type
        /// </summary>
        public bool IsGuid => System.Guid.TryParse(Value, out _);

        /// <summary>
        /// Check if value is Numeric type
        /// </summary>
        public bool IsNumeric => double.TryParse(Value, out _);

        /// <summary>
        /// Check if value is Double type
        /// </summary>
        public bool IsDouble => double.TryParse(Value, out _);

        /// <summary>
        /// Check if value is Boolean type
        /// </summary>
        public bool IsBoolean => bool.TryParse(Value, out _);

        /// <summary>
        /// Check if value is DateTime type
        /// </summary>
        public bool IsDateTime => DateTime.TryParse(Value, out _);

        /// <summary>
        /// Check if value is Integer type
        /// </summary>
        public bool IsInteger => int.TryParse(Value, out _);

        /// <summary>
        /// Check if value is Long type
        /// </summary>
        public bool IsLong => long.TryParse(Value, out _);

        /// <summary>
        /// Check if value is Float type
        /// </summary>
        public bool IsFloat => float.TryParse(Value, out _);

        /// <summary>
        /// Check if value is Decimal type
        /// </summary>
        public bool IsDecimal => decimal.TryParse(Value, out _);

        /// <summary>
        /// Check if value is Char type
        /// </summary>
        public bool IsChar => char.TryParse(Value, out _);

        /// <summary>
        /// Check if value is T type
        /// </summary>
        public bool IsObject<T>() where T : class
        {
            if (string.IsNullOrEmpty(Value))
            {
                return false;
            }

            try
            {
                var obj = System.Text.Json.JsonSerializer.Deserialize<T>(Value);
                return obj != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the given string is a valid SHA-256 hash.
        /// </summary>
        /// <returns>True if the string is a valid SHA-256 hash; otherwise, false.</returns>
        public bool IsHash
        {
            get
            {
                if (string.IsNullOrEmpty(Value))
                {
                    return false;
                }

                // Regular expression to match a 64-character hexadecimal string
                var regex = new Regex("^[a-fA-F0-9]{64}$");
                return regex.IsMatch(Value);
            }
        }

        /// <summary>
        /// Checks if the given string is a valid SHA-256 hash password.
        /// </summary>
        /// <param name="hash">The string to check.</param>
        /// <returns>True if the string is a valid SHA-256 hash password; otherwise, false.</returns>
        public bool IsPasswordHash
        {
            get
            {
                return IsHash;
            }
        }
        #endregion

        #region Set
        /// <summary>
        /// Priority 1: Manual Operator Override (Highest priority)
        /// Priority 2: Critical Equipment Control
        /// Priority 3: Life Safety
        /// Priority 4: Fire Safety
        /// Priority 5: Emergency
        /// Priority 6: Safety
        /// Priority 7: Control Strategy
        /// Priority 8: Manual Operator
        /// Priority 9: Available
        /// Priority 10: Available
        /// Priority 11: Available
        /// Priority 12: Available
        /// Priority 13: Available
        /// Priority 14: Available
        /// Priority 15: Default Value Set
        /// Priority 16: Default or Fallback Value(Lowest priority)
        /// </summary>
        /// <param name="priority">int</param>
        /// <param name="value">object</param>
        /// <returns>true/false</returns>
        public bool SetValue(int priority, object? value)
        {
            ValidateType(value);
            int index = priority - 1;

            return SetRawValue(index, ToStringValue(value));

        }

        /// <summary>
        /// Priority 1: Manual Operator Override (Highest priority)
        /// Priority 2: Critical Equipment Control
        /// Priority 3: Life Safety
        /// Priority 4: Fire Safety
        /// Priority 5: Emergency
        /// Priority 6: Safety
        /// Priority 7: Control Strategy
        /// Priority 8: Manual Operator
        /// Priority 9: Available
        /// Priority 10: Available
        /// Priority 11: Available
        /// Priority 12: Available
        /// Priority 13: Available
        /// Priority 14: Available
        /// Priority 15: Default Value Set
        /// Priority 16: Default or Fallback Value(Lowest priority)
        /// </summary>
        /// <param name="priority">int</param>
        /// <param name="value">string</param>
        /// <returns>true/false</returns>
        public bool SetValue(int priority, string? value)
        {
            ValidateType(value);
            int index = priority - 1;
            return SetRawValue(index, ToStringValue(value));
        }

        /// <summary>
        /// Serialize object and set value at priority
        /// Priority 1: Manual Operator Override (Highest priority)
        /// Priority 2: Critical Equipment Control
        /// Priority 3: Life Safety
        /// Priority 4: Fire Safety
        /// Priority 5: Emergency
        /// Priority 6: Safety
        /// Priority 7: Control Strategy
        /// Priority 8: Manual Operator
        /// Priority 9: Available
        /// Priority 10: Available
        /// Priority 11: Available
        /// Priority 12: Available
        /// Priority 13: Available
        /// Priority 14: Available
        /// Priority 15: Default Value Set
        /// Priority 16: Default or Fallback Value(Lowest priority)
        /// </summary>
        /// <param name="priority">int</param>
        /// <param name="value">class T</param>
        /// <returns>true/false</returns>
        public bool SetObject<T>(int priority, T? value) where T : class
        {
            ValidateType(value);
            int index = priority - 1;
            return SetRawValue(index, ToStringValue(value));
        }

        /// <summary>
        /// Serialize object and set value at priority
        /// Priority 1: Manual Operator Override (Highest priority)
        /// Priority 2: Critical Equipment Control
        /// Priority 3: Life Safety
        /// Priority 4: Fire Safety
        /// Priority 5: Emergency
        /// Priority 6: Safety
        /// Priority 7: Control Strategy
        /// Priority 8: Manual Operator
        /// Priority 9: Available
        /// Priority 10: Available
        /// Priority 11: Available
        /// Priority 12: Available
        /// Priority 13: Available
        /// Priority 14: Available
        /// Priority 15: Default Value Set
        /// Priority 16: Default or Fallback Value(Lowest priority)
        /// </summary>
        /// <param name="priority">int</param>
        /// <param name="password">raw password string</param>
        /// <returns>true/false</returns>
        public bool SetPassword(int priority, string? password)
        {
            ValidateType(password);
            int index = priority - 1;
            IsPassword = true;
            return SetRawValue(index, ToPasswordHash(password));

        }

        /// <summary>
        /// Priority 1: Manual Operator Override (Highest priority)
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueManualOperatorOverride(object? value)
        {
            ValidateType(value);
            return SetValue(1, value);
        }

        /// <summary>
        /// Priority 2: Critical Equipment Control
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueCritica(object? value) => SetValue(2, value);


        /// <summary>
        /// Priority 3: Life Safety
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueLifeSafety(object? value) => SetValue(3, value);


        /// <summary>
        /// Priority 4: Fire Safety
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueFireSafety(object? value) => SetValue(4, value);


        /// <summary>
        /// Priority 5: Emergency
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueEmergency(object? value) => SetValue(5, value);


        /// <summary>
        /// Priority 6: Safety
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueSafety(object? value) => SetValue(6, value);


        /// <summary>
        /// Priority 7: Control Strategy
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueControlStrategy(object? value) => SetValue(7, value);


        /// <summary>
        /// Priority 8: Manual Operator
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueManualOperator(object? value) => SetValue(8, value);


        /// <summary>
        /// Priority 9: Free
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueFree09(object? value) => SetValue(9, value);


        /// <summary>
        /// Priority 10: Free
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueFree10(object? value) => SetValue(10, value);


        /// <summary>
        /// Priority 11: Free
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueFree11(object? value) => SetValue(11, value);


        /// <summary>
        /// Priority 12: Free
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueFree12(object? value) => SetValue(12, value);


        /// <summary>
        /// Priority 13: Free
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueFree13(object? value) => SetValue(13, value);


        /// <summary>
        /// Priority 14: Free
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueFree14(object? value) => SetValue(14, value);


        /// <summary>
        /// Priority 15: Default Value Set
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueDefault(object? value) => SetValue(15, value);


        /// <summary>
        /// Priority 16: Default or Fallback Value (Lowest priority)
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true/fals</returns>
        public bool SetValueDefaultFallback(object? value) => SetValue(16, value);

        #endregion

        #region Get

        /// <summary>
        /// Get the highest prioriety. Zero means value is not set.
        /// </summary>
        public int AsPriority
        {
            get
            {
                return Priority;
            }
        }

        /// <summary>
        /// Get value as data type
        /// </summary>
        public System.Type? AsType
        {
            get
            {
                if (Value == null) return null;

                if (IsGuid) return typeof(Guid);
                if (IsBoolean) return typeof(bool);
                if (IsDateTime) return typeof(DateTime);
                if (IsInteger) return typeof(int);
                if (IsLong) return typeof(long);
                if (IsDouble) return typeof(double);
                if (IsFloat) return typeof(float);
                if (IsDecimal) return typeof(decimal);
                if (IsChar) return typeof(char);

                try
                {
                    var obj = System.Text.Json.JsonSerializer.Deserialize<object>(Value);
                    if (obj != null) return obj.GetType();
                }
                catch (JsonException) { }

                return typeof(string); // Default to string if no other type matches
            }
        }

        /// <summary>
        /// Get value as boolean. Return null if Value cannot parse as boolean.
        /// </summary>
        public bool? AsBoolean
        {
            get
            {
                if (bool.TryParse(Value, out bool result))
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Get value as DateTime. Return null if Value cannot parse as DateTime.
        /// </summary>
        public DateTime? AsDateTime
        {
            get
            {
                if (DateTime.TryParse(Value, out DateTime result))
                {
                    return result;
                }
                return null;
            }
        }
        /// <summary>
        /// Get value as integer. Return null if Value cannot parse as integer.
        /// </summary>
        public int? AsInteger
        {
            get
            {
                if (int.TryParse(Value, out int result))
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Get value as double. Return null if Value cannot parse as double.
        /// </summary>
        public double? AsDouble
        {
            get
            {
                if (double.TryParse(Value, out double result))
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Get value as long. Return null if Value cannot parse as long.
        /// </summary>
        public long? AsLong
        {
            get
            {
                if (long.TryParse(Value, out long result))
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Get value as Guid. Return null if Value cannot parse as Guid.
        /// </summary>
        public Guid? AsGuid
        {
            get
            {
                if (System.Guid.TryParse(Value, out Guid result))
                {
                    return result;
                }
                return null;
            }
        }

        /// <summary>
        /// Get value as float. Return null if Value cannot parse as float.
        /// </summary>
        public float? AsFloat => float.TryParse(Value, out float result) ? result : (float?)null;

        /// <summary>
        /// Get value as decimal. Return null if Value cannot parse as decimal.
        /// </summary>
        public decimal? AsDecimal => decimal.TryParse(Value, out decimal result) ? result : (decimal?)null;

        /// <summary>
        /// Get value as dhar. Return null if Value cannot parse as char.
        /// </summary>
        public char? AsChar => char.TryParse(Value, out char result) ? result : (char?)null;
        /// <summary>
        /// Get value as string. Return null if Value cannot parse as string.
        /// </summary>
        public string? AsString
        {
            get
            {
                return Value;
            }
        }


        /// <summary>
        /// Get value as a deserialized object. Return null if Value cannot deserialize.
        /// </summary>
        public T? AsObject<T>() where T : class
        {
            if (string.IsNullOrEmpty(Value))
            {
                return null;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(Value);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing JSON: {ex.Message}");

            }
            return null;

        }

        #endregion

        #region Helper

        /// <summary>
        /// Check value datatype. If DataType is Attribute, all types are accepted.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateType(object? value)
        {
            if (value == null) return;
            if (StrictDataType == null) return;
            if (value.GetType() != StrictDataType)
            {
                throw new ArgumentException($"Invalid data type. Expected strict data type of {StrictDataType}, but got {value.GetType()}.");
            }
        }

        /// <summary>
        /// Generates a SHA-256 hash for the given password.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <returns>The hashed password as a hexadecimal string.</returns>
        public static string? ToPasswordHash(string? password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return null;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                // Convert the password string to a byte array
                byte[] bytes = Encoding.UTF8.GetBytes(password);

                // Compute the hash
                byte[] hashBytes = sha256.ComputeHash(bytes);

                // Convert the hash byte array to a hexadecimal string
                StringBuilder hashStringBuilder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    hashStringBuilder.Append(b.ToString("x2"));
                }

                return hashStringBuilder.ToString();
            }
        }

        /// <summary>
        /// Convert object to string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string? ToStringValue(object? value)
        {
            if (value == null) return null;
            return value switch
            {
                int intValue => intValue.ToString(),
                double doubleValue => doubleValue.ToString(),
                float floatValue => floatValue.ToString(),
                decimal decimalValue => decimalValue.ToString(),
                bool boolValue => boolValue.ToString(),
                DateTime dateTimeValue => dateTimeValue.ToString("o"), // ISO 8601 format
                Guid guidValue => guidValue.ToString(),
                char charValue => charValue.ToString(),
                byte[] byteArrayValue => Convert.ToBase64String(byteArrayValue),
                _ when value?.GetType().IsClass == true => System.Text.Json.JsonSerializer.Serialize(value),
                _ => value?.ToString() ?? string.Empty,
            };
        }


        #endregion
    }
}
