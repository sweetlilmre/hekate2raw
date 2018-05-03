using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hekate2raw
{
  class Program
  {
    private string[] EMMC_Parts = new string[]
    {
      "gpt_prefix.bin",
      "gpt_suffix.bin",
      "PRODINFO",
      "PRODINFOF",
      "BCPKG2-1-Normal-Main",
      "BCPKG2-2-Normal-Sub",
      "BCPKG2-3-SafeMode-Main",
      "BCPKG2-4-SafeMode-Sub",
      "BCPKG2-5-Repair-Main",
      "BCPKG2-6-Repair-Sub",
      "SAFE",
      "SYSTEM",
    };

    private void dd(FileStream outFile, FileStream inFile, long skip, long seek, long count, int bs)
    {
      int left = Console.CursorLeft;
      int top = Console.CursorTop;
      Console.CursorVisible = false;

      byte[] buffer = new byte[bs];
      outFile.Seek(seek * bs, SeekOrigin.Begin);
      for (int i = 0; i < count; i++)
      {
        Array.Clear(buffer, 0, bs);
        inFile.Read(buffer, 0, bs);
        outFile.Write(buffer, 0, bs);
        Console.WriteLine("{0}%", i*100 / count);
        Console.CursorLeft = left;
        Console.CursorTop = top;
      }
    }

    private void Prep(FileStream outFile, string inFileName, long skip, long seek, long count, int bs)
    {
      Console.WriteLine("Adding: {0}", inFileName);
      using (FileStream inFile = File.OpenRead(inFileName))
      {
        dd(outFile, inFile, skip, seek, count, bs);

      }
    }

    void Run()
    {
      Console.WriteLine("hekate2raw");
      Console.WriteLine("based off of rajkosto's script");
      bool incomplete = false;
      foreach (var emmcPart in EMMC_Parts)
      {
        if (!File.Exists(emmcPart))
        {
          Console.WriteLine("Missing file part: {0}", emmcPart);
          incomplete = true;
        }
      }

      if (incomplete)
      {
        return;
      }

      using (FileStream outFile = File.Create("rawdump.bin"))
      {
        Prep(outFile, "gpt_prefix.bin", 0, 0, 34, 512);
        Prep(outFile, "PRODINFO", 0, 34, 8158, 512);
        Prep(outFile, "PRODINFOF", 0, 256, 256, 16384);
        Prep(outFile, "BCPKG2-1-Normal-Main", 0, 512, 512, 16384);
        Prep(outFile, "BCPKG2-2-Normal-Sub", 0, 1024, 512, 16384);
        Prep(outFile, "BCPKG2-3-SafeMode-Main", 0, 1536, 512, 16384);
        Prep(outFile, "BCPKG2-4-SafeMode-Sub", 0, 2048, 512, 16384);
        Prep(outFile, "BCPKG2-5-Repair-Main", 0, 2560, 512, 16384);
        Prep(outFile, "BCPKG2-6-Repair-Sub", 0, 3072, 512, 16384);
        Prep(outFile, "SAFE", 0, 3584, 4096, 16384);
        Prep(outFile, "SYSTEM", 0, 7680, 163840, 16384);

        long userStartSeek = 171520;
        long userCurrSeek = userStartSeek;
        long fileSizeInBlocks = 0;
        if (File.Exists("USER"))
        {
          fileSizeInBlocks = 1703936;
          Prep(outFile, "USER", 0, userCurrSeek, fileSizeInBlocks, 16384);
          userCurrSeek += fileSizeInBlocks;
        }
        else
        {
          int i = 0;
          string fileName = string.Format("USER.{0}", i);
          while (File.Exists(fileName))
          {
            fileSizeInBlocks = new FileInfo(fileName).Length / 16384;
            Prep(outFile, fileName, 0, userCurrSeek, fileSizeInBlocks, 16384);
            userCurrSeek = userCurrSeek + fileSizeInBlocks;
            i++;
          }
        }
        if (userCurrSeek != userStartSeek)
        {
          Prep(outFile, "gpt_suffix.bin", 0, 61071327, 33, 512);
        }
      }
    }

    static void Main(string[] args)
    {
      Program p = new Program();
      p.Run();
    }
  }
}
