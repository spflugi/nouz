import { useState, useCallback } from 'react';
import { useSettingsStore } from '../../store/settingsStore';
import { useDebounce } from '../../hooks/useDebounce';
import * as db from '../../services/db';

interface Props {
  onClose: () => void;
}

export function SettingsPage({ onClose }: Props) {
  const settings = useSettingsStore();

  const [chatModel, setChatModel] = useState(settings.chatModel);
  const [embeddingModel, setEmbeddingModel] = useState(settings.embeddingModel);
  const [apiKey, setApiKey] = useState(settings.openAiApiKey);
  const [adminKey, setAdminKey] = useState(settings.openAiAdminKey);
  const [topN, setTopN] = useState(String(settings.topNRelevantNotes));
  const [threshold, setThreshold] = useState(settings.minSimilarityThreshold.toFixed(2));

  const saveApiKey = useDebounce((v: string) => settings.setOpenAiApiKey(v), 800);
  const saveAdminKey = useDebounce((v: string) => settings.setOpenAiAdminKey(v), 800);
  const saveChatModel = useDebounce((v: string) => settings.setChatModel(v), 800);
  const saveEmbeddingModel = useDebounce((v: string) => settings.setEmbeddingModel(v), 800);
  const saveTopN = useDebounce((v: string) => settings.setTopNRelevantNotes(parseInt(v, 10) || 0), 800);
  const saveThreshold = useDebounce((v: string) => settings.setMinSimilarityThreshold(parseFloat(v) || 0), 800);

  return (
    <div
      onClick={onClose}
      style={{
        position: 'fixed', inset: 0, zIndex: 400,
        background: 'rgba(0,0,0,0.3)', display: 'flex', alignItems: 'center', justifyContent: 'center',
      }}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          background: 'var(--bg-card)', border: '1px solid var(--border-color)',
          borderRadius: 'var(--radius-lg)', boxShadow: 'var(--shadow-lg)',
          width: 520, maxHeight: '85vh', overflow: 'auto', padding: '24px',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 20 }}>
          <h2 style={{ fontSize: 16, fontWeight: 600, color: 'var(--text-primary)' }}>Settings</h2>
          <button
            onClick={onClose}
            style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)', padding: 4 }}
          >✕</button>
        </div>

        {/* Appearance */}
        <SettingsCard title="Appearance" icon="🎨">
          <SettingRow label="Theme" description="Choose your preferred color scheme">
            <div style={{ display: 'flex', gap: 6 }}>
              <ThemeButton active={settings.theme === 'light'} onClick={() => settings.setTheme('light')}>
                ☀ Light
              </ThemeButton>
              <ThemeButton active={settings.theme === 'dark'} onClick={() => settings.setTheme('dark')}>
                ☾ Dark
              </ThemeButton>
            </div>
          </SettingRow>
        </SettingsCard>

        {/* OpenAI */}
        <SettingsCard title="OpenAI" icon="🤖" style={{ marginTop: 16 }}>
          <SettingRow label="API Key" description="Your OpenAI API key for the AI assistant">
            <SettingInput
              type="password"
              placeholder="sk-..."
              value={apiKey}
              onChange={(v) => { setApiKey(v); saveApiKey(v); }}
            />
          </SettingRow>
          <SettingRow label="Admin API Key" description="Your OpenAI Admin key (optional, for usage data)">
            <SettingInput
              type="password"
              placeholder="sk-admin-..."
              value={adminKey}
              onChange={(v) => { setAdminKey(v); saveAdminKey(v); }}
            />
          </SettingRow>
          <SettingRow label="Chat Model" description="Model used for chat (e.g. gpt-4o-mini, gpt-4o)">
            <SettingInput
              placeholder="gpt-4o-mini"
              value={chatModel}
              onChange={(v) => { setChatModel(v); saveChatModel(v); }}
            />
          </SettingRow>
          <SettingRow label="Embedding Model" description="Model used for semantic search">
            <SettingInput
              placeholder="text-embedding-3-small"
              value={embeddingModel}
              onChange={(v) => { setEmbeddingModel(v); saveEmbeddingModel(v); }}
            />
          </SettingRow>
          <SettingRow label="Context Notes" description="Number of notes included as AI context (0 disables RAG)">
            <SettingInput
              type="number"
              value={topN}
              onChange={(v) => { setTopN(v); saveTopN(v); }}
              style={{ width: 80 }}
            />
          </SettingRow>
          <SettingRow label="Similarity Threshold" description="Minimum similarity score for RAG (0.0 – 1.0)">
            <SettingInput
              type="number"
              value={threshold}
              onChange={(v) => { setThreshold(v); saveThreshold(v); }}
              style={{ width: 80 }}
            />
          </SettingRow>
          <UsageSection adminKey={adminKey} />
        </SettingsCard>
      </div>
    </div>
  );
}

function UsageSection({ adminKey }: { adminKey: string }) {
  const [status, setStatus] = useState<'idle' | 'loading' | 'done' | 'error'>('idle');
  const [data, setData] = useState<db.OpenAiUsage | null>(null);
  const [error, setError] = useState('');

  const loadUsage = useCallback(async () => {
    if (!adminKey) {
      setError('No Admin API key configured.');
      setStatus('error');
      return;
    }
    setStatus('loading');
    try {
      const now = Math.floor(Date.now() / 1000);
      const d = new Date();
      const startOfMonth = Math.floor(new Date(d.getFullYear(), d.getMonth(), 1).getTime() / 1000);
      const result = await db.getOpenAiUsage(adminKey, startOfMonth, now);
      setData(result);
      setStatus('done');
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
      setStatus('error');
    }
  }, [adminKey]);

  const monthLabel = new Date().toLocaleString('default', { month: 'long', year: 'numeric' });

  return (
    <div style={{ padding: '10px 14px', borderBottom: '1px solid var(--border-subtle)' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
        <div>
          <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--text-primary)' }}>Usage &amp; Costs</div>
          <div style={{ fontSize: 11, color: 'var(--text-muted)', marginTop: 2 }}>
            {status === 'done' ? monthLabel : 'Current month — fetched on demand'}
          </div>
        </div>
        <button
          onClick={loadUsage}
          disabled={status === 'loading'}
          style={{
            padding: '5px 11px', fontSize: 12, borderRadius: 'var(--radius-sm)',
            border: '1px solid var(--border-color)', background: 'var(--bg-secondary)',
            color: status === 'loading' ? 'var(--text-muted)' : 'var(--text-secondary)',
            cursor: status === 'loading' ? 'default' : 'pointer',
            whiteSpace: 'nowrap', flexShrink: 0,
          }}
        >
          {status === 'loading' ? 'Loading…' : status === 'done' ? 'Refresh' : 'Check usage'}
        </button>
      </div>

      {status === 'error' && (
        <div style={{ marginTop: 8, fontSize: 11, color: 'var(--color-error)', background: 'var(--color-error-bg)', padding: '5px 8px', borderRadius: 'var(--radius-sm)' }}>
          {error}
        </div>
      )}

      {status === 'done' && data && (
        <div style={{
          marginTop: 10, display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 6,
        }}>
          {[
            { label: 'Cost', value: `$${data.cost.toFixed(4)}` },
            { label: 'Requests', value: data.requests.toLocaleString() },
            { label: 'Input tokens', value: fmtTokens(data.inputTokens) },
            { label: 'Output tokens', value: fmtTokens(data.outputTokens) },
          ].map(({ label, value }) => (
            <div key={label} style={{
              background: 'var(--bg-secondary)', borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--border-subtle)', padding: '6px 8px', textAlign: 'center',
            }}>
              <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-primary)' }}>{value}</div>
              <div style={{ fontSize: 10, color: 'var(--text-muted)', marginTop: 2 }}>{label}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function fmtTokens(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(2)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return String(n);
}

function SettingsCard({ title, icon, children, style }: { title: string; icon: string; children: React.ReactNode; style?: React.CSSProperties }) {
  return (
    <div style={{ border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)', overflow: 'hidden', ...style }}>
      <div style={{ padding: '10px 14px', borderBottom: '1px solid var(--border-subtle)', display: 'flex', gap: 8, alignItems: 'center', background: 'var(--bg-secondary)' }}>
        <span>{icon}</span>
        <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-secondary)' }}>{title}</span>
      </div>
      <div style={{ padding: '4px 0' }}>{children}</div>
    </div>
  );
}

function SettingRow({ label, description, children }: { label: string; description: string; children: React.ReactNode }) {
  return (
    <div style={{ padding: '10px 14px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16, borderBottom: '1px solid var(--border-subtle)' }}>
      <div>
        <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--text-primary)' }}>{label}</div>
        <div style={{ fontSize: 11, color: 'var(--text-muted)', marginTop: 2 }}>{description}</div>
      </div>
      {children}
    </div>
  );
}

function SettingInput({ value, onChange, placeholder, type = 'text', style }: {
  value: string; onChange: (v: string) => void; placeholder?: string; type?: string; style?: React.CSSProperties;
}) {
  return (
    <input
      type={type}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      className="selectable"
      style={{
        padding: '5px 9px', fontSize: 12, border: '1px solid var(--border-color)',
        borderRadius: 'var(--radius-sm)', background: 'var(--bg-secondary)',
        color: 'var(--text-primary)', outline: 'none', width: 180, ...style,
      }}
    />
  );
}

function ThemeButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      style={{
        padding: '5px 12px', fontSize: 12, borderRadius: 'var(--radius-sm)',
        border: `1px solid ${active ? 'var(--accent)' : 'var(--border-color)'}`,
        background: active ? 'var(--accent)' : 'none',
        color: active ? 'var(--bg-primary)' : 'var(--text-secondary)',
        cursor: 'pointer', fontWeight: active ? 600 : 400,
        transition: 'all var(--transition-fast)',
      }}
    >
      {children}
    </button>
  );
}
