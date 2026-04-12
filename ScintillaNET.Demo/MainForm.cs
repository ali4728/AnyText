using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using ScintillaNET;
using ScintillaNET.Demo.Utils;

namespace ScintillaNET.Demo {
	public partial class MainForm : Form {
		public MainForm() {
			InitializeComponent();
		}

		ScintillaNET.Scintilla TextArea;

		private void MainForm_Load(object sender, EventArgs e) {

			// CREATE CONTROL
			TextArea = new ScintillaNET.Scintilla();
			TextPanel.Controls.Add(TextArea);

			// BASIC CONFIG
			TextArea.Dock = System.Windows.Forms.DockStyle.Fill;
			TextArea.TextChanged += (this.OnTextChanged);

			// INITIAL VIEW CONFIG
			TextArea.WrapMode = WrapMode.None;
			TextArea.IndentationGuides = IndentView.LookBoth;

			// STYLING
			InitColors();
			//InitSyntaxColoring();

			// NUMBER MARGIN
			InitNumberMargin();
			InitSyntaxColoringPlain();


			// BOOKMARK MARGIN
			InitBookmarkMargin();

			// CODE FOLDING MARGIN
			//InitCodeFolding();

			// DRAG DROP
			InitDragDropFile();

			// DEFAULT FILE
			//LoadDataFromFile("../../MainForm.cs");

			// INIT HOTKEYS
			InitHotkeys();

		}

		private void InitColors() {

			TextArea.SetSelectionBackColor(true, IntToColor(0xE8CAB3));
			//0xE8CAB3 = light solmon  0x114D9C = blue
		}

		private void InitHotkeys() {

			// register the hotkeys with the form
			HotKeyManager.AddHotKey(this, OpenSearch, Keys.F, true);
			HotKeyManager.AddHotKey(this, OpenFindDialog, Keys.F, true, false, true);
			HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.R, true);
			HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.H, true);
			HotKeyManager.AddHotKey(this, Uppercase, Keys.U, true);
			HotKeyManager.AddHotKey(this, Lowercase, Keys.L, true);
			HotKeyManager.AddHotKey(this, ZoomIn, Keys.Oemplus, true);
			HotKeyManager.AddHotKey(this, ZoomOut, Keys.OemMinus, true);
			HotKeyManager.AddHotKey(this, ZoomDefault, Keys.D0, true);
			HotKeyManager.AddHotKey(this, CloseSearch, Keys.Escape);
			HotKeyManager.AddHotKey(this, UpdateClip, Keys.D9, true);
			HotKeyManager.AddHotKey(this, unWrapXMLShortcut, Keys.D8, true);
			HotKeyManager.AddHotKey(this, buttonRightShortcut, Keys.M, true, false, true);
			HotKeyManager.AddHotKey(this, buttonLeftShortcut, Keys.N, true, false, true);
			
			// remove conflicting hotkeys from scintilla
			TextArea.ClearCmdKey(Keys.Control | Keys.F);
			TextArea.ClearCmdKey(Keys.Control | Keys.R);
			TextArea.ClearCmdKey(Keys.Control | Keys.H);
			TextArea.ClearCmdKey(Keys.Control | Keys.L);
			TextArea.ClearCmdKey(Keys.Control | Keys.U);

		}

		private void InitSyntaxColoringSQL()
		{

			TextArea.StyleResetDefault();
			TextArea.Styles[Style.Default].Font = "Consolas";
			TextArea.Styles[Style.Default].Size = 11;
			TextArea.StyleClearAll();

			// Set the SQL Lexer
			TextArea.Lexer = Lexer.Sql;

			// Show line numbers
			//TextArea.Margins[0].Width = 20;


			//TextArea.SetFoldMarginColor(true, IntToColor(FORE_COLOR));
			//TextArea.SetFoldMarginHighlightColor(true, IntToColor(FORE_COLOR));


			//TextArea.Styles[Style.LineNumber].ForeColor = Color.FromArgb(255, 128, 128, 128);  //Dark Gray
			//TextArea.Styles[Style.LineNumber].BackColor = Color.FromArgb(255, 228, 228, 228);  //Light Gray
			TextArea.Styles[Style.Sql.Comment].ForeColor = Color.Green;
			TextArea.Styles[Style.Sql.CommentLine].ForeColor = Color.Green;
			TextArea.Styles[Style.Sql.CommentLineDoc].ForeColor = Color.Green;
			TextArea.Styles[Style.Sql.Number].ForeColor = Color.Maroon;
			TextArea.Styles[Style.Sql.Word].ForeColor = Color.Blue;
			TextArea.Styles[Style.Sql.Word2].ForeColor = Color.Fuchsia;
			TextArea.Styles[Style.Sql.User1].ForeColor = Color.Gray;
			TextArea.Styles[Style.Sql.User2].ForeColor = Color.FromArgb(255, 00, 128, 192);    //Medium Blue-Green
			TextArea.Styles[Style.Sql.String].ForeColor = Color.Red;
			TextArea.Styles[Style.Sql.Character].ForeColor = Color.Red;
			TextArea.Styles[Style.Sql.Operator].ForeColor = Color.Black;

			// Set keyword lists
			// Word = 0
			TextArea.SetKeywords(0, @"add alter as authorization backup begin bigint binary bit break browse bulk by cascade case catch check checkpoint close clustered column commit compute constraint containstable continue create current cursor cursor database date datetime datetime2 datetimeoffset dbcc deallocate decimal declare default delete deny desc disk distinct distributed double drop dump else end errlvl escape except exec execute exit external fetch file fillfactor float for foreign freetext freetexttable from full function goto grant group having hierarchyid holdlock identity identity_insert identitycol if image index insert int intersect into key kill lineno load merge money national nchar nocheck nocount nolock nonclustered ntext numeric nvarchar of off offsets on open opendatasource openquery openrowset openxml option order over percent plan precision primary print proc procedure public raiserror read readtext real reconfigure references replication restore restrict return revert revoke rollback rowcount rowguidcol rule save schema securityaudit select set setuser shutdown smalldatetime smallint smallmoney sql_variant statistics table table tablesample text textsize then time timestamp tinyint to top tran transaction trigger truncate try union unique uniqueidentifier update updatetext use user values varbinary varchar varying view waitfor when where while with writetext xml go ");
			// Word2 = 1
			TextArea.SetKeywords(1, @"ascii cast char charindex ceiling coalesce collate contains convert current_date current_time current_timestamp current_user floor isnull max min nullif object_id session_user substring system_user tsequal ");
			// User1 = 4
			TextArea.SetKeywords(4, @"all and any between cross exists in inner is join left like not null or outer pivot right some unpivot ( ) * ");
			// User2 = 5
			TextArea.SetKeywords(5, @"sys objects sysobjects ");


		}

		private void InitSyntaxColoringXML()
		{

			// Configure the default style
			TextArea.StyleResetDefault();
			TextArea.Styles[Style.Default].Font = "Consolas";
			TextArea.Styles[Style.Default].Size = 11;
			//TextArea.Styles[Style.Default].BackColor = IntToColor(0x212121);
			//TextArea.Styles[Style.Default].ForeColor = IntToColor(0xFFFFFF);
			TextArea.StyleClearAll();

			
			TextArea.Styles[Style.Xml.Comment].ForeColor = Color.DarkGray;
			TextArea.Styles[Style.Xml.Attribute].ForeColor = Color.Red;
			TextArea.Styles[Style.Xml.Default].ForeColor = Color.Black;
			TextArea.Styles[Style.Xml.XmlStart].ForeColor = Color.Red;
			TextArea.Styles[Style.Xml.XmlEnd].ForeColor = Color.Red;
			TextArea.Styles[Style.Xml.Tag].ForeColor = Color.Blue;
			TextArea.Styles[Style.Xml.TagEnd].ForeColor = Color.Blue;
			TextArea.Styles[Style.Xml.Value].ForeColor = Color.Black;
			TextArea.Styles[Style.Xml.CData].ForeColor = Color.Orange;
			
			TextArea.Lexer = Lexer.Xml;


		}

		private void InitSyntaxColoringPlain()
		{
			TextArea.StyleResetDefault();
			TextArea.Styles[Style.Default].Font = "Consolas";
			TextArea.Styles[Style.Default].Size = 11;
			TextArea.StyleClearAll();
		}


		private void InitSyntaxColoring() {

			// Configure the default style
			TextArea.StyleResetDefault();
			TextArea.Styles[Style.Default].Font = "Consolas";
			TextArea.Styles[Style.Default].Size = 11;
			TextArea.Styles[Style.Default].BackColor = IntToColor(0x212121);
			TextArea.Styles[Style.Default].ForeColor = IntToColor(0xFFFFFF);
			TextArea.StyleClearAll();

			// Configure the CPP (C#) lexer styles
			TextArea.Styles[Style.Cpp.Identifier].ForeColor = IntToColor(0xD0DAE2);
			TextArea.Styles[Style.Cpp.Comment].ForeColor = IntToColor(0xBD758B);
			TextArea.Styles[Style.Cpp.CommentLine].ForeColor = IntToColor(0x40BF57);
			TextArea.Styles[Style.Cpp.CommentDoc].ForeColor = IntToColor(0x2FAE35);
			TextArea.Styles[Style.Cpp.Number].ForeColor = IntToColor(0xFFFF00);
			TextArea.Styles[Style.Cpp.String].ForeColor = IntToColor(0xFFFF00);
			TextArea.Styles[Style.Cpp.Character].ForeColor = IntToColor(0xE95454);
			TextArea.Styles[Style.Cpp.Preprocessor].ForeColor = IntToColor(0x8AAFEE);
			TextArea.Styles[Style.Cpp.Operator].ForeColor = IntToColor(0xE0E0E0);
			TextArea.Styles[Style.Cpp.Regex].ForeColor = IntToColor(0xff00ff);
			TextArea.Styles[Style.Cpp.CommentLineDoc].ForeColor = IntToColor(0x77A7DB);
			TextArea.Styles[Style.Cpp.Word].ForeColor = IntToColor(0x48A8EE);
			TextArea.Styles[Style.Cpp.Word2].ForeColor = IntToColor(0xF98906);
			TextArea.Styles[Style.Cpp.CommentDocKeyword].ForeColor = IntToColor(0xB3D991);
			TextArea.Styles[Style.Cpp.CommentDocKeywordError].ForeColor = IntToColor(0xFF0000);
			TextArea.Styles[Style.Cpp.GlobalClass].ForeColor = IntToColor(0x48A8EE);



			TextArea.Lexer = Lexer.Cpp;



			TextArea.SetKeywords(0, "class extends implements import interface new case do while else if for in switch throw get set function var try catch finally while with default break continue delete return each const namespace package include use is as instanceof typeof author copy default deprecated eventType example exampleText exception haxe inheritDoc internal link mtasc mxmlc param private return see serial serialData serialField since throws usage version langversion playerversion productversion dynamic private public partial static intrinsic internal native override protected AS3 final super this arguments null Infinity NaN undefined true false abstract as base bool break by byte case catch char checked class const continue decimal default delegate do double descending explicit event extern else enum false finally fixed float for foreach from goto group if implicit in int interface internal into is lock long new null namespace object operator out override orderby params private protected public readonly ref return switch struct sbyte sealed short sizeof stackalloc static string select this throw true try typeof uint ulong unchecked unsafe ushort using var virtual volatile void while where yield");
			TextArea.SetKeywords(1, "void Null ArgumentError arguments Array Boolean Class Date DefinitionError Error EvalError Function int Math Namespace Number Object RangeError ReferenceError RegExp SecurityError String SyntaxError TypeError uint XML XMLList Boolean Byte Char DateTime Decimal Double Int16 Int32 Int64 IntPtr SByte Single UInt16 UInt32 UInt64 UIntPtr Void Path File System Windows Forms ScintillaNET");

		}

		private void InitControls()
		{
			FileUtils.CurFileName = "";
			FileUtils.OriginalFileName = "";
			FileUtils.fileSize = 0;
			FileUtils.LineOffsetIndex = null;
			labelTotalBytes.Text = String.Format("Bytes: {0:n0}", 0);
			labelTotals.Text = "0";
			this.Text = "";

		}

		private void OnTextChanged(object sender, EventArgs e) {

		}
		

		#region Numbers, Bookmarks, Code Folding

		/// <summary>
		/// the background color of the text area
		/// </summary>
		private const int BACK_COLOR = 0x2A211C;

		/// <summary>
		/// default text color of the text area
		/// </summary>
		private const int FORE_COLOR = 0xB7B7B7;

		/// <summary>
		/// change this to whatever margin you want the line numbers to show in
		/// </summary>
		private const int NUMBER_MARGIN = 1;

		/// <summary>
		/// change this to whatever margin you want the bookmarks/breakpoints to show in
		/// </summary>
		private const int BOOKMARK_MARGIN = 2;
		private const int BOOKMARK_MARKER = 2;

		/// <summary>
		/// change this to whatever margin you want the code folding tree (+/-) to show in
		/// </summary>
		private const int FOLDING_MARGIN = 3;

		/// <summary>
		/// set this true to show circular buttons for code folding (the [+] and [-] buttons on the margin)
		/// </summary>
		private const bool CODEFOLDING_CIRCULAR = true;

		private void InitNumberMargin() {

			TextArea.Styles[Style.LineNumber].BackColor = IntToColor(BACK_COLOR);
			TextArea.Styles[Style.LineNumber].ForeColor = IntToColor(FORE_COLOR);
			TextArea.Styles[Style.IndentGuide].ForeColor = IntToColor(FORE_COLOR);
			TextArea.Styles[Style.IndentGuide].BackColor = IntToColor(BACK_COLOR);

			var nums = TextArea.Margins[NUMBER_MARGIN];
			nums.Width = 50;	//30;
			nums.Type = MarginType.Number;
			nums.Sensitive = true;
			nums.Mask = 0;

			TextArea.MarginClick += TextArea_MarginClick;
		}

		private void InitBookmarkMargin() {

			//TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));

			var margin = TextArea.Margins[BOOKMARK_MARGIN];
			margin.Width = 20;
			margin.Sensitive = true;
			margin.Type = MarginType.Symbol;
			margin.Mask = (1 << BOOKMARK_MARKER);
			//margin.Cursor = MarginCursor.Arrow;

			var marker = TextArea.Markers[BOOKMARK_MARKER];
			marker.Symbol = MarkerSymbol.Circle;
			marker.SetBackColor(IntToColor(0xFF003B));
			marker.SetForeColor(IntToColor(0x000000));
			marker.SetAlpha(100);

		}

		private void InitCodeFolding() {

			TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));
			TextArea.SetFoldMarginHighlightColor(true, IntToColor(BACK_COLOR));

			// Enable code folding
			TextArea.SetProperty("fold", "1");
			TextArea.SetProperty("fold.compact", "1");

			// Configure a margin to display folding symbols
			TextArea.Margins[FOLDING_MARGIN].Type = MarginType.Symbol;
			TextArea.Margins[FOLDING_MARGIN].Mask = Marker.MaskFolders;
			TextArea.Margins[FOLDING_MARGIN].Sensitive = true;
			TextArea.Margins[FOLDING_MARGIN].Width = 20;

			// Set colors for all folding markers
			for (int i = 25; i <= 31; i++) {
				TextArea.Markers[i].SetForeColor(IntToColor(BACK_COLOR)); // styles for [+] and [-]
				TextArea.Markers[i].SetBackColor(IntToColor(FORE_COLOR)); // styles for [+] and [-]
			}

			// Configure folding markers with respective symbols
			TextArea.Markers[Marker.Folder].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlus : MarkerSymbol.BoxPlus;
			TextArea.Markers[Marker.FolderOpen].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinus : MarkerSymbol.BoxMinus;
			TextArea.Markers[Marker.FolderEnd].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlusConnected : MarkerSymbol.BoxPlusConnected;
			TextArea.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
			TextArea.Markers[Marker.FolderOpenMid].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinusConnected : MarkerSymbol.BoxMinusConnected;
			TextArea.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
			TextArea.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

			// Enable automatic folding
			TextArea.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

		}

		private void TextArea_MarginClick(object sender, MarginClickEventArgs e) {
			if (e.Margin == BOOKMARK_MARGIN) {
				// Do we have a marker for this line?
				const uint mask = (1 << BOOKMARK_MARKER);
				var line = TextArea.Lines[TextArea.LineFromPosition(e.Position)];
				if ((line.MarkerGet() & mask) > 0) {
					// Remove existing bookmark
					line.MarkerDelete(BOOKMARK_MARKER);
				} else {
					// Add bookmark
					line.MarkerAdd(BOOKMARK_MARKER);
				}
			}
		}

		#endregion

		#region Drag & Drop File

		public void InitDragDropFile() {

			TextArea.AllowDrop = true;
			TextArea.DragEnter += delegate(object sender, DragEventArgs e) {
				if (e.Data.GetDataPresent(DataFormats.FileDrop))
					e.Effect = DragDropEffects.Copy;
				else
					e.Effect = DragDropEffects.None;
			};
			TextArea.DragDrop += delegate(object sender, DragEventArgs e) {

				// get file drop
				if (e.Data.GetDataPresent(DataFormats.FileDrop)) {

					Array a = (Array)e.Data.GetData(DataFormats.FileDrop);
					if (a != null) {

						string path = a.GetValue(0).ToString();

						LoadDataFromFile(path);

					}
				}
			};

		}

		private int getLimit()
		{
			int limit = 0;
			try
			{
				limit = Int32.Parse(textBoxLimit.Text.Trim());				
			}
			catch (Exception ex)
			{
				limit = 0;
			}
			return limit;
		}
		private void LoadDataFromFile(string path) {
			richTextBoxBottom.Text = "";
			FileUtils.CurFileName = path;
			FileUtils.fileHasLineBreaks = false;
			FileUtils.CleanupTempEdiDir();

			// Only reset OriginalFileName if this is not a file from a ZIP extraction
			if (string.IsNullOrEmpty(FileUtils.LastTempZipDir) || !path.StartsWith(FileUtils.LastTempZipDir, StringComparison.OrdinalIgnoreCase))
			{
				FileUtils.OriginalFileName = "";
			}

			if (File.Exists(path)) 
			{
				// Handle ZIP files (case-insensitive: .zip, .ZIP, .Zip, etc.)
				if (FileUtils.IsZipFile(path))
				{
					try
					{
						string tempDir = FileUtils.ExtractZipToTemp(path);
						string[] extractedFiles = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
						if (extractedFiles.Length == 0)
						{
							MessageBox.Show("No files found in ZIP archive.", "ZIP Extract", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							return;
						}

						string selectedFile;
						if (extractedFiles.Length == 1)
						{
							selectedFile = extractedFiles[0];
						}
						else
						{
							OpenFileDialog zipPicker = new OpenFileDialog();
							zipPicker.InitialDirectory = tempDir;
							zipPicker.Title = "Select a file from the ZIP archive";
							zipPicker.Filter = "All files|*.*";
							if (zipPicker.ShowDialog() == DialogResult.OK)
							{
								selectedFile = zipPicker.FileName;
							}
							else
							{
								return;
							}
						}

						// Preserve original ZIP path so title bar shows it
						FileUtils.OriginalFileName = path;
						LoadDataFromFile(selectedFile);
					}
					catch (Exception ex)
					{
						MessageBox.Show("Failed to extract ZIP file: " + ex.Message, "ZIP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					return;
				}

				FileInfo fi = new FileInfo(path);
				long fileSize = fi.Length;
				FileUtils.fileSize = fileSize;
				FileUtils.LineOffsetIndex = null;
				ResetLineNumbers();
				labelTotalBytes.Text = String.Format("Bytes: {0:n0}", fileSize);
				int totPages = (int)(FileUtils.fileSize / getLimit());
				labelTotals.Text = totPages.ToString(); 

				// Show original file name in title bar, not temp path
				string displayName = path;
				if (!string.IsNullOrEmpty(FileUtils.OriginalFileName))
					displayName = FileUtils.OriginalFileName;
				else if (!string.IsNullOrEmpty(FileUtils.LastTempZipDir) && path.StartsWith(FileUtils.LastTempZipDir, StringComparison.OrdinalIgnoreCase))
					displayName = Path.GetFileName(path);
				this.Text = displayName;


				try
				{
					int limit = getLimit();

					if (limit > 0)
					{
						if (FileUtils.GCTrigger == 5)
						{
							System.GC.Collect();
							FileUtils.GCTrigger = 0;
						}


						TextArea.Text = FileUtils.readNBites(path, limit, 0);
						FileUtils.GCTrigger++;

						// Auto-unwrap EDI files with no line breaks to temp file
						if (!FileUtils.fileHasLineBreaks)
						{
							EDIHelper ediHelper = new EDIHelper();
							if (ediHelper.IsEDIFile(path))
							{
								this.Cursor = Cursors.WaitCursor;
								try
								{
									Delimeters del = new Delimeters(path);
									string tempFile = FileUtils.UnwrapEdiToTempFile(path, del.SegmentDelimeter);
									FileUtils.OriginalFileName = path;
									FileUtils.CurFileName = tempFile;
									FileInfo tempFi = new FileInfo(tempFile);
									FileUtils.fileSize = tempFi.Length;
									FileUtils.fileHasLineBreaks = true;
									FileUtils.LineOffsetIndex = null;
									labelTotalBytes.Text = String.Format("Bytes: {0:n0}", FileUtils.fileSize);
									int totPagesEdi = (int)(FileUtils.fileSize / limit);
									labelTotals.Text = totPagesEdi.ToString();
									TextArea.Text = FileUtils.readNBites(tempFile, limit, 0);
									UpdateLineNumbers(1);
								}
								finally
								{
									this.Cursor = Cursors.Default;
								}
								return;
							}
						}

						if (FileUtils.fileHasLineBreaks)
						{
							UpdateLineNumbers(1);
						}
						return;
					}
				}
				catch (Exception ex )
				{
					//do nothing
				}

				TextArea.Text = File.ReadAllText(path);

			}
		}

		#endregion

		#region Main Menu Commands

		private void openToolStripMenuItem_Click(object sender, EventArgs e) {
			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				LoadDataFromFile(openFileDialog.FileName);
			}
		}

		private void findToolStripMenuItem_Click(object sender, EventArgs e) {
			OpenSearch();
		}

		private void findDialogToolStripMenuItem_Click(object sender, EventArgs e) {
			OpenFindDialog();
		}

		private void findAndReplaceToolStripMenuItem_Click(object sender, EventArgs e) {
			OpenReplaceDialog();
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e) {
			TextArea.Cut();
		}

		private void copyToolStripMenuItem_Click(object sender, EventArgs e) {
			TextArea.Copy();
		}

		private void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
			TextArea.Paste();
		}

		private void selectAllToolStripMenuItem_Click(object sender, EventArgs e) {
			TextArea.SelectAll();
		}

		private void selectLineToolStripMenuItem_Click(object sender, EventArgs e) {
			Line line = TextArea.Lines[TextArea.CurrentLine];
			TextArea.SetSelection(line.Position + line.Length, line.Position);
		}

		private void clearSelectionToolStripMenuItem_Click(object sender, EventArgs e) {
			TextArea.SetEmptySelection(0);
		}

		private void indentSelectionToolStripMenuItem_Click(object sender, EventArgs e) {
			Indent();
		}

		private void outdentSelectionToolStripMenuItem_Click(object sender, EventArgs e) {
			Outdent();
		}

		private void uppercaseSelectionToolStripMenuItem_Click(object sender, EventArgs e) {
			Uppercase();
		}

		private void lowercaseSelectionToolStripMenuItem_Click(object sender, EventArgs e) {
			Lowercase();
		}

		private void wordWrapToolStripMenuItem1_Click(object sender, EventArgs e) {

			// toggle word wrap
			wordWrapItem.Checked = !wordWrapItem.Checked;
			TextArea.WrapMode = wordWrapItem.Checked ? WrapMode.Word : WrapMode.None;
		}
		
		private void indentGuidesToolStripMenuItem_Click(object sender, EventArgs e) {

			// toggle indent guides
			indentGuidesItem.Checked = !indentGuidesItem.Checked;
			TextArea.IndentationGuides = indentGuidesItem.Checked ? IndentView.LookBoth : IndentView.None;
		}

		private void hiddenCharactersToolStripMenuItem_Click(object sender, EventArgs e) {

			// toggle view whitespace
			hiddenCharactersItem.Checked = !hiddenCharactersItem.Checked;
			TextArea.ViewWhitespace = hiddenCharactersItem.Checked ? WhitespaceMode.VisibleAlways : WhitespaceMode.Invisible;
		}

		private void zoomInToolStripMenuItem_Click(object sender, EventArgs e) {
			ZoomIn();
		}

		private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e) {
			ZoomOut();
		}

		private void zoom100ToolStripMenuItem_Click(object sender, EventArgs e) {
			ZoomDefault();
		}

		private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e) {
			TextArea.FoldAll(FoldAction.Contract);
		}

		private void expandAllToolStripMenuItem_Click(object sender, EventArgs e) {
			TextArea.FoldAll(FoldAction.Expand);
		}
		

		#endregion

		#region Uppercase / Lowercase

		private void Lowercase() {

			// save the selection
			int start = TextArea.SelectionStart;
			int end = TextArea.SelectionEnd;

			// modify the selected text
			TextArea.ReplaceSelection(TextArea.GetTextRange(start, end - start).ToLower());

			// preserve the original selection
			TextArea.SetSelection(start, end);
		}

		private void Uppercase() {

			// save the selection
			int start = TextArea.SelectionStart;
			int end = TextArea.SelectionEnd;

			// modify the selected text
			TextArea.ReplaceSelection(TextArea.GetTextRange(start, end - start).ToUpper());

			// preserve the original selection
			TextArea.SetSelection(start, end);
		}

		#endregion

		#region Indent / Outdent

		private void Indent() {
			// we use this hack to send "Shift+Tab" to scintilla, since there is no known API to indent,
			// although the indentation function exists. Pressing TAB with the editor focused confirms this.
			GenerateKeystrokes("{TAB}");
		}

		private void Outdent() {
			// we use this hack to send "Shift+Tab" to scintilla, since there is no known API to outdent,
			// although the indentation function exists. Pressing Shift+Tab with the editor focused confirms this.
			GenerateKeystrokes("+{TAB}");
		}

		private void GenerateKeystrokes(string keys) {
			HotKeyManager.Enable = false;
			TextArea.Focus();
			SendKeys.Send(keys);
			HotKeyManager.Enable = true;
		}

		#endregion

		#region Zoom

		private void ZoomIn() {
			TextArea.ZoomIn();
		}

		private void ZoomOut() {
			TextArea.ZoomOut();
		}

		private void ZoomDefault() {
			TextArea.Zoom = 0;
		}


		#endregion

		#region Quick Search Bar

		bool SearchIsOpen = false;

		private void OpenSearch() {

			SearchManager.SearchBox = TxtSearch;
			SearchManager.TextArea = TextArea;

			if (!SearchIsOpen) {
				SearchIsOpen = true;
				InvokeIfNeeded(delegate() {
					PanelSearch.Visible = true;
					TxtSearch.Text = SearchManager.LastSearch;
					TxtSearch.Focus();
					TxtSearch.SelectAll();
				});
			} else {
				InvokeIfNeeded(delegate() {
					TxtSearch.Focus();
					TxtSearch.SelectAll();
				});
			}
		}
		private void CloseSearch() {
			if (SearchIsOpen) {
				SearchIsOpen = false;
				InvokeIfNeeded(delegate() {
					PanelSearch.Visible = false;
					//CurBrowser.GetBrowser().StopFinding(true);
				});
			}
		}

		private void UpdateClip()
		{
			FileUtils.UpdateClip();
		}
		private void BtnClearSearch_Click(object sender, EventArgs e) {
			CloseSearch();
		}

		private void BtnPrevSearch_Click(object sender, EventArgs e) {
			SearchManager.Find(false, false);
		}
		private void BtnNextSearch_Click(object sender, EventArgs e) {
			SearchManager.Find(true, false);
		}
		private void TxtSearch_TextChanged(object sender, EventArgs e) {
			SearchManager.Find(true, true);
		}

		private void TxtSearch_KeyDown(object sender, KeyEventArgs e) {
			if (HotKeyManager.IsHotkey(e, Keys.Enter)) {
				SearchManager.Find(true, false);
			}
			if (HotKeyManager.IsHotkey(e, Keys.Enter, true) || HotKeyManager.IsHotkey(e, Keys.Enter, false, true)) {
				SearchManager.Find(false, false);
			}
		}

		#endregion

		#region Find & Replace Dialog

		private void OpenFindDialog() {

		}
		private void OpenReplaceDialog() {


		}

		#endregion

		#region Utils

		public static Color IntToColor(int rgb) {
			return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
		}

		public void InvokeIfNeeded(Action action) {
			if (this.InvokeRequired) {
				this.BeginInvoke(action);
			} else {
				action.Invoke();
			}
		}




        #endregion


        #region custom
        private void toolStripMenuItemFileName_Click(object sender, EventArgs e)
        {
			if (FileUtils.CurFileName.Length > 0)
			{
				FileUtils.UpdateClip();
			}
        }

        private void unWrapXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
			InitSyntaxColoringXML();
			TextArea.Text = FileUtils.UnWrapXML(TextArea.Text);
		}

		private void unWrapXMLShortcut()
		{
			InitSyntaxColoringXML();
			TextArea.Text = FileUtils.UnWrapXML(TextArea.Text);
		}

		private void sQLStyleToolStripMenuItem_Click(object sender, EventArgs e)
        {
			InitSyntaxColoringSQL();

		}

		private void saveFileAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "All files|*.*|Text files|*.txt|EDI files|*.edi|XML files|*.xml";
			sfd.Title = "Save File As";

			if (!string.IsNullOrEmpty(FileUtils.CurFileName) && File.Exists(FileUtils.CurFileName))
			{
				sfd.InitialDirectory = Path.GetDirectoryName(FileUtils.CurFileName);
				string fnWoExt = Path.GetFileNameWithoutExtension(FileUtils.CurFileName);
				string fExt = Path.GetExtension(FileUtils.CurFileName);
				sfd.FileName = fnWoExt + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + fExt;
			}

			if (sfd.ShowDialog() == DialogResult.OK)
			{
				File.WriteAllText(sfd.FileName, TextArea.Text);
			}
		}

		private void copyTempPathToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Only copy if a temp file is actually in use (ZIP extraction or EDI unwrap)
			if (string.IsNullOrEmpty(FileUtils.LastTempZipDir) && string.IsNullOrEmpty(FileUtils.LastTempEdiDir))
			{
				MessageBox.Show("No temp file in use.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string tempPath = FileUtils.CurFileName;
			if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
			{
				Clipboard.SetText(tempPath);
				Console.WriteLine("Copied temp path: " + tempPath);
			}
		}

		private void openTempFolderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Only open if a temp folder is actually in use (ZIP extraction or EDI unwrap)
			string tempDir = "";
			if (!string.IsNullOrEmpty(FileUtils.LastTempZipDir) && Directory.Exists(FileUtils.LastTempZipDir))
			{
				tempDir = FileUtils.LastTempZipDir;
			}
			else if (!string.IsNullOrEmpty(FileUtils.LastTempEdiDir) && Directory.Exists(FileUtils.LastTempEdiDir))
			{
				tempDir = FileUtils.LastTempEdiDir;
			}

			if (!string.IsNullOrEmpty(tempDir))
			{
				Process.Start("explorer.exe", tempDir);
			}
			else
			{
				MessageBox.Show("No temp folder in use.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void copyOriginalPathToolStripMenuItem_Click(object sender, EventArgs e)
		{
			string originalPath = this.Text;
			if (!string.IsNullOrEmpty(originalPath))
			{
				Clipboard.SetText(originalPath);
				Console.WriteLine("Copied original path: " + originalPath);
			}
		}

		private void unWrapFixWidthToolStripMenuItem_Click(object sender, EventArgs e)
        {
			TextArea.Text = FileUtils.getFixWidth(TextArea.Text, 80);
        }

		private void buttonLeftShortcut()
		{
			try
			{
				int page = Int32.Parse(textBoxPage.Text);
				int maxPage = ((int)(FileUtils.fileSize / getLimit()));
				if ((page - 1) >= 0)
				{
					page--;
					int offset = page;

					int limit = getLimit();
					if (limit > 0)
					{
						TextArea.Text = FileUtils.readNBites(FileUtils.CurFileName, limit, offset);
						textBoxPage.Text = page.ToString();
						ApplyLineNumbers(page);
					}

				}
				else
				{
					Console.WriteLine("Page size " + page.ToString() + " you cannot go negative. Max page size of " + maxPage.ToString());
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

		}
		private void buttonLeft_Click(object sender, EventArgs e)
        {
			buttonLeftShortcut();
		}

		private void buttonRightShortcut()
		{
			try
			{
				int page = Int32.Parse(textBoxPage.Text);
				int maxPage = ((int)(FileUtils.fileSize / getLimit()));
				if ((page + 1) <= maxPage)
				{
					page++;
					int offset = page;

					int limit = getLimit();
					if (limit > 0)
					{
						TextArea.Text = FileUtils.readNBites(FileUtils.CurFileName, limit, offset);
						textBoxPage.Text = page.ToString();
						ApplyLineNumbers(page);
					}

				}
				else
				{
					Console.WriteLine("Page size " + page.ToString() + " is => Max page size of " + maxPage.ToString());
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
        private void buttonRight_Click(object sender, EventArgs e)
        {
			buttonRightShortcut();
		}

        private void textBoxPage_DoubleClick(object sender, EventArgs e)
        {

		}


		private void buttonJumpToAnyPage(int page)
		{
			try
			{				
				int maxPage = ((int)(FileUtils.fileSize / getLimit()));
				if (page <= maxPage && page > -1)
				{
					int offset = page;

					int limit = getLimit();
					if (limit > 0)
					{
						TextArea.Text = FileUtils.readNBites(FileUtils.CurFileName, limit, offset);
						textBoxPage.Text = page.ToString();
						ApplyLineNumbers(page);
					}

				}
				else
				{
					Console.WriteLine("Page size " + page.ToString() + " is out of bound Max page size of " + maxPage.ToString());
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
		private void buttonJumpTo_Click(object sender, EventArgs e)
        {
			try
			{
				int page = Int32.Parse(textBoxPage.Text);
				buttonJumpToAnyPage(page);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

        #endregion

        private void syntaxXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
			InitSyntaxColoringXML();
		}

        private void syntaxPlainToolStripMenuItem_Click(object sender, EventArgs e)
        {
			InitSyntaxColoringPlain();
        }

        private void resetObjectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
			InitControls();
			TextArea.Text = "";			
			TextArea.Dispose();
			System.GC.Collect();
			// CREATE CONTROL
			TextArea = new ScintillaNET.Scintilla();
			TextPanel.Controls.Add(TextArea);

			// BASIC CONFIG
			TextArea.Dock = System.Windows.Forms.DockStyle.Fill;
			TextArea.TextChanged += (this.OnTextChanged);

			// INITIAL VIEW CONFIG
			TextArea.WrapMode = WrapMode.None;
			TextArea.IndentationGuides = IndentView.LookBoth;

			// STYLING
			InitColors();
			//InitSyntaxColoring();

			// NUMBER MARGIN
			InitNumberMargin();
			InitSyntaxColoringPlain();


			// BOOKMARK MARGIN
			InitBookmarkMargin();

			// CODE FOLDING MARGIN
			//InitCodeFolding();

			// DRAG DROP
			InitDragDropFile();

			// DEFAULT FILE
			//LoadDataFromFile("../../MainForm.cs");

			// INIT HOTKEYS
			InitHotkeys();

		}

		private void buttonSearchFile_Click(object sender, EventArgs e)
		{
			string txt = textBoxSearchFile.Text.Trim();
			if (string.IsNullOrEmpty(txt)) return;

			// Show busy state
			buttonSearchFile.Enabled = false;
			buttonSearchFile.Text = "Searching...";
			this.Cursor = Cursors.WaitCursor;
			richTextBoxBottom.Clear();
			richTextBoxBottom.Font = new Font("Consolas", 9);
			richTextBoxBottom.AppendText("Searching...");

			// Capture values needed by background thread
			string curFile = FileUtils.CurFileName;
			string displayedText = TextArea.Text;

			BackgroundWorker bgw = new BackgroundWorker();
			bgw.DoWork += delegate(object s, DoWorkEventArgs args)
			{
				Dictionary<long, string> loc;
				EDIHelper helper = new EDIHelper();
				if (!string.IsNullOrEmpty(curFile) && File.Exists(curFile) && helper.IsEDIFile(curFile))
				{
					loc = helper.SearchEDIFile(curFile, txt);
				}
				else
				{
					loc = FileUtils.SearchDisplayedText(displayedText, txt);
				}
				args.Result = loc;
			};
			bgw.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
			{
				this.Cursor = Cursors.Default;
				buttonSearchFile.Text = "Search";
				buttonSearchFile.Enabled = true;

				if (args.Error != null)
				{
					richTextBoxBottom.Clear();
					richTextBoxBottom.AppendText("Search error: " + args.Error.Message);
					return;
				}

				Dictionary<long, string> loc = (Dictionary<long, string>)args.Result;
				DisplaySearchResults(loc);
			};
			bgw.RunWorkerAsync();
		}

		private void DisplaySearchResults(Dictionary<long, string> loc)
		{
			richTextBoxBottom.Clear();
			richTextBoxBottom.Font = new Font("Consolas", 9);

			if (loc.Count == 0)
			{
				string searchTerm = textBoxSearchFile.Text.Trim();
				richTextBoxBottom.SelectionColor = Color.Red;
				richTextBoxBottom.AppendText("No \"" + searchTerm + "\" found.");
				richTextBoxBottom.SelectionColor = richTextBoxBottom.ForeColor;
				return;
			}

			foreach (KeyValuePair<long, string> kvp in loc)
			{
				int startPos = richTextBoxBottom.TextLength;
				string offsetStr = kvp.Key.ToString();
				richTextBoxBottom.AppendText(offsetStr);

				string value = kvp.Value;

				// Find tag portion ("Line:N") and highlight it
				string tag = null;
				int tagIdx = value.IndexOf("Line:");
				if (tagIdx >= 0)
				{
					tag = "Line:";
				}

				if (tag != null && tagIdx >= 0)
				{
					string before = value.Substring(0, tagIdx);
					richTextBoxBottom.AppendText(before);

					int numEnd = value.IndexOf(" ", tagIdx + tag.Length);
					if (numEnd == -1) numEnd = value.Length;
					string tagLabel = value.Substring(tagIdx, numEnd - tagIdx);
					string rest = value.Substring(numEnd);

					int linkStart = richTextBoxBottom.TextLength;
					richTextBoxBottom.AppendText(tagLabel);
					richTextBoxBottom.Select(linkStart, tagLabel.Length);
					richTextBoxBottom.SelectionColor = Color.Blue;
					richTextBoxBottom.SelectionFont = new Font(richTextBoxBottom.Font, FontStyle.Bold | FontStyle.Underline);

					richTextBoxBottom.AppendText(rest);
				}
				else
				{
					richTextBoxBottom.AppendText(value);
				}

				richTextBoxBottom.AppendText(Environment.NewLine);
			}

			richTextBoxBottom.SelectionStart = 0;
			richTextBoxBottom.SelectionLength = 0;
		}


		private void ScrollToLineNumber(int ln)
		{
			int tot = TextArea.Lines.Count;
			if (ln <= tot)
			{
                Console.WriteLine("Will Scroll to line " + ln.ToString());
				var linesOnScreen = TextArea.LinesOnScreen - 2;

				var line = ln;

				var start = TextArea.Lines[line - (linesOnScreen / 2)].Position;
				var end = TextArea.Lines[line + (linesOnScreen / 2)].Position;
				TextArea.ScrollRange(start, end);

				string searchString = textBoxSearchFile.Text;
				HighlightWord(searchString);
			}
			else
			{
                Console.WriteLine("Line " + ln.ToString() + " is out of range. Max Line is " + tot.ToString());
			}

		}
      

		private void richTextBoxBottom_DoubleClick(object sender, EventArgs e)
		{

			string str = richTextBoxBottom.Lines[richTextBoxBottom.GetLineFromCharIndex(richTextBoxBottom.SelectionStart)];
			Console.WriteLine("line is selected: " + str);
			//
			try
			{
				if (str.Contains("Line:"))
				{
					int lineStart = str.IndexOf("Line:") + 5;
					int lineEnd = str.IndexOf(" ", lineStart);
					if (lineEnd == -1) lineEnd = str.Length;
					string lnStr = str.Substring(lineStart, lineEnd - lineStart).Trim();
					int lnInt = Int32.Parse(lnStr);

					// Check if result has a byte offset (paged file) or is in-page
					string numStr = str.Trim().Split(' ')[0];
					long loc = long.Parse(numStr);
					int limit = getLimit();

					if (limit > 0 && loc >= limit)
					{
						// Paged result: byte offset is large, jump to the correct page
						int page = (int)(loc / limit);
						Console.WriteLine(String.Format("Line double click offset:{0:n0} page:{1:n0} line:{2:n0}", loc, page, lnInt));

						buttonJumpToAnyPage(page);

						// Scroll to the matching text within the loaded page
						string searchText = textBoxSearchFile.Text.Trim();
						if (!string.IsNullOrEmpty(searchText))
						{
							HighlightWord(searchText);

							long pageStartByte = (long)page * limit;
							int approxCharPos = (int)(loc - pageStartByte);
							int textLen = TextArea.TextLength;
							if (approxCharPos >= textLen) approxCharPos = Math.Max(0, textLen - 1);

							int searchFrom = Math.Max(0, approxCharPos - 200);
							int pos = TextArea.Text.IndexOf(searchText, searchFrom, StringComparison.OrdinalIgnoreCase);
							if (pos < 0)
								pos = TextArea.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);

							if (pos >= 0)
							{
								int lineIdx = TextArea.LineFromPosition(pos);
								var linesOnScreen = TextArea.LinesOnScreen;
								int topLine = Math.Max(0, lineIdx - (linesOnScreen / 2));
								TextArea.FirstVisibleLine = topLine;
								TextArea.SetSelection(pos, pos + searchText.Length);
							}
						}
					}
					else
					{
						// In-page result: line number is within the visible text
						ScrollToLineNumber(lnInt);
					}
				}
				else
				{
					string numStr = str.Trim().Split(' ')[0];
					long loc = long.Parse(numStr);
					int limit = getLimit();
					int page = (limit > 0) ? (int)(loc / limit) : 0;

					Console.WriteLine(String.Format("Double click page:{0:n0} ", page));

					buttonJumpToAnyPage(page);
				}


			}
			catch (Exception ex)
			{

				Console.WriteLine(ex.Message);
			}
		}


		private void HighlightWord(string text)
		{
			if (string.IsNullOrEmpty(text))
				return;

			// Indicators 0-7 could be in use by a lexer
			// so we'll use indicator 8 to highlight words.
			const int NUM = 8;

			// Remove all uses of our indicator
			TextArea.IndicatorCurrent = NUM;
			TextArea.IndicatorClearRange(0, TextArea.TextLength);

			// Update indicator appearance
			TextArea.Indicators[NUM].Style = IndicatorStyle.StraightBox;
			TextArea.Indicators[NUM].Under = true;
			TextArea.Indicators[NUM].ForeColor = Color.Purple;
			TextArea.Indicators[NUM].OutlineAlpha = 90; //50;
			TextArea.Indicators[NUM].Alpha = 70; //30;

			// Search the document
			TextArea.TargetStart = 0;
			TextArea.TargetEnd = TextArea.TextLength;
			TextArea.SearchFlags = SearchFlags.None;
			while (TextArea.SearchInTarget(text) != -1)
			{
				// Mark the search results with the current indicator
				TextArea.IndicatorFillRange(TextArea.TargetStart, TextArea.TargetEnd - TextArea.TargetStart);

				// Search the remainder of the document
				TextArea.TargetStart = TextArea.TargetEnd;
				TextArea.TargetEnd = TextArea.TextLength;
			}
		}


		#region custom_line_numbers
		private void UpdateLineNumbers(int startingAtLine)
		{
			// Hide auto line numbers on margin 1
			TextArea.Margins[NUMBER_MARGIN].Width = 0;

			// Show custom global line numbers on margin 0
			TextArea.Margins[0].Type = MarginType.RightText;

			// Calculate width based on largest line number
			int maxLineNumber = startingAtLine + TextArea.Lines.Count;
			string maxText = new string('9', maxLineNumber.ToString().Length);
			int width = TextArea.TextWidth(Style.LineNumber, maxText) + 16;
			if (width < 55) width = 55;
			TextArea.Margins[0].Width = width;

			for (int i = 0; i < TextArea.Lines.Count; i++)
			{
				TextArea.Lines[i].MarginStyle = Style.LineNumber;
				TextArea.Lines[i].MarginText = (i + startingAtLine).ToString();
			}
		}

		private void ResetLineNumbers()
		{
			TextArea.Margins[0].Width = 0;
			TextArea.Margins[NUMBER_MARGIN].Width = 50;
			TextArea.Margins[NUMBER_MARGIN].Type = MarginType.Number;
		}

		private void ApplyLineNumbers(int page)
		{
			if (!FileUtils.fileHasLineBreaks) return;
			int limit = getLimit();
			if (limit <= 0) return;

			// Lazy build: index is built on first page navigation
			if (FileUtils.LineOffsetIndex == null)
			{
				this.Cursor = Cursors.WaitCursor;
				FileUtils.BuildLineIndex(FileUtils.CurFileName);
				this.Cursor = Cursors.Default;
			}

			long byteOffset = (long)page * limit;
			int startLine = FileUtils.GetLineNumberAtOffset(FileUtils.CurFileName, byteOffset);
			UpdateLineNumbers(startLine);
		}


        //private void scintilla_Insert(object sender, ModificationEventArgs e)
        //{
        //	// Only update line numbers if the number of lines changed
        //	if (e.LinesAdded != 0)
        //		UpdateLineNumbers(TextArea.LineFromPosition(e.Position));
        //}

        //private void scintilla_Delete(object sender, ModificationEventArgs e)
        //{
        //	// Only update line numbers if the number of lines changed
        //	if (e.LinesAdded != 0)
        //		UpdateLineNumbers(TextArea.LineFromPosition(e.Position));
        //}

		#endregion


		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			FileUtils.CleanupTempZipDir();
			FileUtils.CleanupTempEdiDir();
		}

	}
}
