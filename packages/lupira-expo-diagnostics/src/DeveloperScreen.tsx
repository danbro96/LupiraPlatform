import { useNavigation, type NavigationProp } from '@react-navigation/native';
import { useState } from 'react';
import { ScrollView, StyleSheet, View } from 'react-native';
import { Button, List, RadioButton, Text } from 'react-native-paper';
import { spacing } from '@danbro96/lupira-tokens-core/spacing';
import { TextField } from '@danbro96/lupira-expo-paper/components/TextField';
import { useColors } from '@danbro96/lupira-expo-paper/theme/useColors';

/** 'dev' = the backend's own auth bypass. */
export type AuthMode = 'oidc' | 'dev';

/** `urls.api` is the primary origin; multi-backend apps add keys. */
export type ApiPreset = {
  key: string;
  label: string;
  urls: { api: string } & Record<string, string>;
  authMode: AuthMode;
};

export type DiagnosticRoute = { route: string; label: string };

export interface DeveloperScreenProps {
  presets: ApiPreset[];
  diagnosticRoutes: DiagnosticRoute[];
  apiUrl: string;
  authMode: AuthMode;
  /** Receives the chosen origins trimmed and without a trailing slash. */
  onSelectBackend: (urls: Record<string, string>, authMode: AuthMode) => void;
  customUrlPlaceholder: string;
  /** Rendered as raw JSON; the section is left out when undefined. */
  syncState?: unknown;
}

/** Developer tooling, deliberately out of the user path: backend switching (a family member on the
 *  LAN preset has a silently dead app), diagnostics links, raw sync state. */
export function DeveloperScreen({
  presets,
  diagnosticRoutes,
  apiUrl,
  authMode,
  onSelectBackend,
  customUrlPlaceholder,
  syncState,
}: DeveloperScreenProps) {
  const c = useColors();
  const navigation = useNavigation<NavigationProp<Record<string, undefined>>>();
  const activeKey = presets.find((p) => p.urls.api === apiUrl && p.authMode === authMode)?.key ?? 'custom';
  const [customUrl, setCustomUrl] = useState(activeKey === 'custom' ? apiUrl : '');
  const [customMode, setCustomMode] = useState<AuthMode>(authMode);

  const applyBackend = (urls: Record<string, string>, mode: AuthMode) => {
    const trimmed = Object.fromEntries(Object.entries(urls).map(([k, v]) => [k, v.trim().replace(/\/$/, '')]));
    onSelectBackend(trimmed, mode);
  };

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <List.Subheader style={styles.subheader}>Backend</List.Subheader>
      {/* http presets need cleartext networking — dev-client-only (release builds block cleartext). */}
      {presets.filter((p) => __DEV__ || p.urls.api.startsWith('https')).map((p) => (
        <List.Item
          key={p.key}
          onPress={() => applyBackend(p.urls, p.authMode)}
          title={p.label}
          description={`${Object.values(p.urls).join(' · ')} · ${p.authMode === 'oidc' ? 'sign-in' : 'dev bypass'}`}
          left={() => <RadioButton status={activeKey === p.key ? 'checked' : 'unchecked'} value={p.key} onPress={() => applyBackend(p.urls, p.authMode)} />}
        />
      ))}
      <View style={styles.custom}>
        <List.Item
          title="Custom"
          left={() => <RadioButton status={activeKey === 'custom' ? 'checked' : 'unchecked'} value="custom" />}
        />
        <TextField
          label={customUrlPlaceholder}
          autoCapitalize="none"
          autoCorrect={false}
          value={customUrl}
          onChangeText={setCustomUrl}
        />
        <Button mode="text" compact onPress={() => setCustomMode(customMode === 'oidc' ? 'dev' : 'oidc')}>
          Auth: {customMode === 'oidc' ? 'sign-in' : 'dev bypass'} (tap to toggle)
        </Button>
        <Button
          mode="outlined"
          disabled={!customUrl.trim()}
          onPress={() => applyBackend({ api: customUrl }, customMode)}
        >
          Use custom backend
        </Button>
      </View>

      <List.Subheader style={styles.subheader}>Diagnostics</List.Subheader>
      {diagnosticRoutes.map((d) => (
        <Button key={d.route} mode="text" compact onPress={() => navigation.navigate(d.route)}>
          {d.label}
        </Button>
      ))}

      {syncState === undefined ? null : (
        <>
          <List.Subheader style={styles.subheader}>Sync state</List.Subheader>
          <Text style={[styles.mono, { color: c.textMuted }]}>{JSON.stringify(syncState, null, 2)}</Text>
        </>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  // Inside a padded page a subheader's own 16dp inset would put headings right of the text they head.
  subheader: { paddingHorizontal: 0, paddingTop: spacing.md, paddingBottom: spacing.xs },
  container: { padding: spacing.lg, gap: spacing.sm },
  custom: { paddingVertical: spacing.sm, gap: spacing.sm },
  mono: { fontFamily: 'monospace', fontSize: 11 },
});
