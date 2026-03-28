// General formatting utilities for display values in UI components.

/**
 * Convert a PascalCase or camelCase status string to a spaced label.
 * @example formatStatus("InProgress") → "In Progress"
 * @example formatStatus("Dispensed") → "Dispensed"
 */
export function formatStatus(status: string): string {
  // Insert a space before each uppercase letter that follows a lowercase letter
  return status.replace(/([a-z])([A-Z])/g, "$1 $2");
}

/**
 * Combine first and last name into a display name.
 * @example formatName("Jane", "Doe") → "Jane Doe"
 */
export function formatName(firstName: string, lastName: string): string {
  return `${firstName} ${lastName}`.trim();
}
