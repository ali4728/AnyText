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

## Key Goals
- Open large files without loading entire file into memory
- Unwrap long single-line EDI/XML content
- Provide fast navigation (paging)
- Enable quick text search

---

## Core Concepts

### 1. Chunk-Based File Loading
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

### 2. Paging Mechanism
- Each page = fixed byte window (e.g., 10KB–100KB)
- Maintain:
  - Current offset
  - Total file size
- Navigation:
  - Next page → offset += pageSize
  - Previous page → offset -= pageSize

---

### 3. Line Unwrapping

#### EDI (X12)
- Files often contain one long line
- Split using segment terminator (usually `~`)
- Insert newline after each segment
- Save a copy of file in temp folder after unwrapping for faster access
- Unwrap X12 files automatically on first load if they contain no newlines
- If X12 file already contains newlines, no need to use temp file

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
- Apply lightweight formatting (no full DOM load)
- Avoid loading entire XML into memory

---

### 4. ScintillaNET Integration
- Use **ScintillaNET** for rendering
- Configure:
  - Read-only mode
  - Efficient text display
  - Large text handling

```csharp
scintilla.ReadOnly = true;
scintilla.Text = processedChunk;
```

---

### 5. Search Functionality
- Search the entire file
- Searching within current chunk will be handled by Scintilla's built-in search
- Highlight matches in Scintilla

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

## Suggested Architecture

### Components

#### FileService
- Handles streaming file reads
```csharp
class FileService
{
    byte[] ReadChunk(string path, long offset, int size);
}
```

#### UnwrapService
- Handles EDI/XML formatting
```csharp
class UnwrapService
{
    string UnwrapEdi(string input);
    string FormatXml(string input);
}
```

#### PagingController
- Manages offsets and navigation
```csharp
class PagingController
{
    long CurrentOffset;
    int PageSize;

    void Next();
    void Previous();
}
```

#### Viewer (UI)
- ScintillaNET control
- Displays processed chunk

---

## Workflow

1. User selects file
2. Load first chunk (offset = 0)
3. Detect file type (EDI vs XML)
4. Process chunk:
   - EDI → unwrap
   - XML → format (lightweight)
5. Display in Scintilla
6. User navigates:
   - Next / Previous → load new chunk
7. User searches within visible content

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