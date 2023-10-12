using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.Model
{
    public static class ApplicationConfiguration
    {
        #region Defaults

        private const int HeartRateMaxDefault = 150;
        private const int HeartRateMinDefault = 50;
        private const int SpO2MaxDefault = 100;
        private const int SpO2MinDefault = 70;

        #endregion

        #region Properties

        public static int HeartRateAlarmMaximum
        {
            get
            {
                string val = string.Empty;
                Task.Run(async () =>
                {
                    val = await SecureStorage.Default.GetAsync(nameof(HeartRateAlarmMaximum));
                }).Wait();

                bool parse_success = Int32.TryParse(val, out int result);
                if (parse_success)
                {
                    return result;
                }
                else
                {
                    return HeartRateMaxDefault;
                }
            }
            set
            {
                Task.Run(async () =>
                {
                    await SecureStorage.Default.SetAsync(nameof(HeartRateAlarmMaximum), value.ToString());
                }).Wait();
            }
        }

        public static int HeartRateAlarmMinimum
        {
            get
            {
                string val = string.Empty;
                Task.Run(async () =>
                {
                    val = await SecureStorage.Default.GetAsync(nameof(HeartRateAlarmMinimum));
                }).Wait();

                bool parse_success = Int32.TryParse(val, out int result);
                if (parse_success)
                {
                    return result;
                }
                else
                {
                    return HeartRateMinDefault;
                }
            }
            set
            {
                Task.Run(async () =>
                {
                    await SecureStorage.Default.SetAsync(nameof(HeartRateAlarmMinimum), value.ToString());
                }).Wait();
            }
        }

        public static int SpO2AlarmMaximum
        {
            get
            {
                string val = string.Empty;
                Task.Run(async () =>
                {
                    val = await SecureStorage.Default.GetAsync(nameof(SpO2AlarmMaximum));
                }).Wait();

                bool parse_success = Int32.TryParse(val, out int result);
                if (parse_success)
                {
                    return result;
                }
                else
                {
                    return SpO2MaxDefault;
                }
            }
            set
            {
                Task.Run(async () =>
                {
                    await SecureStorage.Default.SetAsync(nameof(SpO2AlarmMaximum), value.ToString());
                }).Wait();
            }
        }

        public static int SpO2AlarmMinimum
        {
            get
            {
                string val = string.Empty;
                Task.Run(async () =>
                {
                    val = await SecureStorage.Default.GetAsync(nameof(SpO2AlarmMinimum));
                }).Wait();

                bool parse_success = Int32.TryParse(val, out int result);
                if (parse_success)
                {
                    return result;
                }
                else
                {
                    return SpO2MinDefault;
                }
            }
            set
            {
                Task.Run(async () =>
                {
                    await SecureStorage.Default.SetAsync(nameof(SpO2AlarmMinimum), value.ToString());
                }).Wait();
            }
        }

        #endregion

        #region Methods

        public static DateTime GetBuildDate()
        {
            try
            {
                string build_date_str = string.Empty;
                Task.Run(async () =>
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("BuildDate.txt");
                    using var reader = new StreamReader(stream);

                    build_date_str = reader.ReadToEnd();
                }).Wait();


                //Convert the build date to a DateTime object
                var build_date = DateTime.Parse(build_date_str);

                //Return it to the caller
                return build_date;
            }
            catch (Exception ex)
            {
                return DateTime.MinValue;
            }
        }

        public static DateTime GetBuildDate(Assembly assembly)
        {
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var index = value.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    value = value.Substring(index + BuildVersionMetadataPrefix.Length);
                    if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }

        #endregion
    }
}
