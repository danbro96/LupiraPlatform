import { useEffect, useRef, type ReactNode } from 'react';
import { Outlet } from 'react-router';
import { login } from './session.ts';
import { useSession } from './useSession.ts';

/** Route gate: a logged-out visitor is sent to the BFF sign-in (required SSO), returning to the original path. */
export function RequireAuth({ pending = () => null }: { pending?: (title: string) => ReactNode }) {
  const { data, isLoading } = useSession();
  const triggered = useRef(false);

  useEffect(() => {
    if (!isLoading && data == null && !triggered.current) {
      triggered.current = true;
      login();
    }
  }, [isLoading, data]);

  if (isLoading) return pending('Loading…');
  if (data == null) return pending('Signing in…');
  return <Outlet />;
}
