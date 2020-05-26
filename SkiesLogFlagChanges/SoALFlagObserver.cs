using System;

namespace SkiesLogFlagChanges
{
    public class SoALFlagObserver : IObserver<string>
    {
        public event EventHandler<SoALFlagChangedEventArgs> OnChange;
        public event EventHandler<UnhandledExceptionEventArgs> OnException;

        private const uint FLAGS_BASE_ADDRESS = 0x80310a1c;
        private const uint FLAGS_SIZE = 288;

        private readonly byte[] cachedFlags;

        public SoALFlagObserver()
        {
            cachedFlags = new byte[FLAGS_SIZE];
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

            if(parsedLine?.WriteSize == 8)
            {
                uint flagNum = parsedLine.MemoryAddr - FLAGS_BASE_ADDRESS;

                // If the memory address is less than the flags base address, flagNum will overflow 
                // so we only need to check flagNum < FLAGS_SIZE
                if (flagNum >= FLAGS_SIZE)
                {
                    return;
                }

                byte oldFlag = cachedFlags[flagNum];
                cachedFlags[flagNum] = (byte)parsedLine.Value;

                if (oldFlag != parsedLine.Value)
                {
                    var eventArgs = new SoALFlagChangedEventArgs {
                        FlagNumber = (int)flagNum,
                        OldValue = oldFlag,
                        NewValue = parsedLine.Value
                    };
                    OnChange?.Invoke(this, eventArgs);
                }
            }
        }
    }

    public struct SoALFlagChangedEventArgs
    {
        public int FlagNumber { get; set; }
        public uint OldValue { get; set; }
        public uint NewValue { get; set; }
    }
}
