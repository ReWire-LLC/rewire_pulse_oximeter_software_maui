using System;
using System.Collections.Generic;
using System.Linq;
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
                    await SecureStorage.Default.GetAsync(nameof(HeartRateAlarmMaximum));
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
                    await SecureStorage.Default.GetAsync(nameof(HeartRateAlarmMinimum));
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
                    await SecureStorage.Default.GetAsync(nameof(SpO2AlarmMaximum));
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
                    await SecureStorage.Default.GetAsync(nameof(SpO2AlarmMinimum));
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
    }
}
