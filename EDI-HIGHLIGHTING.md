# Configurable X12 highlighting

The viewer detects X12 from the fixed-width `ISA` header, not the file extension. It reads a bounded prefix for the first `GS` and `ST` segments. `ST01` identifies the transaction; `ST03`, or `GS08` when `ST03` is omitted, identifies its implementation version. Each business file is expected to contain only one transaction type. XML and other text files have no EDI highlighting.

At startup the viewer reads `edi-highlighting.xml` beside `AnyText.exe` (for example, `ScintillaNET.Demo\bin\Debug\edi-highlighting.xml`). The build copies `ScintillaNET.Demo\edi-highlighting.xml` there when the project file is newer than the output copy. Edit the XML beside the executable and restart the viewer to apply changes without recompiling; if you edit the project copy instead, build again to update the output. Rebuilds and deployments can replace an edited output copy when the project copy is newer. The old `%AppData%\AnyText\edi-highlighting.xml` is no longer read or modified. The View > Highlight EDI toggle controls both line backgrounds and colored indicators. An unrecognized transaction is still viewable but gets no transaction-specific highlighting. If the output XML is missing or invalid, the viewer reports a warning in the bottom pane and uses the embedded defaults without overwriting the output file. Older output XML containing `<Boundary>` must be changed to `<Highlight elementsToHighlight="row">` and restarted; otherwise the viewer warns and uses its bundled rules.

## Default rules

| Transaction | Row highlight | Segment/element indicators |
| --- | --- | --- |
| 837 Institutional (`X223`), Professional (`X222`), or generic 837 | `HL03=22` | `CLM`, `LX` |
| 834 | `INS` | `HD`, REF02 when REF01 is `0F` |
| 277CA (`X214`) | `HL03=19` or `HL03=PT` | Matching `HL` and `HL03`, plus `STC` |
| 835 | `CLP` | `CLP`, `SVC` |
| 999 | `AK2` | `AK2`, `IK3`, `IK4`, `IK5`, `AK9` |

TA1 is not configured.

## Rule format

The root element is `EdiHighlighting`. Each `Transaction` requires a three-digit `id` (the `ST01` value) and can include a `version` substring from `ST03`/`GS08`. The longest matching `version` wins; a transaction with no `version` is the fallback for that `id`. A `name` is optional.

- `Highlight` marks a matching segment ID with a red indicator. For example, `<Highlight segment="SVC" />`.
- `elementsToHighlight="row"` colors only the entire matching line, leaving other lines unshaded and the text foreground unchanged. For example, `<Highlight segment="HL" qualifier="22" qualPosition="3" elementsToHighlight="row" />` colors only `HL03=22` lines. A row rule does not also draw a segment or element box; add another `<Highlight>` rule if both effects are needed.
- Add `element="3" value="PT"` to a rule to require an exact element value (for example, `HL03=PT`). Element numbering starts at 1 after the segment ID; matching uses the file's ISA element delimiter.
- `highlightSegment="true"` marks the segment ID. It defaults to true when `elementsToHighlight` is absent and false when it is present.
- `highlightValue="true"` also marks the matching element value; it requires `element` and `value`.
- `qualPosition="1" qualifier="0F"` is an alternative to `element="1" value="0F"` for matching a segment's qualifier. The two pairs cannot be combined on one rule.
- `elementsToHighlight="2"` marks only element 2 in a matched segment; `elementsToHighlight="0,2,3"` marks the segment ID and elements 2 and 3. Position 0 means the segment ID (for example `REF`), while positions 1–99 use one-based X12 element numbering (REF01, REF02, etc.). Absent or empty elements are not marked. The list must contain distinct positions and cannot be combined with `row`, `highlightSegment`, or `highlightValue`.
- For example, `<Highlight segment="REF" qualifier="0F" qualPosition="1" elementsToHighlight="2" />` matches REF01=`0F` and boxes only REF02, leaving `REF`, `0F`, and the text foreground unchanged. Existing rules without these attributes behave as before.

## Colors

- On `<EdiHighlighting>`, `defaultBoundaryColor="#C8E1FF"` sets the row background and `defaultHighlightColor="#FF0000"` sets the segment/element indicator color. Omitted attributes retain the original blue and red defaults.
- On `<Transaction>`, either default attribute overrides the corresponding root color for that transaction.
- On a row `<Highlight>`, `color` overrides its matching line background. On other `<Highlight>` rules, `color` overrides their rounded-box indicator only. The original text foreground is unchanged. For example, `<Highlight segment="AK9" color="#90EE90" />` draws a light-green box around `999` AK9.
- Colors can be six-digit `#RRGGBB` values or known color names (such as `LightGreen`). Invalid colors cause the entire output configuration to fall back to the embedded defaults with a warning. Each transaction supports up to six distinct boundary colors and eleven distinct highlight colors because Scintilla has a finite marker/indicator palette.

For example, to configure another ST-based transaction, add a `Transaction` with its `id` and the desired `Highlight` elements. No recompile is needed. Version-specific transactions (such as 277CA) should include `version` to avoid matching a different implementation of the same ST number. See `ScintillaNET.Demo/edi-highlighting.xml` for the bundled definitions.

The viewer reads only the first 64 KB after the ISA segment to locate the first `ST`. Files whose first `ST` falls outside that prefix remain viewable as EDI, but no transaction-specific rules are selected. Paging does not reread the header or load the complete file into memory.
