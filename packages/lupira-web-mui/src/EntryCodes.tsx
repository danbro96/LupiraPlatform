import { useState, type FormEvent } from 'react';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogTitle from '@mui/material/DialogTitle';
import IconButton from '@mui/material/IconButton';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import Tooltip from '@mui/material/Tooltip';
import { CopyIcon, EditIcon, EntryCodeIcon } from './icons.ts';
import { useCopy } from './useCopy.ts';

export interface EntryCode {
  id: string;
  /** What it opens, e.g. "Port" or "Gate". */
  label: string;
  code: string;
  note: string | null;
}

/** `id` null: a code being added; the caller mints its id. */
export type EntryCodeDraft = Omit<EntryCode, 'id'> & { id: string | null };

const NEW_CODE: EntryCodeDraft = { id: null, label: 'Port', code: '', note: null };

/** A place's door and gate codes, masked until clicked. A shown code copies, and with `editable` opens the form
 *  that also adds codes. */
export function EntryCodes({ codes, editable = false, onSave, onRemove }: {
  codes: readonly EntryCode[];
  editable?: boolean;
  onSave: (code: EntryCodeDraft) => void;
  onRemove: (id: string) => void;
}) {
  const [shown, setShown] = useState<ReadonlySet<string>>(new Set());
  const [editing, setEditing] = useState<EntryCodeDraft | null>(null);
  const copy = useCopy();

  if (codes.length === 0 && !editable) return null;
  return (
    <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
      {codes.map((c) => {
        const visible = shown.has(c.id);
        return (
          <Box key={c.id} sx={{ display: 'inline-flex', alignItems: 'center' }}>
            <Chip
              variant="outlined"
              icon={<EntryCodeIcon fontSize="small" />}
              label={`${c.label} ${visible ? c.code : '••••'}${visible && c.note ? ` · ${c.note}` : ''}`}
              onClick={visible ? undefined : () => setShown((s) => new Set(s).add(c.id))}
            />
            {visible && (
              <Tooltip title={`Copy ${c.label}`}>
                <IconButton size="small" onClick={() => copy(c.code, c.label)}><CopyIcon fontSize="small" /></IconButton>
              </Tooltip>
            )}
            {visible && editable && (
              <Tooltip title={`Edit ${c.label}`}>
                <IconButton size="small" onClick={() => setEditing(c)}><EditIcon fontSize="small" /></IconButton>
              </Tooltip>
            )}
          </Box>
        );
      })}
      {editable && <Button variant="text" size="small" onClick={() => setEditing(NEW_CODE)}>Add door code</Button>}
      {editing && <EntryCodeDialog initial={editing} onSave={onSave} onRemove={onRemove} onClose={() => setEditing(null)} />}
    </Stack>
  );
}

function EntryCodeDialog({ initial, onSave, onRemove, onClose }: {
  initial: EntryCodeDraft;
  onSave: (code: EntryCodeDraft) => void;
  onRemove: (id: string) => void;
  onClose: () => void;
}) {
  const [label, setLabel] = useState(initial.label);
  const [code, setCode] = useState(initial.code);
  const [note, setNote] = useState(initial.note ?? '');
  const [confirming, setConfirming] = useState(false);
  const existingId = initial.id;

  if (confirming && existingId) {
    return (
      <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
        <DialogTitle>Remove {initial.label}?</DialogTitle>
        <DialogContent>
          <DialogContentText>Everyone living here loses this code.</DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirming(false)}>Cancel</Button>
          <Button color="error" onClick={() => { onRemove(existingId); onClose(); }}>Remove</Button>
        </DialogActions>
      </Dialog>
    );
  }

  const valid = label.trim() !== '' && code.trim() !== '';
  const submit = (e: FormEvent) => {
    e.preventDefault();
    if (!valid) return;
    onSave({ id: existingId, label: label.trim(), code: code.trim(), note: note.trim() || null });
    onClose();
  };
  return (
    <Dialog open onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{existingId ? 'Edit door code' : 'Add door code'}</DialogTitle>
      <form onSubmit={submit}>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField size="small" label="Opens" value={label} onChange={(e) => setLabel(e.target.value)} autoFocus={!existingId} />
          <TextField size="small" label="Code" value={code} onChange={(e) => setCode(e.target.value)} autoComplete="off" autoFocus={!!existingId} />
          <TextField size="small" label="Note" value={note} onChange={(e) => setNote(e.target.value)} placeholder="Ring 2nd floor" />
        </DialogContent>
        <DialogActions>
          {existingId && <Button color="error" onClick={() => setConfirming(true)} sx={{ mr: 'auto' }}>Remove</Button>}
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={!valid}>Save</Button>
        </DialogActions>
      </form>
    </Dialog>
  );
}
