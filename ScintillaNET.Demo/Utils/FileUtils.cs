using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ScintillaNET.Demo.Utils
{
    public class FileUtils
    {
        public static string CurFileName = "";
        public static long fileSize = 0;
        public static bool fileHasLineBreaks = false;
        public static int GCTrigger = 0;
        //public static int bytesPerPage = 100000;

        public static int[] LineOffsetIndex = null;
        private static int LineCheckpointInterval = 65536; // 64KB

        public static void BuildLineIndex(string path)
        {
            LineOffsetIndex = null;
            if (!File.Exists(path)) return;
            long fileSz = new FileInfo(path).Length;
            int numCheckpoints = (int)(fileSz / LineCheckpointInterval) + 1;
            int[] index = new int[numCheckpoints];
            int lineCount = 1;
            int checkpointIdx = 0;
            index[0] = 1;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[LineCheckpointInterval];
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, LineCheckpointInterval)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        if (buffer[i] == 10) lineCount++;
                    }
                    checkpointIdx++;
                    if (checkpointIdx < numCheckpoints)
                    {
                        index[checkpointIdx] = lineCount;
                    }
                }
            }
            LineOffsetIndex = index;
            Console.WriteLine(String.Format("Line index built: {0:n0} checkpoints, {1:n0} total lines", numCheckpoints, lineCount));
        }

        public static int GetLineNumberAtOffset(string path, long offset)
        {
            if (offset == 0) return 1;
            if (LineOffsetIndex == null || LineOffsetIndex.Length == 0) return 1;
            int checkpointIdx = (int)(offset / LineCheckpointInterval);
            if (checkpointIdx >= LineOffsetIndex.Length)
                checkpointIdx = LineOffsetIndex.Length - 1;
            int lineNumber = LineOffsetIndex[checkpointIdx];
            long checkpointOffset = (long)checkpointIdx * LineCheckpointInterval;
            if (checkpointOffset < offset && File.Exists(path))
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(checkpointOffset, SeekOrigin.Begin);
                    long remaining = offset - checkpointOffset;
                    byte[] buffer = new byte[(int)Math.Min(remaining, LineCheckpointInterval)];
                    while (remaining > 0)
                    {
                        int toRead = (int)Math.Min(remaining, buffer.Length);
                        int bytesRead = fs.Read(buffer, 0, toRead);
                        if (bytesRead == 0) break;
                        for (int i = 0; i < bytesRead; i++)
                        {
                            if (buffer[i] == 10) lineNumber++;
                        }
                        remaining -= bytesRead;
                    }
                }
            }
            return lineNumber;
        }

        public static string readAllFile(string path)
        {
            return File.ReadAllText(path);

        }

        public static void UpdateClip()
        {
            string name = !string.IsNullOrEmpty(OriginalFileName) ? OriginalFileName : CurFileName;
            Clipboard.SetText(name);
        }
        public static string readNBites(string path, int limit, int offset)
        {
            string str = "";
            if (File.Exists(path))
            {
                Console.WriteLine(String.Format("START read byte limit:{0:n0} offset:{1:n0}", limit, offset));
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    
                    offset = (offset * limit);
                    if (limit > ((int)fs.Length))
                    {
                        limit = (int)fs.Length;
                    }
                    //if (fs.Length < (offset + limit))
                    //{
                    //    offset = (int)(fs.Length - limit);
                    //}
                    if (offset > (fs.Length - limit))
                    {
                        limit = (int)(fs.Length - offset);                     
                    }

                    byte[] bytes = new byte[limit];
                    Console.WriteLine(String.Format("END read byte limit:{0:n0} offset:{1:n0}", limit, offset));
                    fs.Seek(offset, 0);
                    int n = fs.Read(bytes, 0, limit);                    

                    if (n > 0)
                    {
                        var utf8NoBom = new UTF8Encoding(false);
                        str = utf8NoBom.GetString(bytes, 0, n);

                        // Strip UTF-8 BOM character if present on first page
                        if (offset == 0 && str.Length > 0 && str[0] == '\uFEFF')
                        {
                            str = str.Substring(1);
                        }

                        #region check_if_line_numbers_exist
                        if (offset == 0) //if reading first junk of file check if file has line numbers
                        {
                            string searchString = "\r\n";
                            int searchCounter = 0;
                            int maxSearchCounter = 10;
                            if (!String.IsNullOrEmpty(str))
                            {
                                int idx = -1;
                                int strIdx = 0;
                                while ((idx = str.IndexOf(searchString, strIdx)) > -1)
                                {
                                    if (searchCounter < maxSearchCounter)
                                    {
                                        strIdx = idx + 1;                                        
                                        searchCounter++;
                                    }
                                    else
                                    {
                                        break;
                                    }

                                }
                            }

                            if (searchCounter > 1) //should have at least  two lines
                            {
                                FileUtils.fileHasLineBreaks = true;
                                Console.WriteLine("File has " + searchCounter.ToString() + " linebreaks");
                            }
                        }
                        #endregion

                    }
                }
            }

            return str;
        }

        public static string getFixWidth(string str, int width)
        {
            char[] ary = str.ToCharArray();
            StringBuilder sb = new StringBuilder();
            int cnt = 0;
            foreach (char item in ary)
            {
                if (item == 10 || item == 13) { continue; }
                cnt++;
                sb.Append(item);
                if ((cnt % width) == 0)
                {
                    sb.Append(Environment.NewLine);
                }
            }


            return sb.ToString();
        }


        public static string UnWrapXML(string textValue)
        {
            if (string.IsNullOrEmpty(textValue))
                return textValue;

            var sb = new StringBuilder(textValue.Length + (textValue.Length / 4));
            int indent = 0;
            int i = 0;
            int len = textValue.Length;

            while (i < len)
            {
                if (textValue[i] == '<')
                {
                    int tagEnd = textValue.IndexOf('>', i);
                    if (tagEnd == -1)
                    {
                        sb.Append(textValue, i, len - i);
                        break;
                    }

                    string tag = textValue.Substring(i, tagEnd - i + 1);

                    bool isClosing = tag.Length > 1 && tag[1] == '/';
                    bool isSelfClosing = tag.Length > 1 && tag[tag.Length - 2] == '/';
                    bool isDeclaration = tag.Length > 1 && (tag[1] == '?' || tag[1] == '!');

                    if (isClosing)
                    {
                        indent = Math.Max(0, indent - 1);
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(Environment.NewLine);
                    }

                    for (int t = 0; t < indent; t++)
                    {
                        sb.Append("  ");
                    }

                    sb.Append(tag);

                    if (!isClosing && !isSelfClosing && !isDeclaration)
                    {
                        indent++;
                    }

                    i = tagEnd + 1;
                }
                else
                {
                    int nextTag = textValue.IndexOf('<', i);
                    if (nextTag == -1)
                    {
                        sb.Append(textValue, i, len - i);
                        break;
                    }

                    string text = textValue.Substring(i, nextTag - i).Trim();
                    if (text.Length > 0)
                    {
                        sb.Append(text);
                    }

                    i = nextTag;
                }
            }

            return sb.ToString();
        }


        public static string LastTempZipDir = "";
        public static string OriginalFileName = "";
        public static string LastTempEdiDir = "";

        public static bool IsZipFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            return path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        }

        public static string ExtractZipToTemp(string zipPath)
        {
            CleanupTempZipDir();

            string tempDir = Path.Combine(Path.GetTempPath(), "EDIViewer_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            Type shellAppType = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(shellAppType);
            dynamic srcFolder = shell.NameSpace(zipPath);
            dynamic destFolder = shell.NameSpace(tempDir);

            if (srcFolder == null)
                throw new InvalidOperationException("Could not open ZIP archive: " + zipPath);

            dynamic items = srcFolder.Items();
            int expectedCount = items.Count;
            // 4 = No progress dialog, 16 = Yes to All, 512 = No confirm dir, 1024 = No error UI
            destFolder.CopyHere(items, 4 | 16 | 512 | 1024);

            // Shell32 CopyHere is async; poll until files appear or timeout
            int timeout = 30000;
            int elapsed = 0;
            while (elapsed < timeout)
            {
                Thread.Sleep(200);
                elapsed += 200;
                string[] files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
                if (files.Length >= expectedCount)
                {
                    Thread.Sleep(300);
                    break;
                }
            }

            LastTempZipDir = tempDir;
            Console.WriteLine("Extracted ZIP to: " + tempDir);
            return tempDir;
        }

        public static void CleanupTempZipDir()
        {
            if (!string.IsNullOrEmpty(LastTempZipDir) && Directory.Exists(LastTempZipDir))
            {
                try
                {
                    Directory.Delete(LastTempZipDir, true);
                    Console.WriteLine("Cleaned up temp dir: " + LastTempZipDir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to clean temp dir: " + ex.Message);
                }
                LastTempZipDir = "";
            }
        }

        public static string UnwrapEdiToTempFile(string sourcePath, char segmentDelimiter)
        {
            CleanupTempEdiDir();

            string tempDir = Path.Combine(Path.GetTempPath(), "EDIViewer_EDI_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string tempFile = Path.Combine(tempDir, Path.GetFileName(sourcePath));

            int bufferSize = 131072; // 128KB

            using (StreamReader reader = new StreamReader(sourcePath))
            using (StreamWriter writer = new StreamWriter(tempFile, false, new UTF8Encoding(false), 65536))
            {
                char[] buffer = new char[bufferSize];
                int charsRead;

                while ((charsRead = reader.Read(buffer, 0, bufferSize)) > 0)
                {
                    for (int i = 0; i < charsRead; i++)
                    {
                        char c = buffer[i];

                        // Skip existing line breaks
                        if (c == '\r' || c == '\n')
                            continue;

                        writer.Write(c);

                        // After segment delimiter, insert newline
                        if (c == segmentDelimiter)
                        {
                            writer.Write(Environment.NewLine);
                        }
                    }
                }
            }

            LastTempEdiDir = tempDir;
            Console.WriteLine("Unwrapped EDI to temp: " + tempFile);
            return tempFile;
        }

        public static void CleanupTempEdiDir()
        {
            if (!string.IsNullOrEmpty(LastTempEdiDir) && Directory.Exists(LastTempEdiDir))
            {
                try
                {
                    Directory.Delete(LastTempEdiDir, true);
                    Console.WriteLine("Cleaned up temp EDI dir: " + LastTempEdiDir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to clean temp EDI dir: " + ex.Message);
                }
                LastTempEdiDir = "";
            }
        }

        public static Dictionary<long, string> SearchFile(string path, string searchString)
        {
            Dictionary<long, string> loc = new Dictionary<long, string>();

            int offset = 0;
            int limit = 100000;
            int maxSearchCounter = 10000; //10K limit search results for memory protection
            int searchCounter = 0;
            if (File.Exists(path))
            {                
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    bool keepReading = true;

                    while (keepReading && (searchCounter < maxSearchCounter))
                    {                        
                        if (limit > ((int)fs.Length))
                        {
                            limit = (int)fs.Length;
                        }
                        if (offset > (fs.Length - limit))
                        {
                            limit = (int)(fs.Length - offset);
                            keepReading = false;
                        }

                        byte[] bytes = new byte[limit];
                        Console.WriteLine(String.Format("SEARCH read byte size:{0:n0} offset:{1:n0} fs.Len:{2:n0}", limit, offset, fs.Length));

                        fs.Seek(offset, 0);
                        int n = fs.Read(bytes, 0, limit);


                        if (n > 0)
                        {
                            var utf8NoBom = new UTF8Encoding(false);
                            string str = utf8NoBom.GetString(bytes, 0, n);

                            if (!String.IsNullOrEmpty(str))
                            {
                                ////Line numbers
                                char[] charAry = str.ToCharArray();
                                Dictionary<int, int> lineDict = new Dictionary<int, int>();
                                int lineCounter = 0;

                                for (int i = 0; i < charAry.Length; i++)
                                {

                                    char achar = charAry[i];
                                    if (achar == '\n')
                                    {
                                        lineCounter++;
                                        lineDict.Add(i, lineCounter);
                                    }
                                }



                                int idx = -1;
                                int strIdx = 0;

                                while ((idx = str.IndexOf(searchString, strIdx, StringComparison.OrdinalIgnoreCase)) > -1)
                                {
                                    if (searchCounter < maxSearchCounter)
                                    {
                                        int curLineNumber = -1;
                                        for (int i = idx; i > 0; i--)
                                        {
                                            if (charAry[i] == '\n')
                                            {
                                                curLineNumber = i;
                                                break;
                                            }
                                        }

                                        strIdx = idx + 1;
                                        int lineMatch = 1;                                  

                                        if (lineDict.ContainsKey(curLineNumber))
                                        {
                                            lineMatch = lineDict[curLineNumber] + 1;
                                        }

                                        int lineStart = (curLineNumber >= 0) ? curLineNumber + 1 : 0;
                                        int lineEnd = str.IndexOf('\n', idx);
                                        if (lineEnd == -1) lineEnd = str.Length;
                                        string lineText = str.Substring(lineStart, Math.Min(lineEnd - lineStart, 200)).Trim();

                                        loc.Add((long)(idx + offset), " Line:" + lineMatch.ToString() + "  " + lineText);

                                        searchCounter++;
                                    }
                                    else 
                                    {
                                        break;
                                    }

                                }
                            }


                        }

                        offset = offset + limit;

                    }
                }
            }

            Console.WriteLine(String.Format("Search Count:{0:n0}", searchCounter));

            return loc;

        }

        public static Dictionary<long, string> SearchDisplayedText(string displayedText, string searchString)
        {
            Dictionary<long, string> loc = new Dictionary<long, string>();

            if (string.IsNullOrEmpty(displayedText) || string.IsNullOrEmpty(searchString))
                return loc;

            int maxSearchCounter = 10000;
            int searchCounter = 0;
            int strIdx = 0;
            int idx;

            string[] lines = displayedText.Split('\n');
            int[] lineStartOffsets = new int[lines.Length];
            int runningOffset = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                lineStartOffsets[i] = runningOffset;
                runningOffset += lines[i].Length + 1;
            }

            while ((idx = displayedText.IndexOf(searchString, strIdx, StringComparison.OrdinalIgnoreCase)) > -1)
            {
                if (searchCounter >= maxSearchCounter)
                    break;

                int lineNumber = 1;
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    if (idx >= lineStartOffsets[i])
                    {
                        lineNumber = i + 1;
                        break;
                    }
                }

                string lineText = (lineNumber <= lines.Length) ? lines[lineNumber - 1].Trim() : "";
                if (lineText.Length > 200)
                    lineText = lineText.Substring(0, 200);

                loc.Add((long)idx, " Line:" + lineNumber.ToString() + "  " + lineText);

                strIdx = idx + 1;
                searchCounter++;
            }

            Console.WriteLine(String.Format("Search Displayed Count:{0:n0}", searchCounter));

            return loc;
        }
    }
}
