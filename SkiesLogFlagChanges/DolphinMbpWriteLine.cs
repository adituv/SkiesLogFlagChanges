using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkiesLogFlagChanges
{
    public sealed class DolphinMbpWriteLine
    {
        public uint FuncAddr { get; }
        public string FuncName { get; }
        public int WriteSize { get; }
        public uint Value { get; }
        public uint MemoryAddr { get; }

        private static readonly Regex parseRegex;

        static DolphinMbpWriteLine()
        {
            parseRegex = new Regex(@"^\d\d:\d\d:\d\d\d core\\powerpc\\breakpoints.cpp:239 N\[MI\]: MBP (?<funcaddr>[0-9a-f]{8}) \((?<funcname>[^)]+)\) Write(?<writesize>8|16|32) (?<value>[0-9a-f]{2,8}) at (?<flagaddr>[0-9a-f]{8})");
        }

        private DolphinMbpWriteLine(uint funcAddr, string funcName, int writeSize, uint value, uint memoryAddr)
        {
            this.FuncAddr = funcAddr;
            this.FuncName = funcName;
            this.WriteSize = writeSize;
            this.Value = value;
            this.MemoryAddr = memoryAddr;
        }

        public static DolphinMbpWriteLine ParseString(string line)
        {
            Match m = parseRegex.Match(line);

            DolphinMbpWriteLine result = null;

            if (m.Success)
            {
                var funcaddr = uint.Parse(m.Groups["funcaddr"].Value,System.Globalization.NumberStyles.HexNumber);
                var funcname = m.Groups["funcname"].Value;
                var writesize = int.Parse(m.Groups["writesize"].Value);
                var value = uint.Parse(m.Groups["value"].Value, System.Globalization.NumberStyles.HexNumber);
                var memaddr = uint.Parse(m.Groups["flagaddr"].Value, System.Globalization.NumberStyles.HexNumber);

                result = new DolphinMbpWriteLine(funcaddr, funcname, writesize, value, memaddr);
            }

            return result;
        }
    }
}
