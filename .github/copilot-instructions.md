# Large EDI/XML Viewer (C# + ScintillaNET)

## Overview
A Windows desktop **read-only viewer** for very large files (500MB–2GB), specifically:
- X12 EDI files (e.g., 837I, 834)
- XML files

The application is **NOT a full editor**. It is optimized for:
- Fast loading
- Line unwrapping
- Paging through large files
- Searching content

---

## Core Concepts

### Chunk-Based File Loading
- Do NOT load entire file
- Read file in chunks (e.g., 10,000 bytes per page)
- Support:
  - First page (top of file)
  - Next / Previous page
  - Jump to offset

```csharp
byte[] ReadChunk(string filePath, long offset, int size);
```

---

### Paging Mechanism
- Each page = fixed byte window (e.g., 10KB–100KB)
- Maintain:
  - Current offset
  - Total file size
- Navigation:
  - Next page → offset += pageSize
  - Previous page → offset -= pageSize

---

### Line Unwrapping

#### EDI (X12)
- Files often contain one long line
- Split using segment terminator (usually `~`)
- Insert newline after each segment
- Save a copy of file in temp folder after unwrapping for faster access
- Unwrap X12 files automatically on first load if they contain no newlines
- If X12 file already contains newlines, no need to use temp file
- EDI detection: read first 107 chars, must start with `ISA`
- Delimiters are positional from ISA segment (element=103, component=104, segment=105)

Example:
```
ISA*...~GS*...~ST*...
```

Becomes:
```
ISA*...~
GS*...~
ST*...
```

#### XML
- Large XML may be minified (single line)
- Auto-unwrap XML files with no line breaks on first load (same as EDI)
- Use `XmlReader`/`XmlWriter` (streaming) for well-formed XML
- Fall back to lightweight character-based parser for malformed XML
- Avoid loading entire XML into memory

---

### ZIP File Support
- ZIP files are extracted to a temp folder using Shell32 COM (`Shell.Application`)
- If ZIP contains one file, open it directly; if multiple, prompt user to pick
- Extracted files then go through normal EDI/XML detection and unwrapping

---

### Temp File Management
- Two tracking fields: `LastTempZipDir` (ZIP extraction) and `LastTempEdiDir` (EDI/XML unwrap)
- When source is already in ZIP temp dir, EDI/XML unwrap reuses that same dir (avoids double temp dirs)
- Unwrapped files use `_unwrapped` suffix to avoid overwriting source
- `OriginalFileName` tracks the user's original file path (ZIP path or direct file path)
- `CurFileName` tracks the actual file being read (may be a temp file)
- Title bar always shows `OriginalFileName` (or the original path), never the temp path
- Temp dirs are cleaned up on new file load and on form close

---

### ScintillaNET Integration
- Use **ScintillaNET** for rendering
- Configure:
  - Read-only mode
  - Efficient text display
  - Large text handling
- Custom line numbers on margin 0 (`MarginType.RightText`), dynamically sized based on digit count

```csharp
scintilla.ReadOnly = true;
scintilla.Text = processedChunk;
```

---

### Search Functionality
- Search the entire file (streaming, not loading into memory)
- EDI files: search uses `SearchEDIFile` which streams through segments
- Non-EDI files: search uses `SearchDisplayedText` on current page
- Count feature: `CountInFile` streams through file counting occurrences
- Search and Count run on `BackgroundWorker` to keep UI responsive
- Results displayed in bottom `RichTextBox` pane with clickable `Line:N` links
- Double-click result → jumps to correct page and scrolls to match

---

## Performance Requirements
- Must handle files up to ~2GB
- Avoid:
  - Full file reads
  - Large string concatenations
- Use:
  - FileStream
  - Buffered reads
  - XmlReader/XmlWriter for XML formatting (streaming, forward-only)
  - Lightweight fallback parser for malformed XML

---

## Constraints
- No full XML DOM loading
- No editing features
- Focus on speed and responsiveness


---

## AI Agent Tasks
- Generate C# implementation for:
  - Chunk-based file reading
  - Paging controller
  - EDI unwrapping logic
  - Lightweight XML formatting
  - ScintillaNET integration
- Ensure:
  - High performance
  - Don's ask for permission to build. Just build the components as needed to achieve the goals outlined above.