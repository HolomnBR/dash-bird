export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE'

export interface HttpClientOptions {
  baseUrl?: string
  headers?: Record<string, string>
}

export class HttpClient {
  private readonly baseUrl: string
  private readonly defaultHeaders: Record<string, string>

  constructor(options?: HttpClientOptions) {
    this.baseUrl = options?.baseUrl ?? 'http://localhost:8000'
    this.defaultHeaders = {
      'Content-Type': 'application/json',
      ...(options?.headers ?? {}),
    }
  }

  async request<T>(method: HttpMethod, path: string, body?: unknown, params?: Record<string, string | number | undefined>): Promise<T> {
    const url = new URL(path, this.baseUrl)
    if (params) {
      for (const [key, value] of Object.entries(params)) {
        if (value !== undefined && value !== null) url.searchParams.set(key, String(value))
      }
    }

    const response = await fetch(url.toString(), {
      method,
      headers: this.defaultHeaders,
      body: body === undefined ? undefined : JSON.stringify(body),
    })

    if (!response.ok) {
      const text = await response.text().catch(() => '')
      throw new Error(`HTTP ${response.status} ${response.statusText} - ${text}`)
    }

    const contentType = response.headers.get('content-type') || ''
    if (contentType.includes('application/json') || contentType.includes('text/json')) {
      return (await response.json()) as T
    }
    return (await response.text()) as unknown as T
  }
}

export const api = new HttpClient()


