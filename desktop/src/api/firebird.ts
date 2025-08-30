import { api } from './http'
import type { QueryRequest } from './types'

export const FirebirdApi = {
  testConnection(databaseId?: string): Promise<{ ok: boolean; message?: string }> {
    return api.request('GET', '/api/Firebird/test-connection', undefined, { databaseId })
  },
  executeQuery<T = unknown[]>(payload: QueryRequest, databaseId?: string): Promise<T> {
    return api.request('POST', '/api/Firebird/execute-query', payload, { databaseId })
  },
  executeNonQuery(payload: QueryRequest, databaseId?: string): Promise<{ affected?: number }> {
    return api.request('POST', '/api/Firebird/execute-non-query', payload, { databaseId })
  },
  executeScalar<T = unknown>(payload: QueryRequest, databaseId?: string): Promise<T> {
    return api.request('POST', '/api/Firebird/execute-scalar', payload, { databaseId })
  },
}


