using System.Collections.Generic;
using System.IO;
using MafiaResearch.Mafia2.Navigation.NavData;
using NUnit.Framework;
using Utils.Extensions;

namespace TestSandbox.Mafia2.Navigation.NavData
{
    
    public class NavObjDataTests
    {
        // IMPORTANT: extract content of all SDS using the "Unpack All SDS" toolkit feature
        private const string GameDir = "E:\\Games\\Mafia II LH\\pc";
        private const string WorkingDir = "E:\\temp\\maftests\\nav_obj_data";
       
        [Test]
        public void TestMethodReadWrite307()
        {
            string path = $"{GameDir}\\sds\\city\\extracted\\greenfield.sds\\NAV_OBJ_DATA_307.nov";
            string outPath = $"{WorkingDir}\\greenfield_out_NAV_OBJ_DATA_307.nov";
            using (FileStream fileStream = File.Open(path, FileMode.Open))
            {
                var navObjData = new NavObjData(fileStream);

                using (FileStream outFileStream = File.Open(outPath, FileMode.Create))
                {
                    navObjData.Write(outFileStream);
                }
            }
        }

        [Test]
        public void TestMethodReadWriteAll()
        {
            string[] files = Directory.GetFiles(GameDir, "*.nov", SearchOption.AllDirectories);
            List<string> csvLines = new List<string>();

            foreach (var file in files)
            {
                using (FileStream inFileStream = File.Open(file, FileMode.Open))
                {
                    inFileStream.Seek(0, SeekOrigin.Begin);
                    MemoryStream inMemoryStream = new MemoryStream();
                    inFileStream.CopyTo(inMemoryStream);
                    
                    // -- fix for 4 "garbage" random bytes
                    inMemoryStream.Seek(-8, SeekOrigin.End);
                    uint stringLength = inMemoryStream.ReadUInt32(false);
                    inMemoryStream.Seek(-8 - stringLength - 8, SeekOrigin.End);
                    uint aiMeshLength = inMemoryStream.ReadUInt32(false);
                    inMemoryStream.Seek(-8 - stringLength - 8 - aiMeshLength, SeekOrigin.End);
                    var aiMeshOffset = inMemoryStream.Position;
                    inMemoryStream.Seek(-8 - stringLength - 8 - aiMeshLength + 76, SeekOrigin.End);
                    uint aiMeshUnk15Offset = inMemoryStream.ReadUInt32(false);
                    if (aiMeshUnk15Offset != 0xFFFFFFFF)
                    {
                        inMemoryStream.Seek(aiMeshOffset + aiMeshUnk15Offset - 4, SeekOrigin.Begin);
                        inMemoryStream.Write((uint)0, false);
                    }
                    else
                    {
                        inMemoryStream.Seek(-8 - stringLength - 8 - 4, SeekOrigin.End);
                        inMemoryStream.Write((uint)0, false);;
                    }
                    // -- end of fix for 4 "garbage" random bytes
                    
                    inMemoryStream.Seek(0, SeekOrigin.Begin);


                    inFileStream.Seek(0, SeekOrigin.Begin);
                    var navObjData = new NavObjData(inFileStream);

                    MemoryStream outMemoryStream = new MemoryStream();
                    navObjData.Write(outMemoryStream);
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
            File.WriteAllLines($"{WorkingDir}\\nav_obj_data_read_write_compare_01.txt", csvLines);
        }
    }
}