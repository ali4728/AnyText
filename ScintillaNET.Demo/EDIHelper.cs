using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ScintillaNET.Demo
{
    public class EDIHelper
    {

        public bool IsEDIFile(string filePath)
        {
            char[] message = new char[107];
            int charCount;


            using (StreamReader reader = new StreamReader(filePath))
            {
                charCount = reader.ReadBlock(message, 0, 107);
                if (charCount < 107)
                {
                    return false;
                }
                string isa = new String(message);
                if (!isa.StartsWith("ISA"))
                {
                    return false;
                }
            }


            return true;
            
        }
        public string ParseFile(string filePath)
        {
            Delimeters del = new Delimeters(filePath);
            StringBuilder sb = new StringBuilder();

            using (StreamReader sr = new StreamReader(filePath))
            {
                EDIReader reader = new EDIReader(sr, del.SegmentDelimeter);

                bool keepReading = true;

                while (keepReading)
                {
                    //Console.WriteLine();
                    string Segment = reader.ReadSegment();

                    if (Segment == null)
                    {
                        keepReading = false;
                    }
                    else
                    {
                        sb.Append(Segment + del.SegmentDelimeter.ToString() + Environment.NewLine);
                    }
                }
            }


            return sb.ToString();
        }

        public void SaveFile(string filePath, string content)
        {
            string fnWoExt = Path.GetFileNameWithoutExtension(filePath);
            string fExt = Path.GetExtension(filePath);
            string outputpath = Path.GetDirectoryName(filePath);
            string newFileName = outputpath + "\\" + fnWoExt + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + fExt;
            File.WriteAllText(newFileName, content);
        }

        public Dictionary<long, string> SearchEDIFile(string filePath, string searchString, int maxResults = 10000)
        {
            Dictionary<long, string> results = new Dictionary<long, string>();

            if (string.IsNullOrEmpty(searchString) || !IsEDIFile(filePath))
                return results;

            Delimeters del = new Delimeters(filePath);
            char segDelim = del.SegmentDelimeter;

            int segmentNumber = 0;
            string leftover = "";
            int bufferSize = 131072; // 128KB chunks
            var utf8 = new UTF8Encoding(false);

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[bufferSize];
                int bytesRead;
                long filePosition = 0;

                while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0 && results.Count < maxResults)
                {
                    string chunk = leftover + utf8.GetString(buffer, 0, bytesRead);
                    long chunkStartOffset = filePosition - leftover.Length;

                    string[] parts = chunk.Split(segDelim);

                    long offsetInChunk = 0;
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        string rawSeg = parts[i];
                        string seg = rawSeg.Replace("\r", "").Replace("\n", "");

                        if (seg.Length > 0)
                        {
                            segmentNumber++;
                            if (seg.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                string sample = seg.Length > 200 ? seg.Substring(0, 200) : seg;
                                long byteOffset = chunkStartOffset + offsetInChunk;
                                if (!results.ContainsKey(byteOffset))
                                    results.Add(byteOffset, " Seg:" + segmentNumber + "  " + sample);
                                if (results.Count >= maxResults) break;
                            }
                        }

                        offsetInChunk += rawSeg.Length + 1; // +1 for delimiter
                    }

                    leftover = parts[parts.Length - 1];
                    filePosition += bytesRead;
                }

                // Handle final segment without trailing delimiter
                if (leftover.Length > 0 && results.Count < maxResults)
                {
                    string seg = leftover.Replace("\r", "").Replace("\n", "");
                    if (seg.Length > 0)
                    {
                        segmentNumber++;
                        if (seg.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string sample = seg.Length > 200 ? seg.Substring(0, 200) : seg;
                            long byteOffset = filePosition - leftover.Length;
                            if (!results.ContainsKey(byteOffset))
                                results.Add(byteOffset, " Seg:" + segmentNumber + "  " + sample);
                        }
                    }
                }
            }

            Console.WriteLine(String.Format("EDI Search Count:{0:n0} Total Segments:{1:n0}", results.Count, segmentNumber));
            return results;
        }

        public string ParseString(string textValue, string filePath)
        {
            Delimeters del = new Delimeters(filePath);
            StringBuilder sb = new StringBuilder();

            byte[] byteArray = Encoding.UTF8.GetBytes(textValue);

            using (StreamReader sr = new StreamReader(new MemoryStream(byteArray)))
            {
                EDIReader reader = new EDIReader(sr, del.SegmentDelimeter);

                bool keepReading = true;

                while (keepReading)
                {
                    //Console.WriteLine();
                    string Segment = reader.ReadSegment();

                    if (Segment == null)
                    {
                        keepReading = false;
                    }
                    else
                    {
                        sb.Append(Segment + del.SegmentDelimeter.ToString() + Environment.NewLine);
                    }
                }
            }


            return sb.ToString();
        }
    }

    public class EDIReader
    {
        private char segDelim;
        private StreamReader sr;

        public EDIReader(StreamReader pSr, char pSegDelim)
        {
            sr = pSr;
            segDelim = pSegDelim;
        }


        public String ReadSegment()
        {
            char curChar;
            var sb = new StringBuilder();
            while (sr.Peek() >= 0)
            {
                if (sr.Peek() == 13) { sr.Read(); } //advance CR
                if (sr.Peek() == 10) { sr.Read(); } //advance LF

                curChar = (char)sr.Read();
                if (curChar == segDelim)
                {
                    if (sb.Length > 0)
                    {
                        return sb.ToString();
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    sb.Append(curChar);
                }
            }
            return null;
        }
    }


    public class Delimeters
    {
        public char SegmentDelimeter { get; set; }
        public char ElementDelimeter { get; set; }
        public char ComponentDelimeter { get; set; }
        public char RepetitionDelimeter { get; set; }


        public Delimeters(string inputFile)
        {

            char[] message = new char[107];
            int charCount;


            using (StreamReader reader = new StreamReader(inputFile))
            {
                charCount = reader.ReadBlock(message, 0, 107);
                //Console.WriteLine("charCount:" + charCount);
            }

            RepetitionDelimeter = message[82];
            ElementDelimeter = message[103];
            ComponentDelimeter = message[104];
            SegmentDelimeter = message[105];




        }

    }
}
