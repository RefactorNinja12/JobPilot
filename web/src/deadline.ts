// How long is left to apply for an ad. The deadline is a calendar date (YYYY-MM-DD) with no time of day.

/** Today as YYYY-MM-DD in the browser's time zone, to compare with the ad's deadline (a date without time). */
function today(): string {
  const now = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

/** Whole days from today to the deadline; both are calendar dates, so UTC midnight avoids daylight-saving drift. */
export function daysLeft(deadline: string): number {
  return Math.round((Date.parse(deadline) - Date.parse(today())) / 86_400_000)
}

export function deadlineText(deadline: string): string {
  const days = daysLeft(deadline)
  if (days < 0) return 'Sista ansökningsdag har passerat'
  if (days === 0) return 'Sista ansökningsdag idag'
  if (days === 1) return '1 dag kvar att söka'
  return `${days} dagar kvar att söka`
}
