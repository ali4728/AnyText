using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ScintillaNET.Demo
{
    public sealed class X12FileContext
    {
        public char ElementDelimiter { get; private set; }
        public char SegmentDelimiter { get; private set; }
        public string TransactionId { get; internal set; }
        public string ImplementationVersion { get; internal set; }

        public X12FileContext(char elementDelimiter, char segmentDelimiter)
        {
            ElementDelimiter = elementDelimiter;
            SegmentDelimiter = segmentDelimiter;
            TransactionId = "";
            ImplementationVersion = "";
        }
    }

    public class EDIHelper
    {

        public bool IsEDIFile(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                return ReadIsa(reader) != null;
            }
        }

        private static char[] ReadIsa(StreamReader reader)
        {
            char[] isa = new char[106];
            if (reader.ReadBlock(isa, 0, isa.Length) != isa.Length ||
                isa[0] != 'I' || isa[1] != 'S' || isa[2] != 'A' ||
                isa[3] != isa[103] || isa[3] == isa[105] ||
                char.IsWhiteSpace(isa[3]) || char.IsWhiteSpace(isa[105]))
                return null;
            return isa;
        }

        public X12FileContext ReadFileContext(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                char[] isa = ReadIsa(reader);
                if (isa == null)
                    return null;

                X12FileContext context = new X12FileContext(isa[103], isa[105]);
                char[] prefix = new char[65536];
                int length = reader.ReadBlock(prefix, 0, prefix.Length);
                string[] segments = new string(prefix, 0, length).Split(context.SegmentDelimiter);
                string gsVersion = "";

                for (int i = 0; i < segments.Length - 1; i++)
                {
                    string[] elements = segments[i].TrimStart('\r', '\n', ' ').Split(context.ElementDelimiter);
                    if (string.Equals(elements[0], "GS", StringComparison.OrdinalIgnoreCase) && elements.Length > 8)
                        gsVersion = elements[8].Trim();
                    else if (string.Equals(elements[0], "ST", StringComparison.OrdinalIgnoreCase))
                    {
                        if (elements.Length > 1)
                            context.TransactionId = elements[1].Trim();
                        context.ImplementationVersion = elements.Length > 3 && !string.IsNullOrEmpty(elements[3])
                            ? elements[3].Trim() : gsVersion;
                        break;
                    }
                }
                return context;
            }
        }

        public bool Is277CaFile(string filePath)
        {
            X12FileContext context = ReadFileContext(filePath);
            return context != null && context.TransactionId == "277" &&
                context.ImplementationVersion.IndexOf("X214", StringComparison.OrdinalIgnoreCase) >= 0;
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
                                    results.Add(byteOffset, " Line:" + segmentNumber + "  " + sample);
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
                                results.Add(byteOffset, " Line:" + segmentNumber + "  " + sample);
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
