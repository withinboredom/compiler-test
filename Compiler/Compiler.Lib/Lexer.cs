using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Source;

namespace Compiler.Lib
{
    public class Lexer<TBus> : Aggregate<TBus> where TBus : Bus, new()
    {
        public async Task Lex(string filename)
        {
            var reader = new Reader<TBus>();
            Attach(reader);

            await reader.Read(filename);
        }

        internal string currentToken;
        internal string currentState;

        private void Init()
        {
            currentToken = "";
            currentState = "data";
            ListenTo.Add("character");
            ListenTo.Add("tokenize");
            ListenTo.Add("token");
        }

        internal void character(Event ev)
        {
            //Console.Write(ev.Value);
            currentToken += ev.Value;
            Emit("tokenize", currentToken);
        }

        internal void token(Event ev)
        {
            var token = ev.Value as Dictionary<string, string>;

            Console.WriteLine($"<{token["type"]}>{token["value"]}</{token["type"]}>");
        }

        internal void tokenize(Event ev)
        {
            var token = ev.Value as string;

            if (string.IsNullOrEmpty(token)) return;

            var cleanToken = token.Trim();

            var endsWith = token[token.Length - 1];
            var endsWithWhite = char.IsWhiteSpace(endsWith);

            switch (currentState)
            {
                case "data":
                    if (endsWith == ':')
                    {
                        currentState = "creatingIdentifier";
                        break;
                    }

                    if (endsWith == '{')
                    {
                        Emit("token", new Dictionary<string, string>()
                        {
                            { "kind", "control" },
                            { "value", "beginBlock" },
                            { "type", "control" }
                        });
                        currentToken = string.Empty;
                        break;
                    }

                    if (endsWith == '}')
                    {
                        Emit("token", new Dictionary<string, string>()
                        {
                            { "kind", "control" },
                            { "value", "endBlock" },
                            { "type", "control" }
                        });
                        currentToken = string.Empty;
                        break;
                    }

                    if (endsWith == '[')
                    {
                        currentState = "kind";
                        break;
                    }

                    break;
                case "creatingIdentifier":
                    if (endsWithWhite)
                    {
                        var ids = cleanToken.Split(':');
                        Emit("token", new Dictionary<string, string>()
                        {
                            { "kind", "identifier" },
                            { "value", ids[0].Trim() },
                            { "type", ids[1] }
                        });
                        currentState = "data";
                        currentToken = string.Empty;
                    }
                    break;
                case "kind":
                    if (endsWith == ']')
                    {
                        var kind = cleanToken.Remove(cleanToken.Length - 1).Substring(1);
                    }
                    break;
            }
        }

        public Lexer() : base()
        {
            Init();
        }

        public Lexer(Guid id) : base(id)
        {
            Init();
        }
    }
}
