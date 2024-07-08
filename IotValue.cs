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
        public string Unit { get; set; } = Units.no_unit;
        public bool AllowManualOperator { get; set; } = true;
        public bool TimeSeries { get; set; } = false;
        public bool BlockChain { get; set; } = false;
        public System.Type? StrictDataType { get; set; } = null;
        public bool IsPassword { get; set; } = false;

        public IotValue()
        {
            InitValues();
        }

        public IotValue(string name, string description)
        {
            InitValues();
            Name = name;
            Description = description;
        }

        public IotValue (string name, string description, object? value, string unit)
        {
            InitValues();
            Name = name;
            Description = description;
            SetValue(16, value);
            Unit = unit;
        }
        public IotValue(string name, string description, object? value, string unit, bool isPassword, bool allowManualOperator, bool timeSeries, bool blockChain)
        {
            InitValues();
            Name = name;
            Description = description;
            Unit = unit;
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

        #region Engineering Units
        public static class Units
        {
            public static class Acceleration
            {
                public const string meters_per_second_per_second = "meters_per_second_per_second"; //	166	Acceleration
            }

            public static class Area
            {
                public const string square_meters = "square_meters"; //	0	Area
                public const string square_centimeters = "square_centimeters"; //	116	Area
                public const string square_feet = "square_feet"; //	1	Area
                public const string square_inches = "square_inches"; //	115	Area
            }

            public static class Currency
            {
                public const string currency1 = "currency1"; //	105	Currency

                public const string currency2 = "currency2"; //	106	Currency
                public const string currency3 = "currency3"; //	107	Currency
                public const string currency4 = "currency4"; //	108	Currency
                public const string currency5 = "currency5"; //	109	Currency
                public const string currency6 = "currency6"; //	110	Currency
                public const string currency7 = "currency7"; //	111	Currency
                public const string currency8 = "currency8"; //	112	Currency
                public const string currency9 = "currency9"; //	113	Currency
                public const string currency10 = "currency10"; //	114	Currency
            }
            public static class Electrical
            {
                public const string milliamperes = "milliamperes"; //	2	Electrical
                public const string amperes = "amperes"; //	3	Electrical
                public const string amperes_per_meter = "amperes_per_meter"; //	167	Electrical
                public const string amperes_per_square_meter = "amperes_per_square_meter"; //	168	Electrical
                public const string ampere_square_meters = "ampere_square_meters"; //	169	Electrical
                public const string decibels = "decibels"; //	199	Electrical
                public const string decibels_millivolt = "decibels_millivolt"; //	200	Electrical
                public const string decibels_volt = "decibels_volt"; //	201	Electrical
                public const string farads = "farads"; //	170	Electrical
                public const string henrys = "henrys"; //	171	Electrical
                public const string ohms = "ohms"; //	4	Electrical
                public const string ohm_meter_squared_per_meter = "ohm_meter_squared_per_meter"; //	237	Electrical
                public const string ohm_meters = "ohm_meters"; //	172	Electrical
                public const string milliohms = "milliohms"; //	145	Electrical
                public const string kilohms = "kilohms"; //	122	Electrical
                public const string megohms = "megohms"; //	123	Electrical
                public const string microsiemens = "microsiemens"; //	190	Electrical
                public const string millisiemens = "millisiemens"; //	202	Electrical
                public const string siemens = "siemens"; //	173	Electrical
                public const string siemens_per_meter = "siemens_per_meter"; //	174	Electrical
                public const string teslas = "teslas"; //	175	Electrical
                public const string volts = "volts"; //	5	Electrical
                public const string millivolts = "millivolts"; //	124	Electrical
                public const string kilovolts = "kilovolts"; //	6	Electrical
                public const string megavolts = "megavolts"; //	7	Electrical
                public const string volt_amperes = "volt_amperes"; //	8	Electrical
                public const string kilovolt_amperes = "kilovolt_amperes"; //	9	Electrical
                public const string megavolt_amperes = "megavolt_amperes"; //	10	Electrical
                public const string volt_amperes_reactive = "volt_amperes_reactive"; //	11	Electrical
                public const string kilovolt_amperes_reactive = "kilovolt_amperes_reactive"; //	12	Electrical
                public const string megavolt_amperes_reactive = "megavolt_amperes_reactive"; //	13	Electrical
                public const string volts_per_degree_kelvin = "volts_per_degree_kelvin"; //	176	Electrical
                public const string volts_per_meter = "volts_per_meter"; //	177	Electrical
                public const string degrees_phase = "degrees_phase"; //	14	Electrical
                public const string power_factor = "power_factor"; //	15	Electrical
                public const string webers = "webers"; //	178	Electrical
            }
            public static class Energy
            {
                public const string ampere_seconds = "ampere_seconds"; //	238	Energy
                public const string volt_ampere_hours = "volt_ampere_hours"; //	239	Energy
                public const string kilovolt_ampere_hours = "kilovolt_ampere_hours"; //	240	Energy
                public const string megavolt_ampere_hours = "megavolt_ampere_hours"; //	241	Energy
                public const string volt_ampere_hours_reactive = "volt_ampere_hours_reactive"; //	242	Energy
                public const string kilovolt_ampere_hours_reactive = "kilovolt_ampere_hours_reactive"; //	243	Energy
                public const string megavolt_ampere_hours_reactive = "megavolt_ampere_hours_reactive"; //	244	Energy
                public const string volt_square_hours = "volt_square_hours"; //	245	Energy
                public const string ampere_square_hours = "ampere_square_hours"; //	246	Energy
                public const string joules = "joules"; //	16	Energy
                public const string kilojoules = "kilojoules"; //	17	Energy
                public const string kilojoules_per_kilogram = "kilojoules_per_kilogram"; //	125	Energy
                public const string megajoules = "megajoules"; //	126	Energy
                public const string watt_hours = "watt_hours"; //	18	Energy
                public const string kilowatt_hours = "kilowatt_hours"; //	19	Energy
                public const string megawatt_hours = "megawatt_hours"; //	146	Energy
                public const string watt_hours_reactive = "watt_hours_reactive"; //	203	Energy
                public const string kilowatt_hours_reactive = "kilowatt_hours_reactive"; //	204	Energy
                public const string megawatt_hours_reactive = "megawatt_hours_reactive"; //	205	Energy
                public const string btus = "btus"; //	20	Energy
                public const string kilo_btus = "kilo_btus"; //	147	Energy
                public const string mega_btus = "mega_btus"; //	148	Energy
                public const string therms = "therms"; //	21	Energy
                public const string ton_hours = "ton_hours"; //	22	Energy
            }
            public static class Enthalpy
            {
                public const string joules_per_kilogram_dry_air = "joules_per_kilogram_dry_air"; //	23	Enthalpy
                public const string kilojoules_per_kilogram_dry_air = "kilojoules_per_kilogram_dry_air"; //	149	Enthalpy
                public const string megajoules_per_kilogram_dry_air = "megajoules_per_kilogram_dry_air"; //	150	Enthalpy
                public const string btus_per_pound_dry_air = "btus_per_pound_dry_air"; //	24	Enthalpy
                public const string btus_per_pound = "btus_per_pound"; //	117	Enthalpy
                public const string joules_per_degree_kelvin = "joules_per_degree_kelvin"; //	127	Entropy
                public const string kilojoules_per_degree_kelvin = "kilojoules_per_degree_kelvin"; //	151	Entropy
                public const string megajoules_per_degree_kelvin = "megajoules_per_degree_kelvin"; //	152	Entropy
                public const string joules_per_kilogram_degree_kelvin = "joules_per_kilogram_degree_kelvin"; //	128	Entropy
            }
            public static class Force
            {
                public const string newton = "newton"; //	153	Force
            }
            public static class Frequency
            {
                public const string cycles_per_hour = "cycles_per_hour"; //	25	Frequency
                public const string cycles_per_minute = "cycles_per_minute"; //	26	Frequency
                public const string hertz = "hertz"; //	27	Frequency
                public const string kilohertz = "kilohertz"; //	129	Frequency
                public const string megahertz = "megahertz"; //	130	Frequency
                public const string per_hour = "per_hour"; //	131	Frequency
            }
            public static class Humidity
            {
                public const string grams_of_water_per_kilogram_dry_air = "grams_of_water_per_kilogram_dry_air"; //	28	Humidity
                public const string percent_relative_humidity = "percent_relative_humidity"; //	29	Humidity
            }
            public static class Length
            {
                public const string micrometers = "micrometers"; //	194	Length
                public const string millimeters = "millimeters"; //	30	Length
                public const string centimeters = "centimeters"; //	118	Length
                public const string kilometers = "kilometers"; //	193	Length
                public const string meters = "meters"; //	31	Length
                public const string inches = "inches"; //	32	Length
                public const string feet = "feet"; //	33	Length
            }
            public static class Light
            {
                public const string candelas = "candelas"; //	179	Light
                public const string candelas_per_square_meter = "candelas_per_square_meter"; //	180	Light
                public const string watts_per_square_foot = "watts_per_square_foot"; //	34	Light
                public const string watts_per_square_meter = "watts_per_square_meter"; //	35	Light
                public const string lumens = "lumens"; //	36	Light
                public const string luxes = "luxes"; //	37	Light
                public const string foot_candles = "foot_candles"; //	38	Light
            }
            public static class Mass
            {
                public const string milligrams = "milligrams"; //	196	Mass
                public const string grams = "grams"; //	195	Mass
                public const string kilograms = "kilograms"; //	39	Mass
                public const string pounds_mass = "pounds_mass"; //	40	Mass
                public const string tons = "tons"; //	41	Mass
            }
            public static class MassFlow
            {
                public const string grams_per_second = "grams_per_second"; //	154	Mass Flow
                public const string grams_per_minute = "grams_per_minute"; //	155	Mass Flow
                public const string kilograms_per_second = "kilograms_per_second"; //	42	Mass Flow
                public const string kilograms_per_minute = "kilograms_per_minute"; //	43	Mass Flow
                public const string kilograms_per_hour = "kilograms_per_hour"; //	44	Mass Flow
                public const string pounds_mass_per_second = "pounds_mass_per_second"; //	119	Mass Flow
                public const string pounds_mass_per_minute = "pounds_mass_per_minute"; //	45	Mass Flow
                public const string pounds_mass_per_hour = "pounds_mass_per_hour"; //	46	Mass Flow
                public const string tons_per_hour = "tons_per_hour"; //	156	Mass Flow
            }
            public static class Power
            {
                public const string milliwatts = "milliwatts"; //	132	Power
                public const string watts = "watts"; //	47	Power
                public const string kilowatts = "kilowatts"; //	48	Power
                public const string megawatts = "megawatts"; //	49	Power
                public const string btus_per_hour = "btus_per_hour"; //	50	Power
                public const string kilo_btus_per_hour = "kilo_btus_per_hour"; //	157	Power
                public const string joule_per_hours = "joule_per_hours"; //	247	Power
                public const string horsepower = "horsepower"; //	51	Power
                public const string tons_refrigeration = "tons_refrigeration"; //	52	Power
            }
            public static class Pressure
            {
                public const string pascals = "pascals"; //	53	Pressure
                public const string hectopascals = "hectopascals"; //	133	Pressure
                public const string kilopascals = "kilopascals"; //	54	Pressure
                public const string millibars = "millibars"; //	134	Pressure
                public const string bars = "bars"; //	55	Pressure
                public const string pounds_force_per_square_inch = "pounds_force_per_square_inch"; //	56	Pressure
                public const string millimeters_of_water = "millimeters_of_water"; //	206	Pressure
                public const string centimeters_of_water = "centimeters_of_water"; //	57	Pressure
                public const string inches_of_water = "inches_of_water"; //	58	Pressure
                public const string millimeters_of_mercury = "millimeters_of_mercury"; //	59	Pressure
                public const string centimeters_of_mercury = "centimeters_of_mercury"; //	60	Pressure
                public const string inches_of_mercury = "inches_of_mercury"; //	61	Pressure
            }
            public static class Temperature
            {
                public const string degrees_celsius = "degrees_celsius"; //	62	Temperature
                public const string degrees_kelvin = "degrees_kelvin"; //	63	Temperature
                public const string degrees_kelvin_per_hour = "degrees_kelvin_per_hour"; //	181	Temperature
                public const string degrees_kelvin_per_minute = "degrees_kelvin_per_minute"; //	182	Temperature
                public const string degrees_fahrenheit = "degrees_fahrenheit"; //	64	Temperature
                public const string degree_days_celsius = "degree_days_celsius"; //	65	Temperature
                public const string degree_days_fahrenheit = "degree_days_fahrenheit"; //	66	Temperature
                public const string delta_degrees_fahrenheit = "delta_degrees_fahrenheit"; //	120	Temperature
                public const string delta_degrees_kelvin = "delta_degrees_kelvin"; //	121	Temperature
            }
            public static class Time
            {
                public const string years = "years"; //	67	Time
                public const string months = "months"; //	68	Time
                public const string weeks = "weeks"; //	69	Time
                public const string days = "days"; //	70	Time
                public const string hours = "hours"; //	71	Time
                public const string minutes = "minutes"; //	72	Time
                public const string seconds = "seconds"; //	73	Time
                public const string hundredths_seconds = "hundredths_seconds"; //	158	Time
                public const string milliseconds = "milliseconds"; //	159	Time
            }
            public static class Torque
            {
                public const string newton_meters = "newton_meters"; //	160	Torque
            }
            public static class Velocitym
            {
                public const string illimeters_per_second = "illimeters_per_second"; //	161	Velocitym
                public const string millimeters_per_minute = "millimeters_per_minute"; //	162	Velocitym
                public const string meters_per_second = "meters_per_second"; //	74	Velocitym
                public const string meters_per_minute = "meters_per_minute"; //	163	Velocitym
                public const string meters_per_hour = "meters_per_hour"; //	164	Velocitym
                public const string kilometers_per_hour = "kilometers_per_hour"; //	75	Velocitym
                public const string feet_per_second = "feet_per_second"; //	76	Velocitym
                public const string feet_per_minute = "feet_per_minute"; //	77	Velocitym
                public const string miles_per_hour = "miles_per_hour"; //	78	Velocitym
            }
            public static class Volume
            {
                public const string cubic_feet = "cubic_feet"; //	79	Volume
                public const string cubic_meters = "cubic_meters"; //	80	Volume
                public const string imperial_gallons = "imperial_gallons"; //	81	Volume
                public const string milliliters = "milliliters"; //	197	Volume
                public const string liters = "liters"; //	82	Volume
                public const string us_gallons = "us_gallons"; //	83	Volume
            }
            public static class VolumetricFlow
            {
                public const string cubic_feet_per_second = "cubic_feet_per_second"; //	142	Volumetric Flow
                public const string cubic_feet_per_minute = "cubic_feet_per_minute"; //	84	Volumetric Flow
                public const string million_standard_cubic_feet_per_minute = "million_standard_cubic_feet_per_minute"; //	254	Volumetric Flow
                public const string cubic_feet_per_hour = "cubic_feet_per_hour"; //	191	Volumetric Flow
                public const string cubic_feet_per_day = "cubic_feet_per_day"; //	248	Volumetric Flow
                public const string standard_cubic_feet_per_day = "standard_cubic_feet_per_day"; //	47808	Volumetric Flow
                public const string million_standard_cubic_feet_per_day = "million_standard_cubic_feet_per_day"; //	47809	Volumetric Flow
                public const string thousand_cubic_feet_per_day = "thousand_cubic_feet_per_day"; //	47810	Volumetric Flow
                public const string thousand_standard_cubic_feet_per_day = "thousand_standard_cubic_feet_per_day"; //	47811	Volumetric Flow
                public const string pounds_mass_per_day = "pounds_mass_per_day"; //	47812	Volumetric Flow
                public const string cubic_meters_per_second = "cubic_meters_per_second"; //	85	Volumetric Flow
                public const string cubic_meters_per_minute = "cubic_meters_per_minute"; //	165	Volumetric Flow
                public const string cubic_meters_per_hour = "cubic_meters_per_hour"; //	135	Volumetric Flow
                public const string cubic_meters_per_day = "cubic_meters_per_day"; //	249	Volumetric Flow
                public const string imperial_gallons_per_minute = "imperial_gallons_per_minute"; //	86	Volumetric Flow
                public const string milliliters_per_second = "milliliters_per_second"; //	198	Volumetric Flow
                public const string liters_per_second = "liters_per_second"; //	87	Volumetric Flow
                public const string liters_per_minute = "liters_per_minute"; //	88	Volumetric Flow
                public const string liters_per_hour = "liters_per_hour"; //	136	Volumetric Flow
                public const string us_gallons_per_minute = "us_gallons_per_minute"; //	89	Volumetric Flow
                public const string us_gallons_per_hour = "us_gallons_per_hour"; //	192	Volumetric Flow
            }
            public static class General
            {
                public const string degrees_angular = "degrees_angular"; //	90	Other
                public const string degrees_celsius_per_hour = "degrees_celsius_per_hour"; //	91	Other
                public const string degrees_celsius_per_minute = "degrees_celsius_per_minute"; //	92	Other
                public const string degrees_fahrenheit_per_hour = "degrees_fahrenheit_per_hour"; //	93	Other
                public const string degrees_fahrenheit_per_minute = "degrees_fahrenheit_per_minute"; //	94	Other
                public const string joule_seconds = "joule_seconds"; //	183	Other
                public const string kilograms_per_cubic_meter = "kilograms_per_cubic_meter"; //	186	Other
                public const string kilowatt_hours_per_square_meter = "kilowatt_hours_per_square_meter"; //	137	Other
                public const string kilowatt_hours_per_square_foot = "kilowatt_hours_per_square_foot"; //	138	Other
                public const string watt_hours_per_cubic_meter = "watt_hours_per_cubic_meter"; //	250	Other
                public const string joules_per_cubic_meter = "joules_per_cubic_meter"; //	251	Other
                public const string megajoules_per_square_meter = "megajoules_per_square_meter"; //	139	Other
                public const string megajoules_per_square_foot = "megajoules_per_square_foot"; //	140	Other
                public const string mole_percent = "mole_percent"; //	252	Other
                public const string no_units = "no_units"; //	95	Other
                public const string newton_seconds = "newton_seconds"; //	187	Other
                public const string newtons_per_meter = "newtons_per_meter"; //	188	Other
                public const string parts_per_million = "parts_per_million"; //	96	Other
                public const string parts_per_billion = "parts_per_billion"; //	97	Other
                public const string pascal_seconds = "pascal_seconds"; //	253	Other
                public const string percent = "percent"; //	98	Other
                public const string percent_obscuration_per_foot = "percent_obscuration_per_foot"; //	143	Other
                public const string percent_obscuration_per_meter = "percent_obscuration_per_meter"; //	144	Other
                public const string percent_per_second = "percent_per_second"; //	99	Other
                public const string per_minute = "per_minute"; //	100	Other
                public const string per_second = "per_second"; //	101	Other
                public const string psi_per_degree_fahrenheit = "psi_per_degree_fahrenheit"; //	102	Other
                public const string radians = "radians"; //	103	Other
                public const string radians_per_second = "radians_per_second"; //	184	Other
                public const string revolutions_per_minute = "revolutions_per_minute"; //	104	Other
                public const string square_meters_per_newton = "square_meters_per_newton"; //	185	Other
                public const string watts_per_meter_per_degree_kelvin = "watts_per_meter_per_degree_kelvin"; //	189	Other
                public const string watts_per_square_meter_degree_kelvin = "watts_per_square_meter_degree_kelvin"; //	141	Other
                public const string per_mille = "per_mille"; //	207	Other
                public const string grams_per_gram = "grams_per_gram"; //	208	Other
                public const string kilograms_per_kilogram = "kilograms_per_kilogram"; //	209	Other
                public const string grams_per_kilogram = "grams_per_kilogram"; //	210	Other
                public const string milligrams_per_gram = "milligrams_per_gram"; //	211	Other
                public const string milligrams_per_kilogram = "milligrams_per_kilogram"; //	212	Other
                public const string grams_per_milliliter = "grams_per_milliliter"; //	213	Other
                public const string grams_per_liter = "grams_per_liter"; //	214	Other
                public const string milligrams_per_liter = "milligrams_per_liter"; //	215	Other
                public const string micrograms_per_liter = "micrograms_per_liter"; //	216	Other
                public const string grams_per_cubic_meter = "grams_per_cubic_meter"; //	217	Other
                public const string milligrams_per_cubic_meter = "milligrams_per_cubic_meter"; //	218	Other
                public const string micrograms_per_cubic_meter = "micrograms_per_cubic_meter"; //	219	Other
                public const string nanograms_per_cubic_meter = "nanograms_per_cubic_meter"; //	220	Other
                public const string grams_per_cubic_centimeter = "grams_per_cubic_centimeter"; //	221	Other
                public const string becquerels = "becquerels"; //	222	Other
                public const string kilobecquerels = "kilobecquerels"; //	223	Other
                public const string megabecquerels = "megabecquerels"; //	224	Other
                public const string gray = "gray"; //	225	Other
                public const string milligray = "milligray"; //	226	Other
                public const string microgray = "microgray"; //	227	Other
                public const string sieverts = "sieverts"; //	228	Other
                public const string millisieverts = "millisieverts"; //	229	Other
                public const string microsieverts = "microsieverts"; //	230	Other
                public const string microsieverts_per_hour = "microsieverts_per_hour"; //	231	Other
                public const string millirems = "millirems"; //	47814	Other
                public const string millirems_per_hour = "millirems_per_hour"; //	47815	Other
                public const string decibels_a = "decibels_a"; //	232	Other
                public const string nephelometric_turbidity_unit = "nephelometric_turbidity_unit"; //	233	Other
                public const string pH = "pH"; //	234	Other
                public const string grams_per_square_meter = "grams_per_square_meter"; //	235	Other
                public const string minutes_per_degree_kelvin = "minutes_per_degree_kelvin"; //	236	Other
                
            }
            public const string no_unit = "no_unit";
        }
        #endregion
    }
}
