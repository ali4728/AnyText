using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
        public Color BoundaryColor { get; private set; }
        public Color HighlightColor { get; private set; }

        public EdiHighlightRule(string segment, int element, string value, bool isBoundary, bool highlightSegment, bool highlightValue, Color boundaryColor, Color highlightColor)
        {
            Segment = segment;
            Element = element;
            Value = value;
            IsBoundary = isBoundary;
            HighlightSegment = highlightSegment;
            HighlightValue = highlightValue;
            BoundaryColor = boundaryColor;
            HighlightColor = highlightColor;
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
        public Color DefaultBoundaryColor { get; private set; }
        public Color DefaultHighlightColor { get; private set; }

        public EdiTransactionRules(string id, string version, string name, IList<EdiHighlightRule> rules, Color defaultBoundaryColor, Color defaultHighlightColor)
        {
            Id = id;
            Version = version;
            Name = name;
            Rules = rules;
            DefaultBoundaryColor = defaultBoundaryColor;
            DefaultHighlightColor = defaultHighlightColor;
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
            Color defaultBoundaryColor = ReadColor(document.Root, "defaultBoundaryColor", Color.FromArgb(200, 225, 255));
            Color defaultHighlightColor = ReadColor(document.Root, "defaultHighlightColor", Color.Red);
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

                Color transactionBoundaryColor = ReadColor(transaction, "defaultBoundaryColor", defaultBoundaryColor);
                Color transactionHighlightColor = ReadColor(transaction, "defaultHighlightColor", defaultHighlightColor);
                IList<EdiHighlightRule> rules = new List<EdiHighlightRule>();
                HashSet<Color> boundaryColors = new HashSet<Color>();
                HashSet<Color> highlightColors = new HashSet<Color>();
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
                    Color ruleBoundaryColor = boundary ? ReadColor(element, "color", transactionBoundaryColor) : transactionBoundaryColor;
                    Color ruleHighlightColor = ReadColor(element, "color", transactionHighlightColor);
                    if (boundary)
                        boundaryColors.Add(ruleBoundaryColor);
                    if (markSegment || markValue)
                        highlightColors.Add(ruleHighlightColor);
                    rules.Add(new EdiHighlightRule(segment, index, value, boundary, markSegment, markValue, ruleBoundaryColor, ruleHighlightColor));
                }
                if (boundaryColors.Count > 6 || highlightColors.Count > 11)
                    throw new FormatException("Transaction " + id + " exceeds the limit of 6 boundary or 11 highlight colors.");
                transactions.Add(new EdiTransactionRules(id, version, (string)transaction.Attribute("name") ?? id, rules, transactionBoundaryColor, transactionHighlightColor));
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

        private static Color ReadColor(XElement element, string name, Color inherited)
        {
            string text = (string)element.Attribute(name);
            if (text == null)
                return inherited;
            text = text.Trim();
            if (text.Length == 7 && text[0] == '#')
            {
                int rgb;
                if (int.TryParse(text.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
                    return Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
            }
            else
            {
                Color named = Color.FromName(text);
                if (named.IsKnownColor && named != Color.Transparent)
                    return named;
            }
            throw new FormatException("Invalid " + name + " color: " + text + ". Use #RRGGBB or a named color.");
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
