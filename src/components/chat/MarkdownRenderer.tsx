
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

interface Props {
  content: string;
}

export function MarkdownRenderer({ content }: Props) {
  return (
    <div style={{ lineHeight: 1.6 }}>
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          p: ({ children }) => <p style={{ margin: '0 0 6px' }}>{children}</p>,
          h1: ({ children }) => <h1 style={{ fontSize: '1.3em', fontWeight: 700, margin: '8px 0 4px' }}>{children}</h1>,
          h2: ({ children }) => <h2 style={{ fontSize: '1.1em', fontWeight: 600, margin: '8px 0 4px' }}>{children}</h2>,
          h3: ({ children }) => <h3 style={{ fontSize: '1em', fontWeight: 600, margin: '6px 0 3px' }}>{children}</h3>,
          ul: ({ children }) => <ul style={{ paddingLeft: 16, margin: '4px 0' }}>{children}</ul>,
          ol: ({ children }) => <ol style={{ paddingLeft: 16, margin: '4px 0' }}>{children}</ol>,
          li: ({ children }) => <li style={{ margin: '2px 0' }}>{children}</li>,
          code: ({ inline, children }: any) =>
            inline ? (
              <code style={{
                background: 'var(--bg-secondary)', padding: '1px 4px',
                borderRadius: 3, fontFamily: 'monospace', fontSize: '0.9em',
              }}>{children}</code>
            ) : (
              <pre style={{
                background: 'var(--bg-secondary)', padding: '8px 12px',
                borderRadius: 'var(--radius-md)', overflow: 'auto',
                fontSize: '0.85em', margin: '6px 0',
                fontFamily: 'monospace',
              }}>
                <code>{children}</code>
              </pre>
            ),
          blockquote: ({ children }) => (
            <blockquote style={{
              borderLeft: '3px solid var(--border-color)', paddingLeft: 10,
              color: 'var(--text-secondary)', margin: '6px 0', fontStyle: 'italic',
            }}>
              {children}
            </blockquote>
          ),
          a: ({ href, children }) => (
            <a href={href} style={{ color: 'var(--accent)', textDecoration: 'underline' }} target="_blank" rel="noreferrer">
              {children}
            </a>
          ),
          strong: ({ children }) => <strong style={{ fontWeight: 600 }}>{children}</strong>,
          em: ({ children }) => <em style={{ fontStyle: 'italic' }}>{children}</em>,
          table: ({ children }) => (
            <table style={{ borderCollapse: 'collapse', width: '100%', fontSize: '0.9em', margin: '6px 0' }}>{children}</table>
          ),
          th: ({ children }) => <th style={{ border: '1px solid var(--border-color)', padding: '4px 8px', background: 'var(--bg-secondary)', fontWeight: 600, textAlign: 'left' }}>{children}</th>,
          td: ({ children }) => <td style={{ border: '1px solid var(--border-subtle)', padding: '4px 8px' }}>{children}</td>,
          hr: () => <hr style={{ border: 'none', borderTop: '1px solid var(--border-color)', margin: '8px 0' }} />,
        }}
      >
        {content}
      </ReactMarkdown>
    </div>
  );
}
