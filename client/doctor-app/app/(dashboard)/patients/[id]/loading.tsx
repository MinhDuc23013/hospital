// Skeleton loader for patient profile page.
export default function PatientProfileLoading() {
  return (
    <div className="space-y-6">
      <div className="h-8 w-56 animate-pulse rounded-md bg-muted" />
      <div className="rounded-lg border bg-card p-6 space-y-4">
        <div className="h-5 w-32 animate-pulse rounded bg-muted" />
        <div className="grid gap-4 sm:grid-cols-2">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="h-5 animate-pulse rounded bg-muted" />
          ))}
        </div>
      </div>
      <div className="flex gap-3">
        <div className="h-10 w-40 animate-pulse rounded-md bg-muted" />
        <div className="h-10 w-44 animate-pulse rounded-md bg-muted" />
      </div>
    </div>
  );
}
