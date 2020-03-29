using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;

namespace Cache
{
    class Program
    {
        private static int writeCount = 0;
        private static DateTime writeTime = DateTime.Now.Subtract(TimeSpan.FromDays(1));
        private static char[] loadChars = new char[] { '|', '/', '-', '\\' };
        private static long lastTotalByteRead = 0;
        private static bool waitInput = false;
        private static int readBufferSize = 1024 * 1024 * 8; //8 MByte

        static void Main(string[] args)
        {
            bool clearCache = false;
            bool showHelp = false;

            OptionSet argsOptions = new OptionSet() {
                { "c|clear_cache", "clear standby memory before caching the listed files.", v => clearCache= v != null },
                { "w|wait_input", "before terminating waits an user's input.", v => waitInput= v != null },
                { "h|help", "show this message and exit", v => showHelp = v != null },
            };

            List<string> files;
            try
            {
                files = argsOptions.Parse(args);
                foreach (string fileOrDir in files)
                {
                    if (!Directory.Exists(fileOrDir) && !File.Exists(fileOrDir))
                    {
                        throw new IOException(String.Format("\"{0}\" is nor a file or a directory", fileOrDir));
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write("cache: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `cache --help' for more information.");
                WaitInput();
                return;
            }
            if (showHelp)
            {
                Console.WriteLine("Usage: cache [OPTIONS] [file1/directory1] [file2/directory2] [...");
                Console.WriteLine("The listed files or directory will be read and cached in ram (if enough).");
                Console.WriteLine();
                Console.WriteLine("Options:");
                argsOptions.WriteOptionDescriptions(Console.Out);
                WaitInput();
                return;
            }
            if (clearCache)
            {
                try
                {
                    Console.Write("Clearing cache... ");
                    MemUtils.ClearFileSystemCache(true);
                    Console.WriteLine("Done.");
                }
                catch
                {
                    Console.WriteLine("Permission denied. Skipped.");
                }
            }

            byte[] readBuffer = new byte[readBufferSize];
            foreach (string fileOrDir in files)
            {
                long totalByte = 0;
                int filesCount = 0;
                Console.Write("Analizing {0} ... ", fileOrDir);
                foreach (string file in IOHelper.Files(fileOrDir))
                {
                    totalByte += (new FileInfo(file)).Length;
                    filesCount++;
                }
                Console.WriteLine("Total: {1} files, {0:0.0} GB", totalByte / (1024.0 * 1024 * 1024), filesCount);
                Console.WriteLine("Caching file in ram...");
                long totalByteRead = 0;
                DateTime startTime = DateTime.Now;
                foreach (string file in IOHelper.Files(fileOrDir))
                {
                    FileStream fileReader = null;
                    try
                    {
                        fileReader = File.OpenRead(file);
                        while (fileReader.Position != fileReader.Length)
                        {
                            totalByteRead += fileReader.Read(readBuffer, 0, readBuffer.Length);
                            WriteProgress(totalByte, totalByteRead);
                        }
                    }
                    catch
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error while opening {0} (skipped)", file);
                        Console.ResetColor();
                        totalByte -= (new FileInfo(file)).Length;
                    }
                    finally
                    {
                        if(fileReader != null)
                        {
                            fileReader.Close();
                            fileReader.Dispose();
                        }
                    }
                   
                }
                WriteProgressBar(Console.BufferWidth, 1);
                Console.WriteLine("\nDone {0:0.0} GB in {1:0.0} seconds => {2:0.0} MB/s.\n",
                    totalByte / (1024.0 * 1024 * 1024),
                    (DateTime.Now - startTime).TotalSeconds,
                    totalByte / (1024 * 1024) / (DateTime.Now - startTime).TotalSeconds);
            }
            WaitInput();
        }

        static void WriteProgress(long totalByte, long totalByteRead)
        {
            if (DateTime.Now - writeTime > TimeSpan.FromSeconds(0.5))
            {
                double p = ((double)totalByteRead) / totalByte;
                string info = String.Format(" {0:0.0} MB/s {1}",
                    ((totalByteRead - lastTotalByteRead) / (1024 * 1024)) / (DateTime.Now - writeTime).TotalSeconds,
                    loadChars[writeCount % loadChars.Length]);

                WriteProgressBar(Console.BufferWidth - info.Length, p);
                Console.Write(info);
                writeCount++;
                writeTime = DateTime.Now;
                lastTotalByteRead = totalByteRead;
            }
        }

        static void WriteProgressBar(int maxSize, double progress)
        {
            progress = Math.Min(1, Math.Max(0, progress));
            maxSize = maxSize - 3;
            int toWrite = (int)(maxSize * progress + 0.5);
            Console.CursorLeft = 0;
            Console.Write("[");
            Console.Write(new string('=', toWrite));
            Console.Write(new string(' ', maxSize - toWrite));
            Console.Write("]");
        }
        private static void WaitInput()
        {
            if (waitInput)
            {
                Console.ReadKey();
            }
        }
    }
}
