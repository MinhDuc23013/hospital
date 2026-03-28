// Doctor-scoped proxy allowlist — only these paths may pass through to the gateway.
// Exported separately so unit tests can import without Next.js route file constraints.
const ALLOWED_PATHS = [
  /^appointments$/, // list all
  /^appointments\/[^/]+$/, // get/update single
  /^appointments\/[^/]+\/(complete|cancel)$/, // status mutations
  /^patients$/, // list
  /^patients\/[^/]+$/, // get single
  /^medical-records$/, // list + create
  /^medical-records\/[^/]+$/, // get single
  /^prescriptions$/, // list + create
  /^prescriptions\/[^/]+$/, // get single
  /^providers\/?$/, // list providers
];

export function isAllowedPath(path: string): boolean {
  return ALLOWED_PATHS.some((pattern) => pattern.test(path));
}
