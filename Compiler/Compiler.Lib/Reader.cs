using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Source;

namespace Compiler.Lib
{
    internal class Reader<TBus> : Emitter<TBus> where TBus : Bus, new()
    {
        public async Task Read(string filename)
        {
            if (File.Exists(filename))
            {
                foreach (var character in File.ReadAllText(filename))
                {
                    await Emit("character", character.ToString());
                }
            }
        }
    }
}
