
import type { Block, TableMetadata } from '../../../types';
import { parseMetadata, stringifyMetadata } from '../../../utils/blocks';

interface Props {
  block: Block;
  isEditing: boolean;
  onContentChange: (updated: Block) => void;
}

export function TableBlock({ block, isEditing, onContentChange }: Props) {
  const meta = parseMetadata<TableMetadata>(block.metadata);
  const rows = meta.rows ?? [['', '']];
  const headers = meta.headers ?? rows[0]?.map((_, i) => `Column ${i + 1}`) ?? [];

  const updateMeta = (next: TableMetadata) => {
    onContentChange({ ...block, metadata: stringifyMetadata(next as any) });
  };

  const setCell = (rowIdx: number, colIdx: number, value: string) => {
    const nextRows = rows.map((r, ri) =>
      ri === rowIdx ? r.map((c, ci) => (ci === colIdx ? value : c)) : r
    );
    updateMeta({ ...meta, rows: nextRows });
  };

  const setHeader = (colIdx: number, value: string) => {
    const nextHeaders = headers.map((h, i) => (i === colIdx ? value : h));
    updateMeta({ ...meta, headers: nextHeaders });
  };

  const addRow = () => {
    const newRow = new Array(headers.length).fill('');
    updateMeta({ ...meta, rows: [...rows, newRow] });
  };

  const removeRow = (rowIdx: number) => {
    if (rows.length <= 1) return;
    updateMeta({ ...meta, rows: rows.filter((_, i) => i !== rowIdx) });
  };

  const addColumn = () => {
    const nextRows = rows.map((r) => [...r, '']);
    const nextHeaders = [...headers, `Column ${headers.length + 1}`];
    updateMeta({ ...meta, rows: nextRows, headers: nextHeaders });
  };

  const removeColumn = (colIdx: number) => {
    if (headers.length <= 1) return;
    const nextRows = rows.map((r) => r.filter((_, i) => i !== colIdx));
    const nextHeaders = headers.filter((_, i) => i !== colIdx);
    updateMeta({ ...meta, rows: nextRows, headers: nextHeaders });
  };

  return (
    <div style={{ overflowX: 'auto' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
        <thead>
          <tr>
            {headers.map((h, ci) => (
              <th key={ci} style={{ position: 'relative', borderBottom: '2px solid var(--border-color)', padding: '4px 8px', textAlign: 'left', fontWeight: 600, background: 'var(--bg-secondary)' }}>
                <span
                  contentEditable={isEditing}
                  suppressContentEditableWarning
                  onBlur={(e) => setHeader(ci, e.currentTarget.textContent ?? '')}
                  style={{ outline: 'none', display: 'block' }}
                  className={isEditing ? 'selectable' : ''}
                >
                  {h}
                </span>
                {isEditing && (
                  <button
                    onClick={() => removeColumn(ci)}
                    style={{ position: 'absolute', top: 2, right: 2, background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)', fontSize: 10, padding: 2 }}
                    title="Remove column"
                  >×</button>
                )}
              </th>
            ))}
            {isEditing && (
              <th style={{ border: 'none', padding: '4px 6px', background: 'var(--bg-secondary)' }}>
                <button
                  onClick={addColumn}
                  style={{ background: 'none', border: '1px dashed var(--border-color)', cursor: 'pointer', color: 'var(--text-muted)', borderRadius: 'var(--radius-sm)', padding: '2px 6px', fontSize: 12 }}
                >+</button>
              </th>
            )}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, ri) => (
            <tr key={ri} style={{ background: ri % 2 === 0 ? 'none' : 'var(--bg-secondary)' }}>
              {row.map((cell, ci) => (
                <td key={ci} style={{ border: '1px solid var(--border-subtle)', padding: '4px 8px' }}>
                  <span
                    contentEditable={isEditing}
                    suppressContentEditableWarning
                    onBlur={(e) => setCell(ri, ci, e.currentTarget.textContent ?? '')}
                    style={{ outline: 'none', display: 'block', minWidth: 60 }}
                    className={isEditing ? 'selectable' : ''}
                  >
                    {cell}
                  </span>
                </td>
              ))}
              {isEditing && (
                <td style={{ border: 'none', padding: '2px 4px' }}>
                  <button
                    onClick={() => removeRow(ri)}
                    style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)', fontSize: 12, padding: 2 }}
                    title="Remove row"
                  >×</button>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
      {isEditing && (
        <button
          onClick={addRow}
          style={{
            marginTop: 4, background: 'none', border: '1px dashed var(--border-color)',
            cursor: 'pointer', color: 'var(--text-muted)', borderRadius: 'var(--radius-sm)',
            padding: '3px 12px', fontSize: 12, width: '100%',
          }}
        >+ Add row</button>
      )}
    </div>
  );
}
