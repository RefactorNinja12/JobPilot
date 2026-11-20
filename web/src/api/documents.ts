// Mirrors the JSON from Jobpilot.Api: the user's base CV and cover letter, as plain text.
import { getJson, postFile, sendJson } from './http'

export type DocumentKind = 'Cv' | 'CoverLetter'

/** Longest text the API accepts. */
export const maxDocumentLength = 30_000

export type BaseDocument = {
  kind: DocumentKind
  text: string
  updatedAt: string
}

/** The saved documents; a kind that has not been saved is not in the list. */
export function fetchDocuments(signal?: AbortSignal): Promise<BaseDocument[]> {
  return getJson<BaseDocument[]>('/api/documents', signal)
}

/** Saves one document, replacing the earlier one of that kind. */
export function saveDocument(kind: DocumentKind, text: string): Promise<BaseDocument> {
  return sendJson<BaseDocument>('PUT', '/api/documents', { kind, text })
}

/** Reads the text out of a Word (.docx) file. Nothing is saved: the user checks the text first. */
export function extractDocxText(file: File): Promise<{ text: string }> {
  return postFile<{ text: string }>('/api/documents/extract', file)
}
