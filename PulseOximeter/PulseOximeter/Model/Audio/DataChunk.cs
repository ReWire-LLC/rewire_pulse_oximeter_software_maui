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
    public class DataChunk
    {
        private const string CHUNK_ID = "data";

        public string ChunkId { get; private set; } = CHUNK_ID;
        public UInt32 ChunkSize { get; set; } = 0;
        public List<short> WaveData { get; private set; } = new List<short>();

        public DataChunk()
        {
            ChunkId = CHUNK_ID;
            ChunkSize = 0;  // Until we add some data
        }

        public UInt32 Length()
        {
            return (UInt32)GetBytes().Length;
        }

        public byte[] GetBytes()
        {
            List<Byte> chunkBytes = new List<Byte>();

            chunkBytes.AddRange(Encoding.ASCII.GetBytes(ChunkId));
            chunkBytes.AddRange(BitConverter.GetBytes(ChunkSize));
            byte[] bufferBytes = new byte[WaveData.Count * 2];
            Buffer.BlockCopy(WaveData.ToArray(), 0, bufferBytes, 0, bufferBytes.Length);
            chunkBytes.AddRange(bufferBytes.ToList());

            return chunkBytes.ToArray();
        }

        public void AddSampleData(short[] leftBuffer, short[] rightBuffer)
        {
            short[] new_wave_data = new short[leftBuffer.Length + rightBuffer.Length];
            int bufferOffset = 0;
            for (int index = 0; index < new_wave_data.Length; index += 2)
            {
                new_wave_data[index] = leftBuffer[bufferOffset];
                new_wave_data[index + 1] = rightBuffer[bufferOffset];
                bufferOffset++;
            }

            WaveData.AddRange(new_wave_data);
            ChunkSize = (UInt32)WaveData.Count * 2;
        }
    }
}
