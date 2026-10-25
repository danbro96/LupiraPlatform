# Changelog

## 0.2.0

- `cookieTransport`: an error response's `ApiError` carries the problem+json `type`, `title` and `traceId`.
- **Breaking** peer `@danbro96/lupira-http` `^0.2.1 || ^0.3.0`.

## 0.1.1

- Peer range admits `@danbro96/lupira-http` 0.2.0.

## 0.1.0

- `session`: `SessionUser`, `getSessionUser`, `login`, `logout`.
- `useSession`: the react-query session hook.
- `RequireAuth`: route gate (`<Outlet />` once signed in), `pending` renders the interim states.
- `cookieTransport`: `cookieTransport({ baseUrl?, redirectOn401? })`, `installCookieTransport(options?)` over the `@danbro96/lupira-http` transport seam.
