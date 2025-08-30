// Types derived from the provided OpenAPI 3.0.4 spec

export interface AddDatabaseRequest {
  name?: string | null
  database: string
  server?: string | null
  username?: string | null
  password?: string | null
  port?: number | null
  charset?: string | null
}

export interface DatabaseConfig {
  id?: string | null
  name?: string | null
  server?: string | null
  database?: string | null
  username?: string | null
  password?: string | null
  port: number
  charset?: string | null
  createdAt: string
  isActive: boolean
}

export interface QueryRequest {
  query?: string | null
}

export interface SetDefaultDatabaseRequest {
  databaseId?: string | null
}

export interface TableSchemaRequest {
  tableNames?: string[] | null
}

export interface TableMetadata {
  name: string
  schema?: string
  type?: string
  description?: string
}

export type ProjectConfig = Record<string, unknown>

export type TableSchema = Record<string, {
  columns: Array<{ name: string; type?: string; nullable?: boolean }>
  primaryKeys?: string[]
}>


