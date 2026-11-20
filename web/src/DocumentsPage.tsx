import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import {
  extractDocxText,
  fetchDocuments,
  maxDocumentLength,
  saveDocument,
  type BaseDocument,
  type DocumentKind,
} from './api/documents'
import { ApiError } from './api/http'

const message = (e: unknown) => (e instanceof ApiError ? e.message : 'Något gick fel. Försök igen.')

const savedAt = (iso: string) =>
  new Date(iso).toLocaleString('sv-SE', { dateStyle: 'medium', timeStyle: 'short' })

function DocumentEditor({
  kind,
  title,
  saved,
  onSaved,
}: {
  kind: DocumentKind
  title: string
  saved: BaseDocument | undefined
  onSaved: (document: BaseDocument) => void
}) {
  const [text, setText] = useState(saved?.text ?? '')
  const [notice, setNotice] = useState<string>()
  const [error, setError] = useState<string>()
  const [busy, setBusy] = useState(false)
  const fileInput = useRef<HTMLInputElement>(null)

  const changed = text.trim() !== (saved?.text ?? '')
  const tooLong = text.length > maxDocumentLength
  const textId = `document-${kind}`

  const onFile = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = '' // So the same file can be chosen again.
    if (!file) return
    setError(undefined)
    setNotice(undefined)
    setBusy(true)
    extractDocxText(file).then(
      (extracted) => {
        setText(extracted.text)
        setNotice('Texten från filen ligger i rutan. Läs igenom den och tryck på Spara när den stämmer.')
        setBusy(false)
      },
      (err: unknown) => {
        setError(message(err))
        setBusy(false)
      },
    )
  }

  const onSubmit = (e: FormEvent) => {
    e.preventDefault()
    setError(undefined)
    setNotice(undefined)
    setBusy(true)
    saveDocument(kind, text).then(
      (document) => {
        setText(document.text)
        onSaved(document)
        setBusy(false)
      },
      (err: unknown) => {
        setError(message(err))
        setBusy(false)
      },
    )
  }

  return (
    <form className="document-form" onSubmit={onSubmit} aria-label={title}>
      <label htmlFor={textId}>{title}</label>
      <textarea
        id={textId}
        rows={14}
        value={text}
        onChange={(e) => setText(e.target.value)}
        placeholder="Klistra in texten här, eller ladda upp en Word-fil nedan."
      />
      <p className={tooLong ? 'error' : 'muted'}>
        {text.length.toLocaleString('sv-SE')} av {maxDocumentLength.toLocaleString('sv-SE')} tecken
        {saved && ` · Sparad ${savedAt(saved.updatedAt)}`}
      </p>
      <div className="document-actions">
        <button type="button" onClick={() => fileInput.current?.click()} disabled={busy}>
          Ladda upp Word-fil (.docx)
        </button>
        <input
          ref={fileInput}
          type="file"
          accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
          onChange={onFile}
          hidden
          aria-label={`Word-fil för ${title}`}
        />
        <button type="submit" disabled={busy || !changed || text.trim() === '' || tooLong}>
          Spara
        </button>
      </div>
      {notice && <p role="status">{notice}</p>}
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
    </form>
  )
}

/** The base CV and cover letter: the starting point Claude later tailors to each ad. Plain text only. */
export function DocumentsPage() {
  const [documents, setDocuments] = useState<BaseDocument[]>()
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    fetchDocuments(controller.signal)
      .then(setDocuments)
      .catch(() => {
        if (!controller.signal.aborted) setFailed(true)
      })
    return () => controller.abort()
  }, [])

  if (failed) return <p role="alert" className="error">Kunde inte hämta dokumenten. Kör API:t?</p>
  if (!documents) return <p aria-busy="true">Hämtar dokumenten…</p>

  const onSaved = (saved: BaseDocument) =>
    setDocuments((current) => [...(current ?? []).filter((d) => d.kind !== saved.kind), saved])

  return (
    <div className="documents">
      <p className="muted">
        Din grund för ansökningar. Bara texten sparas, layouten från Word följer inte med.
      </p>
      <DocumentEditor kind="Cv" title="CV" saved={documents.find((d) => d.kind === 'Cv')} onSaved={onSaved} />
      <DocumentEditor
        kind="CoverLetter"
        title="Personligt brev"
        saved={documents.find((d) => d.kind === 'CoverLetter')}
        onSaved={onSaved}
      />
    </div>
  )
}
