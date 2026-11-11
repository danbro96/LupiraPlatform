import { useState } from 'react';
import { StyleSheet, View } from 'react-native';
import { Chip } from 'react-native-paper';
import { copyText } from '@danbro96/lupira-expo-feedback/copy';
import { toastError } from '@danbro96/lupira-expo-feedback/toast';
import { spacing } from '@danbro96/lupira-tokens-core/spacing';
import { ICONS } from '../icons.ts';
import { fieldGap } from '../theme/styles.ts';
import { Button } from './Button.tsx';
import { useConfirm } from './ConfirmDialog.tsx';
import { Sheet } from './Sheet.tsx';
import { TextField } from './TextField.tsx';

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

/** A place's door and gate codes, masked until tapped; hold copies. With `editable`, tapping a shown code edits it
 *  in a sheet that also adds codes. The code itself is never logged. */
export function EntryCodes({ codes, editable = false, onSave, onRemove }: {
  codes: readonly EntryCode[];
  editable?: boolean;
  onSave: (code: EntryCodeDraft) => void;
  onRemove: (id: string) => void;
}) {
  const [shown, setShown] = useState<ReadonlySet<string>>(new Set());
  const [editing, setEditing] = useState<EntryCodeDraft | null>(null);

  if (codes.length === 0 && !editable) return null;
  return (
    <View style={styles.row}>
      {codes.map((c) => {
        const visible = shown.has(c.id);
        return (
          <Chip
            key={c.id}
            compact
            icon={ICONS.entryCode}
            onPress={() => (visible && editable ? setEditing(c) : setShown((s) => new Set(s).add(c.id)))}
            onLongPress={() => copyText(c.code, c.label)}
            accessibilityHint={visible && editable ? 'Tap to edit, hold to copy' : 'Tap to show, hold to copy'}
          >
            {`${c.label} ${visible ? c.code : '••••'}${c.note && visible ? ` · ${c.note}` : ''}`}
          </Chip>
        );
      })}
      {editable && <Button title="Add door code" variant="text" onPress={() => setEditing(NEW_CODE)} />}
      {editing && <EntryCodeSheet initial={editing} onSave={onSave} onRemove={onRemove} onDismiss={() => setEditing(null)} />}
    </View>
  );
}

function EntryCodeSheet({ initial, onSave, onRemove, onDismiss }: {
  initial: EntryCodeDraft;
  onSave: (code: EntryCodeDraft) => void;
  onRemove: (id: string) => void;
  onDismiss: () => void;
}) {
  const confirm = useConfirm();
  const [label, setLabel] = useState(initial.label);
  const [code, setCode] = useState(initial.code);
  const [note, setNote] = useState(initial.note ?? '');
  const existingId = initial.id;
  const valid = label.trim() !== '' && code.trim() !== '';

  const save = () => {
    if (!valid) return;
    onSave({ id: existingId, label: label.trim(), code: code.trim(), note: note.trim() || null });
    onDismiss();
  };
  // The confirm dialog's portal sits under any sheet opened after it, so the sheet closes first.
  const remove = (id: string) => {
    onDismiss();
    confirm({ title: `Remove ${initial.label}?`, message: 'Everyone living here loses this code.', confirmLabel: 'Remove', destructive: true })
      .then((ok) => {
        if (ok) onRemove(id);
      })
      .catch(() => toastError('Remove failed'));
  };

  return (
    <Sheet title={existingId ? 'Edit door code' : 'Add door code'} onDismiss={onDismiss}>
      <TextField label="Opens" value={label} onChangeText={setLabel} />
      <TextField label="Code" value={code} onChangeText={setCode} autoComplete="off" autoCorrect={false} autoFocus={!!existingId} style={fieldGap} />
      <TextField label="Note" value={note} onChangeText={setNote} placeholder="Ring 2nd floor" style={fieldGap} />
      <View style={styles.actions}>
        {existingId && <Button title="Remove" variant="destructive" onPress={() => remove(existingId)} />}
        <Button title="Save" onPress={save} disabled={!valid} />
      </View>
    </Sheet>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', gap: spacing.sm },
  actions: { flexDirection: 'row', justifyContent: 'flex-end', gap: spacing.sm, marginTop: spacing.md },
});
