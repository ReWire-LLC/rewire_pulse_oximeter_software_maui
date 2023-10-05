using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.Model.Audio
{
    /// <summary>
    /// Source for this class: https://www.codeguru.com/dotnet/making-sounds-with-waves-using-c/
    /// </summary>
    public class SineGenerator
    {
        private readonly double _frequency;
        private readonly UInt32 _sampleRate;
        private readonly UInt16 _millisecondsInLength;
        private short[] _dataBuffer;

        public short[] Data { get { return _dataBuffer; } }

        public SineGenerator(double frequency,
           UInt32 sampleRate, UInt16 millisecondsInLength)
        {
            _frequency = frequency;
            _sampleRate = sampleRate;
            _millisecondsInLength = millisecondsInLength;
            GenerateData();
        }

        private void GenerateData()
        {
            uint bufferSize = Convert.ToUInt32(Math.Round(Convert.ToDouble(_sampleRate) * (Convert.ToDouble(_millisecondsInLength) / 1000.0)));
            _dataBuffer = new short[bufferSize];

            int amplitude = 32760;

            double timePeriod = (Math.PI * 2 * _frequency) /
               (_sampleRate);

            for (uint index = 0; index < bufferSize - 1; index++)
            {
                _dataBuffer[index] = Convert.ToInt16(amplitude *
                   Math.Sin(timePeriod * index));
            }
        }
    }
}
