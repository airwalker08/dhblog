import { useEffect, useState } from 'react';

export function DiagnosticsPage() {
  const [data, setData] = useState<object | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    fetch('/api/diagnostics', {
      headers: { Authorization: `Bearer ${localStorage.getItem('dhblog_token')}` },
    })
      .then((r) => r.json())
      .then(setData)
      .catch((e) => setError(e.message));
  }, []);

  return (
    <div>
      <h4>Diagnostics</h4>
      {error && <p className="red-text">{error}</p>}
      {data && (
        <pre className="card-panel diagnostics-pre">
          {JSON.stringify(data, null, 2)}
        </pre>
      )}
    </div>
  );
}
