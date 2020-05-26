using System;
using System.Collections.Generic;

namespace SkiesLogFlagChanges
{
    public class SoALBitObserver : IObserver<string>
    {
        public event EventHandler<SoALBitChangedEventArgs> OnChange;
        public event EventHandler<UnhandledExceptionEventArgs> OnException;

        private const uint BITS_BASE_ADDRESS = 0x80310b3c;
        private const uint BITS_CHUNKS_COUNT = 854;

        private readonly uint[] cachedBitChunks;

        public SoALBitObserver()
        {
            cachedBitChunks = new uint[BITS_CHUNKS_COUNT];
        }

        public void OnCompleted()
        {
            // Do nothing on completion
        }

        public void OnError(Exception error)
        {
            if (OnException != null)
            {
                OnException.Invoke(this, new UnhandledExceptionEventArgs(error, false));
            }
            else
            {
                // If we have no handler for the exception, bubble it upwards so that the
                // program can crash and error to the user when something happens.
                throw error;
            }
        }

        public void OnNext(string value)
        {
            var parsedLine = DolphinMbpWriteLine.ParseString(value);

            if(parsedLine?.WriteSize == 32)
            {
                uint chunkOffset = (parsedLine.MemoryAddr - BITS_BASE_ADDRESS) / 4;

                // If the memory address is less than the flags base address, flagNum will overflow 
                // so we only need to check flagNum < FLAGS_SIZE
                if (chunkOffset >= BITS_CHUNKS_COUNT)
                {
                    return;
                }

                uint oldChunk = cachedBitChunks[chunkOffset];
                uint newChunk = parsedLine.Value;
                cachedBitChunks[chunkOffset] = newChunk;

                List<int> changedBits = BitUtils.GetListOfBits((int)(oldChunk ^ newChunk));

                var eventArgs = new SoALBitChangedEventArgs
                {
                    ChunkNumber = (int)chunkOffset
                };

                foreach(int b in changedBits)
                {
                    eventArgs.ChunkBit = b;
                    eventArgs.IsBeingSet = (newChunk & (1 << b)) != 0;

                    OnChange?.Invoke(this, eventArgs);
                }
            }
        }
    }

    public struct SoALBitChangedEventArgs
    {
        public int ChunkNumber { get; set; }
        public int ChunkBit { get; set; }
        public int BitNumber => (ChunkNumber * 32) + ChunkBit;

        public bool IsBeingSet { get; set; }
    }
}
