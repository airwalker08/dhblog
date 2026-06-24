import { useCallback, useEffect, useState } from 'react';
import Autocomplete from '@mui/material/Autocomplete';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import Pagination from '@mui/material/Pagination';
import TextField from '@mui/material/TextField';
import { apiFetch } from '../api/client';
import type { BlogEntry, PagedBlogEntries, TopicSuggestion } from '../types';
import { useAuth } from '../auth/AuthContext';

const PAGE_SIZE = 10;

function BlogEntryCard({ entry }: { entry: BlogEntry }) {
  return (
    <div className="card blog-entry-card">
      <div className="card-content">
        <span className="card-title">{entry.title || 'Untitled'}</span>
        <p>{entry.text}</p>
        {entry.topics.length > 0 && (
          <div>
            {entry.topics.map((t) => (
              <span key={t} className="chip">
                {t}
              </span>
            ))}
          </div>
        )}
        {entry.images.map((img) => (
          <img key={img.imageId} src={img.url} alt="" className="blog-entry-image" />
        ))}
        <p className="grey-text">{new Date(entry.createdAt).toLocaleString()}</p>
      </div>
    </div>
  );
}

export function BlogPage() {
  const { user, hasFeature } = useAuth();
  const [pagedEntries, setPagedEntries] = useState<PagedBlogEntries | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [listError, setListError] = useState('');
  const [dialogOpen, setDialogOpen] = useState(false);
  const [title, setTitle] = useState('');
  const [text, setText] = useState('');
  const [topicInput, setTopicInput] = useState('');
  const [topics, setTopics] = useState<string[]>([]);
  const [suggestions, setSuggestions] = useState<TopicSuggestion[]>([]);
  const [formError, setFormError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const canWrite = hasFeature('BLOG', true);

  const loadEntries = useCallback(
    (pageNumber: number) => {
      if (!user) return;
      setLoading(true);
      setListError('');
      apiFetch<PagedBlogEntries>(
        `/api/blog/user/${user.userId}?page=${pageNumber}&pageSize=${PAGE_SIZE}`,
      )
        .then((result) => {
          setPagedEntries(result);
          if (result.totalPages > 0 && pageNumber > result.totalPages) {
            setPage(result.totalPages);
          }
        })
        .catch((e) => setListError(e.message))
        .finally(() => setLoading(false));
    },
    [user],
  );

  useEffect(() => {
    loadEntries(page);
  }, [loadEntries, page]);

  useEffect(() => {
    if (!topicInput.trim()) {
      setSuggestions([]);
      return;
    }
    const t = setTimeout(() => {
      apiFetch<TopicSuggestion[]>(`/api/topics/suggest?q=${encodeURIComponent(topicInput)}`)
        .then(setSuggestions)
        .catch(() => setSuggestions([]));
    }, 300);
    return () => clearTimeout(t);
  }, [topicInput]);

  const resetForm = () => {
    setTitle('');
    setText('');
    setTopics([]);
    setTopicInput('');
    setFormError('');
  };

  const openDialog = () => {
    resetForm();
    setDialogOpen(true);
  };

  const closeDialog = () => {
    if (submitting) return;
    setDialogOpen(false);
    resetForm();
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError('');
    setSubmitting(true);
    try {
      await apiFetch('/api/blog', {
        method: 'POST',
        body: JSON.stringify({ title, text, topics }),
      });
      setDialogOpen(false);
      resetForm();
      if (page === 1) loadEntries(1);
      else setPage(1);
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Failed to create entry');
    } finally {
      setSubmitting(false);
    }
  };

  const entries = pagedEntries?.items ?? [];
  const totalPages = pagedEntries?.totalPages ?? 0;

  return (
    <div>
      <div className="page-header">
        <h4 className="page-header__title">Blog</h4>
        {canWrite && (
          <Button variant="contained" onClick={openDialog}>
            Add new entry
          </Button>
        )}
      </div>

      {listError && <p className="red-text">{listError}</p>}
      {loading && !pagedEntries && <p className="grey-text">Loading entries…</p>}
      {!loading && entries.length === 0 && !listError && (
        <p className="grey-text">No blog entries yet.</p>
      )}

      {entries.map((entry) => (
        <BlogEntryCard key={entry.entryId} entry={entry} />
      ))}

      {totalPages > 1 && (
        <div className="pagination-row">
          <Pagination
            count={totalPages}
            page={page}
            onChange={(_, value) => setPage(value)}
            color="primary"
          />
        </div>
      )}

      <Dialog open={dialogOpen} onClose={closeDialog} fullWidth maxWidth="md">
        <DialogTitle>New blog entry</DialogTitle>
        <form onSubmit={handleCreate}>
          <DialogContent>
            {formError && <p className="red-text">{formError}</p>}
            <TextField
              autoFocus
              fullWidth
              label="Title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              margin="normal"
              required
            />
            <TextField
              fullWidth
              multiline
              minRows={5}
              label="What's on your mind?"
              value={text}
              onChange={(e) => setText(e.target.value)}
              margin="normal"
              required
            />
            <Autocomplete
              freeSolo
              options={suggestions.map((s) => s.displayText)}
              inputValue={topicInput}
              onInputChange={(_, v) => setTopicInput(v)}
              onChange={(_, value) => {
                if (value && !topics.includes(value)) {
                  setTopics([...topics, value]);
                  setTopicInput('');
                }
              }}
              renderInput={(params) => (
                <TextField {...params} label="Add topics" margin="normal" />
              )}
            />
            <div className="topic-chip-list">
              {topics.map((t) => (
                <span key={t} className="chip">
                  {t}
                  <i
                    className="close material-icons chip-close"
                    onClick={() => setTopics(topics.filter((x) => x !== t))}
                  >
                    close
                  </i>
                </span>
              ))}
            </div>
          </DialogContent>
          <DialogActions>
            <Button onClick={closeDialog} disabled={submitting}>
              Cancel
            </Button>
            <Button type="submit" variant="contained" disabled={!title.trim() || !text.trim() || submitting}>
              {submitting ? 'Posting…' : 'Post'}
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </div>
  );
}
