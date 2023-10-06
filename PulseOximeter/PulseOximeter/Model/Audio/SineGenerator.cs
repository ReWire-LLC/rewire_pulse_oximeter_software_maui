using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.Model.Audio
{
    /// <summary>
    /// Original source for this class: https://www.codeguru.com/dotnet/making-sounds-with-waves-using-c/
    /// 
    /// Changes that I have made:
    ///     1. It now takes a length in units of milliseconds, rather than in units of seconds
    ///     2. 
    /// </summary>
    public class SineGenerator
    {
        private readonly double _frequency;
        private readonly UInt32 _sampleRate;
        private readonly UInt16 _millisecondsInLength;
        private readonly UInt16 _ramp_duration_ms;
        private short[] _dataBuffer;

        public short[] Data { get { return _dataBuffer; } }

        public SineGenerator(double frequency, UInt32 sampleRate, UInt16 millisecondsInLength, UInt16 ramp_duration_ms = 0)
        {
            _frequency = frequency;
            _sampleRate = sampleRate;
            _millisecondsInLength = millisecondsInLength;
            _ramp_duration_ms = ramp_duration_ms;
            GenerateData();
        }

        private void GenerateData()
        {
            uint bufferSize = Convert.ToUInt32(Math.Round(Convert.ToDouble(_sampleRate) * (Convert.ToDouble(_millisecondsInLength) / 1000.0)));
            _dataBuffer = new short[bufferSize];

            uint ramp_size = Convert.ToUInt32(Math.Round(Convert.ToDouble(_sampleRate) * (Convert.ToDouble(_ramp_duration_ms) / 1000.0)));
            double ramp_increment_per_sample = 1;
            if (ramp_size > 0)
            {
                ramp_increment_per_sample = 1.0 / Convert.ToDouble(ramp_size);
            }

            double amplitude = 32760;
            double timePeriod = (Math.PI * 2 * _frequency) / (_sampleRate);

            for (uint index = 0; index < bufferSize - 1; index++)
            {
                double actual_amplitude = amplitude;
                if (index < ramp_size)
                {
                    actual_amplitude *= (index * ramp_increment_per_sample);
                }
                else if ((bufferSize - index) < ramp_size)
                {
                    actual_amplitude *= ((bufferSize - index) * ramp_increment_per_sample);
                }

                _dataBuffer[index] = Convert.ToInt16(actual_amplitude * Math.Sin(timePeriod * index));
                //System.Diagnostics.Debug.WriteLine("TONE VALUE: " + _dataBuffer[index].ToString());
            }
        }
    }
}
