# EDI / XML / Text Viewer

A fast, lightweight Windows desktop viewer designed to open and navigate **very large files** (500 MB – 2 GB) that most text editors struggle with or refuse to open.

---

## What It Does

- **Opens huge files instantly** — instead of loading the entire file into memory, the viewer reads only a small portion at a time (a "page"), so even multi-gigabyte files open in seconds.
- **Automatically formats single-line files** — many EDI and XML files are stored as one extremely long line. The viewer detects this and reformats the content with proper line breaks so you can actually read it.
- **Page through the file** — use the `<<` and `>>` buttons to move forward and backward through the file, page by page.
- **Search the entire file** — find any text across the whole file, not just what's currently visible. Click a result to jump directly to that location.
- **Count occurrences** — quickly count how many times a term appears in the entire file.
- **Open ZIP files** — open `.zip` archives directly. If the ZIP contains one file it opens automatically; if multiple, you pick which one.

---

## Supported File Types

| File Type | What happens on open |
|---|---|
| **EDI (X12)** — `.edi`, `.txt`, `.837`, `.834`, etc. | Detected automatically by file content. Single-line files are reformatted with one segment per line for readability. |
| **XML** — `.xml` | Minified (single-line) XML is automatically pretty-printed with proper indentation. Works even with malformed XML. |
| **ZIP** — `.zip` | Extracted to a temporary folder. The contained file is then opened normally. |
| **Any text file** | Opened as-is with paging support. |

---

## How to Use

### Opening a File
- **File → Open** or drag and drop a file onto the window.

### Navigating Pages
- `^` — Jump to a specific page number (enter the page number in the text box first)
- `<<` — Previous page
- `>>` — Next page
- **MaxBytes** — Controls how much data is loaded per page (default: 100,000 bytes). Increase for more content per page, decrease for faster loading.

### Searching
- Type your search term in the text box (top right) and click **Search**.
- Results appear in the bottom pane with clickable line references.
- **Double-click** a result to jump to that location in the file.
- Click **Count** to see how many times the term appears in the entire file.

### View Menu Options
- **UnWrap XML** — Reformat XML content in the current page with indentation
- **UnWrap FixWidth** — Break content into fixed-width lines (80 characters)
- **Save File As** — Save the currently displayed content to a new file
- **Copy Temp File Path** — Copy the path to the temporary working file (only available when viewing EDI/XML/ZIP files that were auto-processed)
- **Open Temp Folder** — Open the temporary folder in Windows Explorer
- **Copy Original File Path** — Copy the original file path shown in the title bar
- **Word Wrap** — Toggle word wrapping
- **Zoom In / Out / 100%** — Adjust text size

---

## Key Features

### Speed
The viewer never loads the entire file into memory. It reads only the portion you're looking at, making it possible to browse files that would crash or freeze other editors.

### Automatic Formatting
- **EDI files**: The viewer detects X12 EDI format (files starting with `ISA`) and automatically splits segments onto separate lines for readability.
- **XML files**: Minified XML is automatically indented and formatted. If the XML is well-formed, it uses a proper XML parser for accurate formatting. If the XML has errors, it falls back to a simpler formatter that handles imperfect files gracefully.

### Temporary Files
When the viewer reformats a file (EDI unwrapping, XML indentation, or ZIP extraction), it creates a temporary copy. The original file is **never modified**. The title bar always shows the original file name. Temporary files are automatically cleaned up when you open a new file or close the application.

---

## Requirements

- Windows 7 or later
- .NET Framework 4.0

---

## Building from Source

1. Clone the repository
2. Open `ScintillaNET.Demo.sln` in Visual Studio
3. Build and run

---

## License

See [LICENSE](LICENSE) for details.
