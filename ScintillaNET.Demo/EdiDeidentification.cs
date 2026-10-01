using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ScintillaNET.Demo
{
    internal sealed class EdiDeidentification
    {
        private sealed class Rule
        {
            internal string Segment;
            internal int Element;
            internal int QualifierElement;
            internal string Qualifier;
            internal string Scope;
            internal string Replacement;
            internal int Component;
        }

        private sealed class TransactionRules
        {
            internal string Id;
            internal string Version;
            internal readonly List<Rule> Rules = new List<Rule>();
        }

        private readonly List<TransactionRules> transactions = new List<TransactionRules>();
        private const int MaxSegmentBytes = 1048576;
        private static readonly Encoding ByteEncoding = Encoding.GetEncoding(28591);

        internal static string ConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "edi-deidentification.xml"); }
        }

        internal static EdiDeidentification Load()
        {
            if (File.Exists(ConfigPath))
            {
                using (FileStream file = File.OpenRead(ConfigPath))
                {
                    if (file.Length > 1048576)
                        throw new InvalidDataException("De-identification configuration exceeds 1 MB.");
                    return Parse(file);
                }
            }
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("ScintillaNET.Demo.edi-deidentification.xml"))
            {
                if (resource == null)
                    throw new InvalidOperationException("Bundled de-identification rules are missing.");
                return Parse(resource);
            }
        }

        private static EdiDeidentification Parse(Stream stream)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Prohibit;
            settings.XmlResolver = null;
            XDocument document;
            using (XmlReader reader = XmlReader.Create(stream, settings))
                document = XDocument.Load(reader);
            if (document.Root == null || document.Root.Name != "EdiDeidentification")
                throw new FormatException("Expected EdiDeidentification root element.");
            EdiDeidentification config = new EdiDeidentification();
            foreach (XElement transaction in document.Root.Elements())
            {
                if (transaction.Name != "Transaction")
                    throw new FormatException("Expected Transaction under EdiDeidentification.");
                TransactionRules selected = new TransactionRules();
                selected.Id = (string)transaction.Attribute("id");
                selected.Version = (string)transaction.Attribute("version");
                if (selected.Id == null || selected.Id.Length != 3 ||
                    !char.IsDigit(selected.Id[0]) || !char.IsDigit(selected.Id[1]) || !char.IsDigit(selected.Id[2]) ||
                    string.IsNullOrEmpty(selected.Version))
                    throw new FormatException("Each de-identification Transaction needs a three-digit id and nonempty version.");
                foreach (TransactionRules existing in config.transactions)
                    if (existing.Id == selected.Id && string.Equals(existing.Version, selected.Version, StringComparison.OrdinalIgnoreCase))
                        throw new FormatException("Duplicate de-identification Transaction: " + selected.Id + " " + selected.Version);
                foreach (XElement node in transaction.Elements())
                {
                    if (node.Name != "Replace")
                        throw new FormatException("Unexpected de-identification rule: " + node.Name);
                    Rule rule = new Rule();
                    rule.Segment = (string)node.Attribute("segment");
                    rule.Scope = (string)node.Attribute("scope");
                    rule.Qualifier = (string)node.Attribute("qualifier");
                    rule.Replacement = (string)node.Attribute("with");
                    if (string.IsNullOrEmpty(rule.Segment) || rule.Segment.Length < 2 || rule.Segment.Length > 3 ||
                        (rule.Scope != "Person" && rule.Scope != "Claim") || rule.Replacement == null ||
                        !int.TryParse((string)node.Attribute("element"), NumberStyles.None, CultureInfo.InvariantCulture, out rule.Element) ||
                        rule.Element < 1 || rule.Element > 99 ||
                        !int.TryParse((string)node.Attribute("qualifierElement") ?? "0", NumberStyles.None, CultureInfo.InvariantCulture, out rule.QualifierElement) ||
                        rule.QualifierElement < 0 || rule.QualifierElement > 99 ||
                        (rule.QualifierElement == 0) != (rule.Qualifier == null) ||
                        !int.TryParse((string)node.Attribute("component") ?? "0", NumberStyles.None, CultureInfo.InvariantCulture, out rule.Component) ||
                        rule.Component < 0 || rule.Component > 99)
                        throw new FormatException("Invalid de-identification rule for " + rule.Segment + ".");
                    foreach (char c in rule.Segment)
                        if (!char.IsLetterOrDigit(c) || c > 127)
                            throw new FormatException("Invalid segment ID: " + rule.Segment);
                    if (rule.Replacement.Length > 256 || (rule.Qualifier != null && rule.Qualifier.Length == 0))
                        throw new FormatException("Unsafe replacement or qualifier in " + rule.Segment + ".");
                    foreach (char c in rule.Replacement)
                        if (c < 32 || c > 126)
                            throw new FormatException("Replacement must contain printable ASCII only.");
                    selected.Rules.Add(rule);
                }
                if (selected.Rules.Count == 0)
                    throw new FormatException("De-identification Transaction " + selected.Id + " " + selected.Version + " has no Replace rules.");
                config.transactions.Add(selected);
            }
            if (config.transactions.Count == 0)
                throw new FormatException("At least one de-identification Transaction is required.");
            return config;
        }

        private List<Rule> SelectRules(X12FileContext context)
        {
            if (context == null || context.TransactionId != "837" || string.IsNullOrEmpty(context.ImplementationVersion))
                throw new InvalidOperationException("Only 837I (X223) and 837P (X222) files are supported.");
            TransactionRules selected = null;
            foreach (TransactionRules candidate in transactions)
            {
                if (candidate.Id != context.TransactionId ||
                    context.ImplementationVersion.IndexOf(candidate.Version, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (selected == null || candidate.Version.Length > selected.Version.Length)
                    selected = candidate;
            }
            if (selected == null)
                throw new InvalidOperationException("No de-identification rules match transaction " +
                    context.TransactionId + " version " + context.ImplementationVersion + ".");
            return selected.Rules;
        }

        internal void Process(string sourcePath, string outputPath, X12FileContext context)
        {
            if (context == null || context.TransactionId != "837" ||
                (context.ImplementationVersion.IndexOf("X223", StringComparison.OrdinalIgnoreCase) < 0 &&
                 context.ImplementationVersion.IndexOf("X222", StringComparison.OrdinalIgnoreCase) < 0))
                throw new InvalidOperationException("Only 837I (X223) and 837P (X222) files are supported.");
            List<Rule> rules = SelectRules(context);
            if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The output must not overwrite the source.");
            foreach (Rule rule in rules)
                if (rule.Replacement.IndexOfAny(new char[] { context.ElementDelimiter, context.SegmentDelimiter, context.ComponentDelimiter }) >= 0)
                    throw new FormatException("A replacement contains an EDI delimiter: " + rule.Segment);

            bool created = false;
            try
            {
                using (FileStream input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
                using (FileStream output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536))
                {
                    created = true;
                    byte[] buffer = new byte[65536];
                    using (MemoryStream segment = new MemoryStream())
                    {
                        bool personLoop = false;
                        bool personDetails = false;
                        int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            int start = 0;
                            for (int i = 0; i < read; i++)
                            {
                                if (buffer[i] != (byte)context.SegmentDelimiter) continue;
                                if (i > start) segment.Write(buffer, start, i - start);
                                if (segment.Length > MaxSegmentBytes)
                                    throw new InvalidDataException("EDI segment exceeds 1 MB.");
                                WriteSegment(segment, output, context, rules, ref personLoop, ref personDetails);
                                output.WriteByte(buffer[i]);
                                segment.SetLength(0);
                                start = i + 1;
                            }
                            if (start < read) segment.Write(buffer, start, read - start);
                            if (segment.Length > MaxSegmentBytes)
                                throw new InvalidDataException("EDI segment exceeds 1 MB.");
                        }
                        if (segment.Length > 0)
                            WriteSegment(segment, output, context, rules, ref personLoop, ref personDetails);
                    }
                }
            }
            catch
            {
                if (created && File.Exists(outputPath)) File.Delete(outputPath);
                throw;
            }
        }

        private void WriteSegment(MemoryStream segment, Stream output, X12FileContext context, List<Rule> rules, ref bool personLoop, ref bool personDetails)
        {
            string text = ByteEncoding.GetString(segment.GetBuffer(), 0, (int)segment.Length);
            int first = 0;
            while (first < text.Length && char.IsWhiteSpace(text[first])) first++;
            int idEnd = text.IndexOf(context.ElementDelimiter, first);
            if (idEnd < 0)
            {
                segment.WriteTo(output);
                return;
            }
            string id = text.Substring(first, idEnd - first);
            string[] fields = text.Substring(first, text.Length - first).Split(context.ElementDelimiter);
            if (id == "HL")
            {
                personLoop = fields.Length > 3 && (fields[3] == "22" || fields[3] == "23");
                personDetails = false;
            }
            else if (id == "ST" || id == "SE" || id == "GS" || id == "GE" || id == "IEA")
            {
                if (id == "ST" && (fields.Length < 2 || fields[1] != "837"))
                    throw new InvalidDataException("Mixed or unsupported transactions cannot be de-identified.");
                personLoop = false;
                personDetails = false;
            }
            else if (id == "NM1")
                personDetails = personLoop && fields.Length > 1 && (fields[1] == "IL" || fields[1] == "QC");

            bool changed = false;
            foreach (Rule rule in rules)
            {
                if (rule.Segment != id || !personLoop || (rule.Scope == "Person" && !personDetails) ||
                    rule.Element >= fields.Length || string.IsNullOrEmpty(fields[rule.Element]) ||
                    (rule.QualifierElement > 0 && (rule.QualifierElement >= fields.Length || fields[rule.QualifierElement] != rule.Qualifier)))
                    continue;
                if (rule.Component == 0)
                    fields[rule.Element] = rule.Replacement;
                else
                {
                    string[] parts = fields[rule.Element].Split(context.ComponentDelimiter);
                    if (rule.Component > parts.Length || parts[rule.Component - 1].Length == 0) continue;
                    parts[rule.Component - 1] = rule.Replacement;
                    fields[rule.Element] = string.Join(context.ComponentDelimiter.ToString(), parts);
                }
                changed = true;
            }
            if (!changed)
                segment.WriteTo(output);
            else
            {
                byte[] bytes = ByteEncoding.GetBytes(text.Substring(0, first) + string.Join(context.ElementDelimiter.ToString(), fields));
                output.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
