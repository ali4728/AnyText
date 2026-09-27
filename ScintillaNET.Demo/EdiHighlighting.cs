using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace ScintillaNET.Demo
{
    public sealed class EdiHighlightRule
    {
        public string Segment { get; private set; }
        public int Element { get; private set; }
        public string Value { get; private set; }
        public bool IsBoundary { get; private set; }
        public bool HighlightSegment { get; private set; }
        public bool HighlightValue { get; private set; }

        public EdiHighlightRule(string segment, int element, string value, bool isBoundary, bool highlightSegment, bool highlightValue)
        {
            Segment = segment;
            Element = element;
            Value = value;
            IsBoundary = isBoundary;
            HighlightSegment = highlightSegment;
            HighlightValue = highlightValue;
        }

        public bool Matches(string line, char elementDelimiter, char segmentDelimiter, out int valueStart, out int valueLength)
        {
            valueStart = 0;
            valueLength = 0;
            if (line.Length <= Segment.Length ||
                !line.StartsWith(Segment, StringComparison.OrdinalIgnoreCase) ||
                (line[Segment.Length] != elementDelimiter && line[Segment.Length] != segmentDelimiter))
                return false;

            if (Element == 0)
                return true;

            int position = Segment.Length;
            for (int i = 1; i <= Element; i++)
            {
                if (position >= line.Length || line[position] != elementDelimiter)
                    return false;
                position++;
                if (i < Element)
                {
                    int next = line.IndexOf(elementDelimiter, position);
                    int segmentEnd = line.IndexOf(segmentDelimiter, position);
                    if (next < 0 || (segmentEnd >= 0 && segmentEnd < next))
                        return false;
                    position = next;
                }
            }

            int end = line.IndexOfAny(new char[] { elementDelimiter, segmentDelimiter, '\r', '\n' }, position);
            if (end < 0)
                end = line.Length;
            valueStart = position;
            valueLength = end - position;
            return string.Equals(line.Substring(position, valueLength), Value, StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class EdiTransactionRules
    {
        public string Id { get; private set; }
        public string Version { get; private set; }
        public string Name { get; private set; }
        public IList<EdiHighlightRule> Rules { get; private set; }

        public EdiTransactionRules(string id, string version, string name, IList<EdiHighlightRule> rules)
        {
            Id = id;
            Version = version;
            Name = name;
            Rules = rules;
        }
    }

    public sealed class EdiHighlightConfiguration
    {
        private const string DefaultResource = "ScintillaNET.Demo.edi-highlighting.xml";
        private readonly IList<EdiTransactionRules> transactions;

        private EdiHighlightConfiguration(IList<EdiTransactionRules> transactions)
        {
            this.transactions = transactions;
        }

        public static string ConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "edi-highlighting.xml"); }
        }

        public static EdiHighlightConfiguration Load(out string warning)
        {
            warning = null;
            try
            {
                string path = ConfigPath;
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (file.Length > 1048576)
                        throw new InvalidDataException("Highlight configuration exceeds 1 MB.");
                    return Parse(file);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is XmlException || ex is FormatException)
            {
                warning = "Could not load EDI highlighting configuration (" + ConfigPath + "): " + ex.Message + ". Using built-in rules.";
                using (Stream resource = OpenDefaults())
                    return Parse(resource);
            }
        }

        private static Stream OpenDefaults()
        {
            Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultResource);
            if (resource == null)
                throw new InvalidOperationException("Bundled EDI highlighting rules are missing.");
            return resource;
        }

        private static EdiHighlightConfiguration Parse(Stream stream)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Prohibit;
            settings.XmlResolver = null;
            XDocument document;
            using (XmlReader reader = XmlReader.Create(stream, settings))
                document = XDocument.Load(reader);

            if (document.Root == null || document.Root.Name != "EdiHighlighting")
                throw new FormatException("Expected an EdiHighlighting root element.");
            IList<EdiTransactionRules> transactions = new List<EdiTransactionRules>();
            foreach (XElement transaction in document.Root.Elements())
            {
                if (transaction.Name != "Transaction")
                    throw new FormatException("Unexpected configuration element: " + transaction.Name);
                string id = (string)transaction.Attribute("id");
                string version = (string)transaction.Attribute("version") ?? "";
                if (id == null || id.Length != 3 || !char.IsDigit(id[0]) || !char.IsDigit(id[1]) || !char.IsDigit(id[2]))
                    throw new FormatException("Transaction id must be a three-digit ST01 value.");
                foreach (EdiTransactionRules existing in transactions)
                    if (existing.Id == id && string.Equals(existing.Version, version, StringComparison.OrdinalIgnoreCase))
                        throw new FormatException("Duplicate transaction rule: " + id + " " + version);

                IList<EdiHighlightRule> rules = new List<EdiHighlightRule>();
                foreach (XElement element in transaction.Elements())
                {
                    bool boundary = element.Name == "Boundary";
                    if (!boundary && element.Name != "Highlight")
                        throw new FormatException("Unexpected rule element: " + element.Name);
                    string segment = (string)element.Attribute("segment");
                    if (string.IsNullOrEmpty(segment) || segment.Length < 2 || segment.Length > 3)
                        throw new FormatException("Each rule needs a two- or three-character segment ID.");
                    foreach (char c in segment)
                        if (!char.IsLetterOrDigit(c) || c > 127)
                            throw new FormatException("Invalid segment ID: " + segment);
                    int index = 0;
                    string indexText = (string)element.Attribute("element");
                    string value = (string)element.Attribute("value");
                    if ((indexText == null) != (value == null) ||
                        (indexText != null && (!int.TryParse(indexText, out index) || index < 1 || index > 99 || string.IsNullOrEmpty(value))))
                        throw new FormatException("A matched element requires both a positive element number and a value.");
                    bool markSegment = ReadBool(element, "highlightSegment", !boundary);
                    bool markValue = ReadBool(element, "highlightValue", false);
                    if (markValue && index == 0)
                        throw new FormatException("highlightValue requires an element match.");
                    rules.Add(new EdiHighlightRule(segment, index, value, boundary, markSegment, markValue));
                }
                transactions.Add(new EdiTransactionRules(id, version, (string)transaction.Attribute("name") ?? id, rules));
            }
            if (transactions.Count == 0)
                throw new FormatException("At least one transaction rule is required.");
            return new EdiHighlightConfiguration(transactions);
        }

        private static bool ReadBool(XElement element, string name, bool defaultValue)
        {
            string text = (string)element.Attribute(name);
            bool value;
            if (text == null)
                return defaultValue;
            if (!bool.TryParse(text, out value))
                throw new FormatException("Invalid " + name + " value: " + text);
            return value;
        }

        public EdiTransactionRules Select(X12FileContext context)
        {
            if (context == null || string.IsNullOrEmpty(context.TransactionId))
                return null;
            EdiTransactionRules selected = null;
            foreach (EdiTransactionRules candidate in transactions)
            {
                if (candidate.Id != context.TransactionId ||
                    (candidate.Version.Length != 0 && context.ImplementationVersion.IndexOf(candidate.Version, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;
                if (selected == null || candidate.Version.Length > selected.Version.Length)
                    selected = candidate;
            }
            return selected;
        }
    }
}
