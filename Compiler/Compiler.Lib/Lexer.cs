using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Source;

namespace Compiler.Lib
{
    /// <summary>
    /// Converts a file to tokens to be parsed
    /// </summary>
    /// <typeparam name="TBus"></typeparam>
    public class Lexer<TBus> : Aggregate<TBus> where TBus : Bus, new()
    {
        /// <summary>
        /// A trigger to read a file
        /// </summary>
        /// <param name="filename">The filename to read</param>
        /// <returns></returns>
        public async Task Lex(string filename)
        {
            var reader = new Reader<TBus>();
            Attach(reader);

            await reader.Read(filename);
        }

        internal string currentToken;
        internal string currentState;

        /// <summary>
        /// Initializes the state tree
        /// </summary>
        protected override void Init()
        {
            Tree = new Hashtable()
            {
                { "data", new Hashtable()
                {
                    { "currentToken", "" },
                    { "currentState", "data" }
                } },
                { "on", new Hashtable()
                {
                    { "character", new List<Hashtable> {
                        new Hashtable()
                    {
                        { "call", "character" },
                        { "emits", new List<string>
                        {
                            "tokenize"
                        } },
                        { "state", new Hashtable()
                        {
                            { "reads", new List<string>()
                            {
                                "currentToken"
                            } },
                            { "writes", new List<string>() }
                        } }
                    } } },
                    { "tokenize", new List<Hashtable> {
                        new Hashtable()
                    {
                        { "call", "tokenize" },
                        { "emits", new List<string>()
                        {
                            "token"
                        } },
                        { "state", new Hashtable()
                        {
                            { "reads", new List<string>()
                            {
                                "currentState"
                            } },
                            { "writes", new List<string>()
                            {
                                "currentToken",
                                "currentState"
                            } }
                        } }
                    } }},
                    { "token", new List<Hashtable> {
                        new Hashtable()
                    {
                        { "call", "token" },
                        { "emits" , new List<string>() },
                        { "state", new Hashtable()
                        {
                            { "reads", new List<string>() },
                            { "writes", new List<string>() }
                        } }
                    } }}
                } }
            };

            currentToken = "";
            currentState = "data";
        }

        /// <summary>
        /// Appends a character to the internal state and fires an event to tokenize the state
        /// </summary>
        /// <param name="ev"></param>
        internal void Character(Event ev)
        {
            //Console.Write(ev.Value);
            currentToken += ev.Value;
            Emit("tokenize", currentToken);
        }

        /// <summary>
        /// Outputs a token
        /// </summary>
        /// <param name="ev"></param>
        internal void Token(Event ev)
        {
            var token = ev.Value as Dictionary<string, string>;

            Console.WriteLine($"<{token["kind"]}>{token["value"]}</{token["type"]}>");
        }

        /// <summary>
        /// Takes the current state and tries to deduce a token from it
        /// </summary>
        /// <param name="ev"></param>
        internal void Tokenize(Event ev)
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
    }
}
