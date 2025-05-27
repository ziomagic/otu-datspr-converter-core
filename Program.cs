using System;
using System.Diagnostics;

namespace OpenTibiaUnity
{
    static class Program
    {
        public const int SEGMENT_DIMENTION = 1024;

        static void Main(string[] args)
        {

            Console.WriteLine("Loading version: {0}", 840);

            Stopwatch watch = new Stopwatch();
            watch.Start();

            Core.Converter.IConverter converter = new Core.Converter.LegacyConverter(840);
            var task = converter.BeginProcessing();
            task.Wait();

            watch.Stop();

            double seconds = watch.ElapsedMilliseconds / (double)1000;
            Console.WriteLine("Time elapsed: " + seconds + " seconds.");
        }
    }
}
