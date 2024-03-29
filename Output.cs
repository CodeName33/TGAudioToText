using System;
using System.Collections.Generic;
using System.Text;

namespace TGInstaAudioToText
{
    public static class Output
    {
        static Object Locker = new Object();
        public static ConsoleColor TextDefault
        {
            get
            {
                return ConsoleColor.White;
            }
        }

        public static ConsoleColor TextSuccess
        {
            get
            {
                return ConsoleColor.Green;
            }
        }

        public static ConsoleColor TextWarning
        {
            get
            {
                return ConsoleColor.Yellow;
            }
        }

        public static ConsoleColor TextError
        {
            get
            {
                return ConsoleColor.Red;
            }
        }

        public static ConsoleColor TextInfo
        {
            get
            {
                return ConsoleColor.DarkCyan;
            }
        }

        public static ConsoleColor TextComment
        {
            get
            {
                return ConsoleColor.DarkGray;
            }
        }

        public static string NewLineIfNeed
        {
            get
            {
                return "\0";
            }
        }
        public static void Write(object v, ConsoleColor textColor = ConsoleColor.Gray, ConsoleColor backColor = ConsoleColor.Black)
        {
            lock (Locker)
            {
                var oldTextColor = Console.ForegroundColor;
                var oldBackColor = Console.BackgroundColor;

                string vs = v.ToString();

                if (vs.StartsWith(NewLineIfNeed))
                {
                    vs = vs[NewLineIfNeed.Length..];
                    if (Console.CursorLeft > 0)
                    {
                        Console.WriteLine();
                    }
                }

                if (Console.CursorLeft == 0)
                {
                    Console.ForegroundColor = TextComment;
                    Console.Write($"{Sys.TimeDefaultFormat(DateTime.Now)}  ");
                }

                Console.ForegroundColor = textColor;
                Console.BackgroundColor = backColor;

                Console.Write(v.ToString());

                Console.ForegroundColor = oldTextColor;
                Console.BackgroundColor = oldBackColor;
            }
        }

        public static void WriteLine(object v, ConsoleColor textColor = ConsoleColor.Gray, ConsoleColor backColor = ConsoleColor.Black)
        {
            Write($"{v}{Environment.NewLine}", textColor, backColor);
        }
    }
}
