using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Compiler.Lib;
using Source;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var lex = new Lexer<Bus>();
            lex.Lex("test.txt").GetAwaiter().GetResult();
            Console.ReadKey(false);
        }
    }
}
