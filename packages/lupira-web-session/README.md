# @danbro96/lupira-web-session

Cookie-session plumbing for SPAs behind a Lupira BFF (`/auth/*` same-origin).
In `main.tsx`: `installCookieTransport()`. Gate routes with `<RequireAuth pending={(title) => <Centered title={title} />} />`.
