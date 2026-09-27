# Configurable X12 highlighting

The viewer detects X12 from the fixed-width `ISA` header, not the file extension. It reads a bounded prefix for the first `GS` and `ST` segments. `ST01` identifies the transaction; `ST03`, or `GS08` when `ST03` is omitted, identifies its implementation version. Each business file is expected to contain only one transaction type. XML and other text files have no EDI highlighting.

The default rules are installed on first launch to `%AppData%\AnyText\edi-highlighting.xml`. Edit this file and restart the viewer to apply changes. The View > EDI Record Boundaries toggle controls both line backgrounds and red indicators. An unrecognized transaction is still viewable but gets no transaction-specific highlighting. If the file cannot be read or has invalid XML/rules, the viewer reports a warning in the bottom pane and uses the bundled defaults without replacing the user's file.

## Default rules

| Transaction | Boundary line | Red segment/element indicators |
| --- | --- | --- |
| 837 Institutional (`X223`), Professional (`X222`), or generic 837 | `HL03=22` | `CLM`, `LX` |
| 834 | `INS` | `HD` |
| 277CA (`X214`) | `HL03=19` or `HL03=PT` | Matching `HL` and `HL03` |
| 835 | `CLP` | `CLP`, `SVC` |
| 999 | `AK2` | `AK2`, `IK3`, `IK4`, `IK5`, `AK9` |

TA1 is not configured.

## Rule format

The root element is `EdiHighlighting`. Each `Transaction` requires a three-digit `id` (the `ST01` value) and can include a `version` substring from `ST03`/`GS08`. The longest matching `version` wins; a transaction with no `version` is the fallback for that `id`. A `name` is optional.

- `Highlight` marks a matching segment ID with a red indicator. For example, `<Highlight segment="SVC" />`.
- `Boundary` marks the entire matching line and alternates the background of the lines between boundaries. For example, `<Boundary segment="CLP" highlightSegment="true" />`.
- Add `element="3" value="PT"` to either rule to require an exact element value (for example, `HL03=PT`). Element numbering starts at 1 after the segment ID; matching uses the file's ISA element delimiter.
- `highlightSegment="true"` marks the segment ID. It defaults to true for `Highlight` and false for `Boundary`.
- `highlightValue="true"` also marks the matching element value; it requires `element` and `value`.

For example, to configure another ST-based transaction, add a `Transaction` with its `id` and the desired `Boundary`/`Highlight` elements. No recompile is needed. Version-specific transactions (such as 277CA) should include `version` to avoid matching a different implementation of the same ST number. See `ScintillaNET.Demo/edi-highlighting.xml` for the bundled definitions.

The viewer reads only the first 64 KB after the ISA segment to locate the first `ST`. Files whose first `ST` falls outside that prefix remain viewable as EDI, but no transaction-specific rules are selected. Paging does not reread the header or load the complete file into memory.
