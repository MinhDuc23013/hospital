// Date formatting utilities using date-fns (already in package.json).
import { format, formatDistanceToNow, parseISO } from "date-fns";

function toDate(date: string | Date): Date {
  return typeof date === "string" ? parseISO(date) : date;
}

/**
 * Format a date to a readable long-form date string.
 * @example formatDate("2026-03-19T14:30:00Z") → "March 19, 2026"
 */
export function formatDate(date: string | Date): string {
  return format(toDate(date), "MMMM d, yyyy");
}

/**
 * Format a date to a 12-hour time string.
 * @example formatTime("2026-03-19T14:30:00Z") → "2:30 PM"
 */
export function formatTime(date: string | Date): string {
  return format(toDate(date), "h:mm a");
}

/**
 * Format a date as a relative time string from now.
 * @example formatRelative("2026-03-21T14:30:00Z") → "in 2 days"
 */
export function formatRelative(date: string | Date): string {
  return formatDistanceToNow(toDate(date), { addSuffix: true });
}

/**
 * Format an ISO 8601 duration string to a human-readable label.
 * Supports hours (H) and minutes (M) components.
 * @example formatDuration("PT30M") → "30 min"
 * @example formatDuration("PT1H30M") → "1 hr 30 min"
 * @example formatDuration("PT1H") → "1 hr"
 */
export function formatDuration(duration: string): string {
  const hourMatch = duration.match(/(\d+)H/);
  const minMatch = duration.match(/(\d+)M/);
  const hours = hourMatch ? parseInt(hourMatch[1], 10) : 0;
  const minutes = minMatch ? parseInt(minMatch[1], 10) : 0;

  const parts: string[] = [];
  if (hours > 0) parts.push(`${hours} hr`);
  if (minutes > 0) parts.push(`${minutes} min`);

  return parts.length > 0 ? parts.join(" ") : duration;
}
