using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MugBot.Code
{
    public static class Util
    {

        public static void LogList(List<string> listToLog)
        {
            foreach (var s in listToLog)
            {
                Console.WriteLine(s);
            }
            Console.WriteLine("");
        }
    }
}