import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, View } from 'react-native';
import { Card, List, ProgressBar, Text } from 'react-native-paper';
import type { Palette } from '@danbro96/lupira-tokens-core/color';
import { radii, spacing } from '@danbro96/lupira-tokens-core/spacing';
import { changeCount, syncStatusLine, type ParkedChange, type ParkedChangeOp, type SyncViewStatus } from '../status.ts';
import { useColors } from '../theme/useColors.ts';
import { Button } from './Button.tsx';
import { useConfirm } from './ConfirmDialog.tsx';

type WarningPalette = Palette & { warning?: string };

export interface SyncStatusViewProps<Op extends ParkedChangeOp = ParkedChangeOp> {
  status: SyncViewStatus;
  parked: readonly ParkedChange<Op>[];
  opLabel: (op: Op) => string;
  onSync: () => void;
  onRetry: (commandId: string) => void;
  onDiscard: (commandId: string) => void;
  onRetryAll?: () => void;
  online?: boolean;
  /** Names each aggregate's docs in the progress line ("events"); the aggregate name is the fallback. */
  progressLabels?: Record<string, string>;
}

/** The body of an app's sync screen: connection and last sync, a running sync's progress, changes the server
 *  did not accept (retry / discard / details) and the queued count. The app owns the screen and the data. */
export function SyncStatusView<Op extends ParkedChangeOp>({
  status, parked, opLabel, onSync, onRetry, onDiscard, onRetryAll, online = true, progressLabels = {},
}: SyncStatusViewProps<Op>) {
  const c = useColors<WarningPalette>();
  const { phase, progress, pending, lastError } = status;
  const syncing = phase !== 'idle';

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <View style={styles.statusRow}>
        <Text style={[styles.statusText, { color: c.textMuted }]}>{syncStatusLine(status, online)}</Text>
        <Button title="Sync now" variant="secondary" onPress={onSync} disabled={syncing || !online} />
      </View>
      {lastError ? <Text style={[styles.small, { color: c.warning ?? c.danger }]}>{lastError}</Text> : null}

      {syncing ? (
        <View style={styles.progressBlock}>
          <ProgressBar indeterminate />
          <Text style={[styles.progressText, { color: c.textMuted }]}>
            {progress ? `${progress.count} ${progressLabels[progress.aggregate] ?? progress.aggregate}…` : 'Starting…'}
          </Text>
        </View>
      ) : null}

      {!syncing && parked.length === 0 && pending === 0 ? (
        <Text style={[styles.empty, { color: c.textMuted }]}>All changes are synced.</Text>
      ) : null}

      {parked.length > 0 ? (
        <View style={styles.sectionHead}>
          <List.Subheader style={[styles.subheader, styles.grow]}>Needs attention</List.Subheader>
          {onRetryAll && parked.length > 1 ? <Button title="Retry all" variant="text" onPress={onRetryAll} /> : null}
        </View>
      ) : null}
      {parked.map(row => (
        <ParkedCard key={row.op.commandId} row={row} label={opLabel(row.op)} onRetry={onRetry} onDiscard={onDiscard} />
      ))}

      {pending > 0 ? <List.Subheader style={styles.subheader}>Waiting to sync</List.Subheader> : null}
      {pending > 0 ? <Text style={[styles.small, { color: c.textMuted }]}>{changeCount(pending)} queued</Text> : null}
    </ScrollView>
  );
}

function ParkedCard({ row, label, onRetry, onDiscard }: {
  row: ParkedChange;
  label: string;
  onRetry: (commandId: string) => void;
  onDiscard: (commandId: string) => void;
}) {
  const c = useColors<WarningPalette>();
  const confirm = useConfirm();
  const [expanded, setExpanded] = useState(false);

  async function discard() {
    const ok = await confirm({
      title: 'Discard change',
      message: `Discard “${label}”? The local edit is undone and the server’s version is restored.`,
      confirmLabel: 'Discard',
      destructive: true,
    });
    if (ok) onDiscard(row.op.commandId);
  }

  return (
    <Card mode="outlined" style={styles.card} theme={{ colors: { outline: c.warning ?? c.danger } }}>
      <Card.Content>
        <Pressable onPress={() => setExpanded(!expanded)}>
          <Text style={styles.opLabel}>{label}</Text>
          <Text style={[styles.small, { color: c.textMuted }]}>
            {new Date(row.op.occurredAt).toLocaleString()} · {row.attempts} attempt{row.attempts === 1 ? '' : 's'}
          </Text>
          {row.lastError ? (
            <Text style={[styles.small, { color: c.danger }]} numberOfLines={expanded ? undefined : 2}>{row.lastError}</Text>
          ) : null}
        </Pressable>
        {expanded ? <Text style={[styles.payload, { backgroundColor: c.bg }]}>{JSON.stringify(row.op, null, 2)}</Text> : null}
      </Card.Content>
      <Card.Actions>
        <Button title="Retry" onPress={() => onRetry(row.op.commandId)} />
        <Button title="Discard" variant="destructive" onPress={() => void discard()} />
        <Button title={expanded ? 'Less' : 'Details'} variant="secondary" onPress={() => setExpanded(!expanded)} />
      </Card.Actions>
    </Card>
  );
}

const styles = StyleSheet.create({
  container: { padding: spacing.lg, gap: spacing.sm },
  statusRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.sm },
  statusText: { flex: 1, fontSize: 13 },
  small: { fontSize: 12 },
  progressBlock: { gap: spacing.xs, marginTop: spacing.sm },
  progressText: { fontSize: 13, textAlign: 'center' },
  empty: { textAlign: 'center', marginTop: spacing.xxl },
  sectionHead: { flexDirection: 'row', alignItems: 'center' },
  grow: { flex: 1 },
  // Inside a padded page a subheader's own 16dp inset would put headings right of the text they head.
  subheader: { paddingHorizontal: 0, paddingTop: spacing.md, paddingBottom: spacing.xs },
  card: { marginBottom: spacing.sm },
  opLabel: { fontSize: 15, fontWeight: '600' },
  payload: { fontFamily: 'monospace', fontSize: 11, borderRadius: radii.sm, padding: spacing.sm },
});
