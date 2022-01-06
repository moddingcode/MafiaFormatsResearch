using System.Collections.Generic;
using System.IO;
using MafiaResearch.Mafia2.Navigation.NavData;
using NUnit.Framework;
using Utils.Extensions;

namespace TestSandbox.Mafia2.Navigation.NavData
{
    public class NavAiWorldDataTests
    {
        // IMPORTANT: extract content of all SDS using the "Unpack All SDS" toolkit feature
        private const string GameDir = "E:\\Games\\Mafia II LH\\pc";
        private const string WorkingDir = "E:\\temp\\maftests\\nav_obj_data";
        
        [Test]
        public void TestMethodReadWrite()
        {
            string path = $"{GameDir}\\sds\\city\\extracted\\greenfield.sds\\NAV_AIWORLD_DATA_312.nav";
            string outPath = $"{WorkingDir}\\greenfield_out_NAV_AIWORLD_DATA_312.nav";
            using (FileStream fileStream = File.Open(path, FileMode.Open))
            {
                var navAiWorldData = new NavAiWorldData(fileStream);

                using (FileStream outFileStream = File.Open(outPath, FileMode.Create))
                {
                    navAiWorldData.Write(outFileStream);
                }
            }
        }
        
        [Test]
        public void TestMethodReadWriteAll()
        {
            string[] files = Directory.GetFiles(GameDir, "*.nav", SearchOption.AllDirectories);
            List<string> csvLines = new List<string>();

            foreach (var file in files)
            {

                using (FileStream inFileStream = File.Open(file, FileMode.Open))
                {
                    inFileStream.Seek(0, SeekOrigin.Begin);
                    MemoryStream inMemoryStream = new MemoryStream();
                    inFileStream.CopyTo(inMemoryStream);
                    inMemoryStream.Seek(0, SeekOrigin.Begin);

                    inFileStream.Seek(0, SeekOrigin.Begin);
                    var navAiWorldData = new NavAiWorldData(inFileStream);

                    MemoryStream outMemoryStream = new MemoryStream();
                    navAiWorldData.Write(outMemoryStream);
                    outMemoryStream.Seek(0, SeekOrigin.Begin);

                    bool sizeEquals = inMemoryStream.Length == outMemoryStream.Length;
                    string bytesEqual = "false,###";
                    if (sizeEquals)
                    {
                        byte i;
                        byte j;

                        bool bEq = true;
                        long offset = -1;
                        for (var k = 0; k < inMemoryStream.Length; k++)
                        {
                            i = inMemoryStream.ReadByte8();
                            j = outMemoryStream.ReadByte8();

                            if (i != j)
                            {
                                bEq = false;
                                offset = inMemoryStream.Position;
                                break;
                            }
                        }

                        bytesEqual = $"{bEq},{offset}";
                    }


                    csvLines.Add($"{file},{sizeEquals},{bytesEqual}");
                }
            }
            File.WriteAllLines($"{WorkingDir}\\nav_aiworld_data_read_write_compare_01.txt", csvLines);
        }
    }
}