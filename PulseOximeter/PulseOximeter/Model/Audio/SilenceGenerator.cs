using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.Model.Audio
{
    public class SilenceGenerator
    {
        private readonly UInt32 _sampleRate;
        private readonly UInt16 _millisecondsInLength;
        private short[] _dataBuffer;

        public short[] Data { get { return _dataBuffer; } }

        public SilenceGenerator(UInt32 sampleRate, UInt16 millisecondsInLength)
        {
            _sampleRate = sampleRate;
            _millisecondsInLength = millisecondsInLength;
            GenerateData();
        }

        private void GenerateData()
        {
            int bufferSize = Convert.ToInt32(Math.Round(Convert.ToDouble(_sampleRate) * (Convert.ToDouble(_millisecondsInLength) / 1000.0)));
            _dataBuffer = Enumerable.Repeat<short>(0, bufferSize).ToArray();
        }
    }
}
