using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Source;

namespace Compiler.Lib
{
    /// <summary>
    /// A simple emitter that reads a file and emits it as events
    /// </summary>
    /// <typeparam name="TBus"></typeparam>
    internal class Reader<TBus> : Emitter<TBus> where TBus : Bus, new()
    {
        /// <summary>
        /// Reads a file into memory then emits the characters as an event
        /// </summary>
        /// <param name="filename">The filename to read</param>
        /// <returns></returns>
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
