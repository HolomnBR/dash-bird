import { api } from './http'
import type { AddDatabaseRequest, DatabaseConfig, TableSchemaRequest, TableSchema, ProjectConfig, TableMetadata } from './types'

export const DatabaseConfigApi = {
  listDatabases(): Promise<DatabaseConfig[]> {
    return api.request('GET', '/api/DatabaseConfig/databases')
  },
  addDatabase(payload: AddDatabaseRequest): Promise<DatabaseConfig> {
    return api.request('POST', '/api/DatabaseConfig/databases', payload)
  },
  getDatabase(id: string): Promise<DatabaseConfig> {
    return api.request('GET', `/api/DatabaseConfig/databases/${encodeURIComponent(id)}`)
  },
  updateDatabase(id: string, payload: DatabaseConfig): Promise<DatabaseConfig> {
    return api.request('PUT', `/api/DatabaseConfig/databases/${encodeURIComponent(id)}`, payload)
  },
  removeDatabase(id: string): Promise<void> {
    return api.request('DELETE', `/api/DatabaseConfig/databases/${encodeURIComponent(id)}`)
  },
  testConnection(id: string): Promise<{ ok: boolean; message?: string }> {
    return api.request('GET', `/api/DatabaseConfig/databases/${encodeURIComponent(id)}/test-connection`)
  },
  listTables(id: string): Promise<TableMetadata[]> {
    return api.request('GET', `/api/DatabaseConfig/databases/${encodeURIComponent(id)}/tables`)
  },
  getTableSchema(id: string, payload: TableSchemaRequest): Promise<TableSchema> {
    return api.request('POST', `/api/DatabaseConfig/databases/${encodeURIComponent(id)}/table-schema`, payload)
  },
  getDefaultDatabase(): Promise<DatabaseConfig> {
    return api.request('GET', '/api/DatabaseConfig/default-database')
  },
  setDefaultDatabase(payload: { databaseId: string }): Promise<void> {
    return api.request('POST', '/api/DatabaseConfig/default-database', payload)
  },
  getProjectConfig(): Promise<ProjectConfig> {
    return api.request('GET', '/api/DatabaseConfig/project-config')
  },
}


